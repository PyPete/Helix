using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using CoreResult = Helix.Orchestration.Core.Capabilities.CapabilityInvocationResult;

namespace Helix.Orchestration.Agent.Adapters.Devices;

/// <summary>
/// Bridges an <see cref="IHelixAdapter"/> (Devices layer) to
/// <see cref="Helix.Orchestration.Core.Capabilities.ICapabilityInvoker"/> (Core layer),
/// translating between the two <c>CapabilityInvocationResult</c> types.
/// </summary>
internal sealed class HelixAdapterCapabilityBridge(IHelixAdapter adapter)
    : Helix.Orchestration.Core.Capabilities.ICapabilityInvoker
{
    public async Task<CoreResult> InvokeAsync(
        string capabilityName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        JsonElement jsonParams = parameters is JsonElement je
            ? je
            : JsonSerializer.SerializeToElement(parameters ?? new { });

        var result = await adapter.InvokeCapabilityAsync(capabilityName, jsonParams, null, cancellationToken);

        return result.Success
            ? CoreResult.Succeeded(result.ReturnValue)
            : CoreResult.Failed(result.ErrorMessage ?? "Capability invocation failed.");
    }

    public async Task<bool> IsAvailableAsync(string capabilityName)
        => adapter.GetCapabilities().Any(c => c.Name == capabilityName)
           && await adapter.HealthCheckAsync();
}
