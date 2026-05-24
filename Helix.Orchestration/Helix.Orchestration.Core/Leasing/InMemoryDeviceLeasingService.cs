using System.Collections.Concurrent;

namespace Helix.Orchestration.Core.Leasing;

/// <summary>
/// Thread-safe, in-process implementation of <see cref="IDeviceLeasingService"/>.
/// Suitable for single-node agents and unit tests.
/// </summary>
public sealed class InMemoryDeviceLeasingService : IDeviceLeasingService
{
    private readonly ConcurrentDictionary<string, List<DeviceLease>> _leases = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public Task<DeviceLease> AcquireAsync(
        string deviceId,
        string holderName,
        LeaseMode mode = LeaseMode.Exclusive,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var existing = _leases.GetOrAdd(deviceId, _ => new List<DeviceLease>());

            // Remove expired leases before evaluating.
            existing.RemoveAll(l => l.ExpiresAt.HasValue && l.ExpiresAt.Value <= DateTimeOffset.UtcNow);

            if (existing.Count > 0)
            {
                bool conflict = mode == LeaseMode.Exclusive
                    || existing.Any(l => l.Mode == LeaseMode.Exclusive);

                if (conflict)
                    throw new DeviceAlreadyLeasedException(deviceId, mode);
            }

            var now = DateTimeOffset.UtcNow;
            var leaseId = Guid.NewGuid().ToString("N");
            DeviceLease? lease = null;

            lease = new DeviceLease(
                leaseId: leaseId,
                deviceId: deviceId,
                mode: mode,
                holderName: holderName,
                acquiredAt: now,
                expiresAt: ttl.HasValue ? now + ttl.Value : null,
                release: () =>
                {
                    lock (_lock)
                    {
                        if (_leases.TryGetValue(deviceId, out var list))
                            list.RemoveAll(l => l.LeaseId == leaseId);
                    }
                    return Task.CompletedTask;
                });

            existing.Add(lease);
            return Task.FromResult(lease);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<DeviceLease> GetActiveLeases(string? deviceId = null)
    {
        lock (_lock)
        {
            if (deviceId is not null)
            {
                return _leases.TryGetValue(deviceId, out var list)
                    ? list.Where(IsActive).ToList()
                    : [];
            }

            return _leases.Values.SelectMany(l => l).Where(IsActive).ToList();
        }
    }

    /// <inheritdoc/>
    public bool IsHeldBy(string deviceId, string holderName)
    {
        lock (_lock)
        {
            return _leases.TryGetValue(deviceId, out var list)
                && list.Any(l => l.HolderName == holderName && IsActive(l));
        }
    }

    /// <inheritdoc/>
    public Task ForceReleaseAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_leases.TryGetValue(deviceId, out var list))
                list.Clear();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Purges all expired leases across tracked devices.
    /// </summary>
    /// <returns>Number of removed expired leases.</returns>
    public Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var removed = 0;

            foreach (var (_, leases) in _leases)
            {
                removed += leases.RemoveAll(l => l.ExpiresAt.HasValue && l.ExpiresAt.Value <= DateTimeOffset.UtcNow);
            }

            return Task.FromResult(removed);
        }
    }

    private static bool IsActive(DeviceLease l) =>
        !l.ExpiresAt.HasValue || l.ExpiresAt.Value > DateTimeOffset.UtcNow;
}
