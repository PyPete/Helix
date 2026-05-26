namespace Helix.Orchestration.Core.Capabilities;

/// <summary>
/// Defines the contract for invoking device capabilities in a device-agnostic manner.
/// Workflows call capabilities by name, not by device type or protocol.
/// Implementations route to OPC UA, SiLA 2, or vendor SDKs.
/// </summary>
public interface ICapabilityInvoker
{
    /// <summary>
    /// Invokes a capability by name with optional parameters.
    /// </summary>
    /// <param name="capabilityName">The capability to invoke (e.g., "SetTemperature", "RunMethod", "AcquireSpectrum").</param>
    /// <param name="parameters">Optional parameters for the capability invocation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the capability invocation.</returns>
    Task<CapabilityInvocationResult> InvokeAsync(
        string capabilityName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a capability is available (device online, properly configured).
    /// </summary>
    /// <param name="capabilityName">The capability name to check.</param>
    /// <returns>True if the capability is available, false otherwise.</returns>
    Task<bool> IsAvailableAsync(string capabilityName);
}
