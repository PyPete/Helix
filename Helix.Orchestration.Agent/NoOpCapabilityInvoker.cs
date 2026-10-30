using Helix.Orchestration.Core.Capabilities;

namespace Helix.Orchestration.Agent;

/// <summary>Stub capability invoker used during early runtime bring-up.</summary>
public sealed class NoOpCapabilityInvoker : ICapabilityInvoker
{
    public Task<CapabilityInvocationResult> InvokeAsync(string capabilityName, object? parameters = null, CancellationToken cancellationToken = default)
        => Task.FromResult(CapabilityInvocationResult.Succeeded(parameters));

    public Task<bool> IsAvailableAsync(string capabilityName) => Task.FromResult(true);
}
