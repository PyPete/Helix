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
        // Strip adapter prefix: "simulator.SetFlow" → "SetFlow"
        var dot = capabilityName.IndexOf('.');
        var verbName = dot >= 0 ? capabilityName[(dot + 1)..] : capabilityName;

        JsonElement jsonParams = parameters is JsonElement je
            ? je
            : JsonSerializer.SerializeToElement(parameters ?? new { });

        var result = await adapter.InvokeCapabilityAsync(verbName, jsonParams, null, cancellationToken);

        return result.Success
            ? CoreResult.Succeeded(result.ReturnValue)
            : CoreResult.Failed(result.ErrorMessage ?? "Capability invocation failed.");
    }

    public Task<bool> IsAvailableAsync(string capabilityName)
        => adapter.HealthCheckAsync();
}
