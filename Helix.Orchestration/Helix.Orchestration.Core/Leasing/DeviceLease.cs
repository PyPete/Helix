namespace Helix.Orchestration.Core.Leasing;

/// <summary>
/// Represents an active lease on a device or resource.
/// Disposing the lease releases it back to the pool.
/// </summary>
public sealed class DeviceLease : IAsyncDisposable
{
    private readonly Func<Task> _release;
    private bool _disposed;

    /// <summary>Unique identifier for the lease itself.</summary>
    public string LeaseId { get; }

    /// <summary>Identifier of the leased device.</summary>
    public string DeviceId { get; }

    /// <summary>The lease mode that was granted.</summary>
    public LeaseMode Mode { get; }

    /// <summary>When the lease was acquired.</summary>
    public DateTimeOffset AcquiredAt { get; }

    /// <summary>Optional expiry; null means the lease does not expire automatically.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>The workflow or caller that holds this lease.</summary>
    public string HolderName { get; }

    internal DeviceLease(
        string leaseId,
        string deviceId,
        LeaseMode mode,
        string holderName,
        DateTimeOffset acquiredAt,
        DateTimeOffset? expiresAt,
        Func<Task> release)
    {
        LeaseId = leaseId;
        DeviceId = deviceId;
        Mode = mode;
        HolderName = holderName;
        AcquiredAt = acquiredAt;
        ExpiresAt = expiresAt;
        _release = release;
    }

    /// <summary>Releases the lease.</summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            await _release();
        }
    }
}
