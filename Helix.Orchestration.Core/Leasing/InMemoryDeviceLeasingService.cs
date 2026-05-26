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
    public Task<DeviceLease> RenewAsync(
        string deviceId,
        string leaseId,
        TimeSpan extension,
        CancellationToken cancellationToken = default)
    {
        if (extension <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(extension), "Lease extension must be greater than zero.");

        lock (_lock)
        {
            if (!_leases.TryGetValue(deviceId, out var list))
                throw new InvalidOperationException($"No active leases found for device '{deviceId}'.");

            list.RemoveAll(l => l.ExpiresAt.HasValue && l.ExpiresAt.Value <= DateTimeOffset.UtcNow);

            var index = list.FindIndex(l => l.LeaseId == leaseId);
            if (index < 0)
                throw new InvalidOperationException($"Lease '{leaseId}' was not found for device '{deviceId}'.");

            var existing = list[index];
            if (!existing.ExpiresAt.HasValue)
                throw new InvalidOperationException($"Lease '{leaseId}' cannot be renewed because it does not expire.");

            var now = DateTimeOffset.UtcNow;
            if (existing.ExpiresAt.Value <= now)
                throw new InvalidOperationException($"Lease '{leaseId}' is expired and cannot be renewed.");

            var baseTime = existing.ExpiresAt.Value > now ? existing.ExpiresAt.Value : now;
            var renewed = new DeviceLease(
                leaseId: existing.LeaseId,
                deviceId: existing.DeviceId,
                mode: existing.Mode,
                holderName: existing.HolderName,
                acquiredAt: existing.AcquiredAt,
                expiresAt: baseTime + extension,
                release: () =>
                {
                    lock (_lock)
                    {
                        if (_leases.TryGetValue(deviceId, out var leases))
                            leases.RemoveAll(l => l.LeaseId == leaseId);
                    }

                    return Task.CompletedTask;
                });

            list[index] = renewed;
            return Task.FromResult(renewed);
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
