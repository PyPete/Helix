using Helix.Orchestration.Core.Capabilities;

namespace Helix.Orchestration.Agent;

/// <summary>
/// Stub capability invoker used during early runtime bring-up.
/// Always reports success without contacting real devices.
/// </summary>
public sealed class NoOpCapabilityInvoker : ICapabilityInvoker
{
    /// <summary>
    /// Returns a successful invocation result containing the provided parameters.
    /// </summary>
    public Task<CapabilityInvocationResult> InvokeAsync(string capabilityName, object? parameters = null, CancellationToken cancellationToken = default)
        => Task.FromResult(CapabilityInvocationResult.Succeeded(parameters));

    /// <summary>
    /// Always returns true for capability availability.
    /// </summary>
    public Task<bool> IsAvailableAsync(string capabilityName)
        => Task.FromResult(true);
}
