using Helix.Orchestration.Devices.Abstractions;
using CoreResult = Helix.Orchestration.Core.Capabilities.CapabilityInvocationResult;

namespace Helix.Orchestration.Agent.Adapters.Devices;

/// <summary>
/// Retained compatibility bridge for adapter discovery and health checks only.
/// </summary>
/// <remarks>
/// Device adapters have no canonical command identity, fresh admission snapshot,
/// dispatch-start fence, or durable outcome ledger. They are therefore incapable
/// of independently owning a mutation path.
/// </remarks>
internal sealed class HelixAdapterCapabilityBridge(IHelixAdapter adapter)
    : Helix.Orchestration.Core.Capabilities.ICapabilityInvoker
{
    public Task<CoreResult> InvokeAsync(
        string capabilityName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityName);

        // Forwarding here would strip canonical vocabulary and create a direct
        // registry-to-device mutation bypass. The canonical Agent dispatcher is
        // the only component that can issue effects.
        return Task.FromResult(CoreResult.Failed(
            $"Direct adapter invocation for '{capabilityName}' is disabled; submit a canonical command to the Agent dispatcher."));
    }

    public Task<bool> IsAvailableAsync(string capabilityName)
        => adapter.HealthCheckAsync();
}
