namespace Helix.Orchestration.Core.Leasing;

/// <summary>
/// Service that grants, tracks, and releases device leases.
/// </summary>
public interface IDeviceLeasingService
{
    /// <summary>
    /// Attempts to acquire a lease on <paramref name="deviceId"/>.
    /// </summary>
    /// <param name="deviceId">The device to lease.</param>
    /// <param name="holderName">Name of the workflow or caller requesting the lease.</param>
    /// <param name="mode">The lease mode requested.</param>
    /// <param name="ttl">Optional time-to-live; null means the lease does not expire automatically.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="DeviceLease"/> that releases the device when disposed.</returns>
    /// <exception cref="DeviceAlreadyLeasedException">
    /// Thrown when the device is already held in an incompatible mode.
    /// </exception>
    Task<DeviceLease> AcquireAsync(
        string deviceId,
        string holderName,
        LeaseMode mode = LeaseMode.Exclusive,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active leases, optionally filtered to a single device.
    /// </summary>
    IReadOnlyList<DeviceLease> GetActiveLeases(string? deviceId = null);

    /// <summary>
    /// Returns true if the given device has an active lease held by <paramref name="holderName"/>.
    /// </summary>
    bool IsHeldBy(string deviceId, string holderName);

    /// <summary>
    /// Forcibly releases all leases held on <paramref name="deviceId"/>.
    /// Used by safety/emergency-stop paths.
    /// </summary>
    Task ForceReleaseAsync(string deviceId, CancellationToken cancellationToken = default);
}
