using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Devices.Interfaces;

namespace Helix.Orchestration.Agent.Adapters.Devices;

public sealed class DeviceCapabilityAdapter<TDevice>(string capabilityName, TDevice device)
    : ICapabilityInvoker
    where TDevice : class, IDevice
{
    private readonly string _capabilityName = capabilityName;
    private readonly TDevice _device = device;

    public Task<bool> IsAvailableAsync(string capabilityName)
    {
        if (!string.Equals(capabilityName, _capabilityName, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(false);

        var result = _device.IsDevice();
        return Task.FromResult(result.State != global::Helix.Orchestration.Devices.Model.DeviceResultState.Failure);
    }

    public async Task<CapabilityInvocationResult> InvokeAsync(string capabilityName, object? parameters = null, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(capabilityName, _capabilityName, StringComparison.OrdinalIgnoreCase))
            return CapabilityInvocationResult.Failed($"Capability '{capabilityName}' is not handled by this adapter.");

        var started = DateTimeOffset.UtcNow;

        try
        {
            var op = parameters as DeviceOperationParameters;

            var returnValue = await Task.Run(() => Execute(op), cancellationToken);

            return CapabilityInvocationResult.Succeeded(returnValue, DateTimeOffset.UtcNow - started);
        }
        catch (Exception ex)
        {
            return CapabilityInvocationResult.Failed("Device capability invocation failed.", ex, DateTimeOffset.UtcNow - started);
        }
    }

    private object Execute(DeviceOperationParameters? operation)
    {
        if (operation is null || string.IsNullOrWhiteSpace(operation.Operation))
            return _device.GetMeasurements();

        return operation.Operation.ToLowerInvariant() switch
        {
            "health" => _device.IsDevice(),
            "close" => _device.Close(),
            "measurements" => _device.GetMeasurements(),
            "status" => ExecuteStatusQuery(),
            _ => throw new NotSupportedException($"Operation '{operation.Operation}' is not supported by generic adapter.")
        };
    }

    private object ExecuteStatusQuery()
    {
        // Type-check and dispatch to device-specific status method if available
        if (_device is IVapourtec vapourtec)
        {
            var (result, status) = vapourtec.GetStatus();
            return new { Result = result, Status = status };
        }

        throw new NotSupportedException($"Device type '{_device.GetType().Name}' does not support status queries.");
    }
}
