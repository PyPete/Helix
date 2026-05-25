using System.Text.Json;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Simulation.Devices;

namespace Helix.Orchestration.Simulation;

/// <summary>
/// Simulated capability invoker that routes capability calls to simulated devices.
/// </summary>
public sealed class SimCapabilityInvoker : ICapabilityInvoker
{
    private readonly SimReactor _reactor;

    public SimCapabilityInvoker(SimReactor reactor)
    {
        _reactor = reactor;
    }

    public Task<CapabilityInvocationResult> InvokeAsync(
        string capabilityName, 
        object? parameters = null, 
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            switch (capabilityName.ToLowerInvariant())
            {
                case "settemperature":
                    return HandleSetTemperature(parameters, startTime);

                default:
                    // For unknown capabilities, return success (Phase 9 stub behavior)
                    var elapsed = DateTimeOffset.UtcNow - startTime;
                    return Task.FromResult(CapabilityInvocationResult.Succeeded(
                        returnValue: $"Capability '{capabilityName}' acknowledged (stub).",
                        duration: elapsed));
            }
        }
        catch (Exception ex)
        {
            var elapsed = DateTimeOffset.UtcNow - startTime;
            return Task.FromResult(CapabilityInvocationResult.Failed(
                errorMessage: $"Capability '{capabilityName}' failed: {ex.Message}",
                exception: ex,
                duration: elapsed));
        }
    }

    public Task<bool> IsAvailableAsync(string capabilityName)
    {
        // All sim capabilities are always available
        return Task.FromResult(true);
    }

    private Task<CapabilityInvocationResult> HandleSetTemperature(object? parameters, DateTimeOffset startTime)
    {
        if (parameters == null)
        {
            return Task.FromResult(CapabilityInvocationResult.Failed("SetTemperature requires a 'Value' parameter."));
        }

        double temperature;

        // Handle various parameter formats
        if (parameters is double d)
        {
            temperature = d;
        }
        else if (parameters is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
        {
            temperature = jsonElement.GetDouble();
        }
        else
        {
            // Try to extract from object properties
            var parametersType = parameters.GetType();
            var valueProperty = parametersType.GetProperty("Value") ?? parametersType.GetProperty("value");

            if (valueProperty != null)
            {
                var value = valueProperty.GetValue(parameters);
                if (value is double dv)
                {
                    temperature = dv;
                }
                else if (value != null && double.TryParse(value.ToString(), out var parsed))
                {
                    temperature = parsed;
                }
                else
                {
                    return Task.FromResult(CapabilityInvocationResult.Failed("SetTemperature 'Value' must be a number."));
                }
            }
            else
            {
                return Task.FromResult(CapabilityInvocationResult.Failed("SetTemperature requires a 'Value' parameter."));
            }
        }

        _reactor.SetTemperature(temperature);

        var elapsed = DateTimeOffset.UtcNow - startTime;
        return Task.FromResult(CapabilityInvocationResult.Succeeded(
            returnValue: $"Temperature setpoint updated to {temperature}°C",
            duration: elapsed));
    }
}
