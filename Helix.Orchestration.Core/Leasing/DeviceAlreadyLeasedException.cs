namespace Helix.Orchestration.Core.Leasing;

/// <summary>
/// Thrown when a device lease cannot be acquired because it is already held
/// in an incompatible mode.
/// </summary>
public sealed class DeviceAlreadyLeasedException : Exception
{
    /// <summary>The device that could not be leased.</summary>
    public string DeviceId { get; }

    /// <summary>The lease mode that was requested.</summary>
    public LeaseMode RequestedMode { get; }

    public DeviceAlreadyLeasedException(string deviceId, LeaseMode requestedMode)
        : base($"Device '{deviceId}' could not be leased in {requestedMode} mode because it is already held.")
    {
        DeviceId = deviceId;
        RequestedMode = requestedMode;
    }
}
