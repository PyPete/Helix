using System.Runtime.CompilerServices;
using System.Text.Json;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Null;
using Microsoft.Extensions.Logging;
using CoreCapabilityDescriptor = Helix.Orchestration.Core.Capabilities.CapabilityDescriptor;
using CoreCapabilityCategory = Helix.Orchestration.Core.Capabilities.CapabilityCategory;
using CoreParameterSchema = Helix.Orchestration.Core.Capabilities.ParameterSchema;
using CoreParameterType = Helix.Orchestration.Core.Capabilities.ParameterType;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>In-process simulator with command-identified, observable effects.</summary>
public sealed class SimulatorDevice : DeviceAdapterBase
{
    private readonly ISimulatorStateStore? _stateStore;
    private double _flowSetpoint;
    private double _temperature = 25;
    private string _pumpMode = "Isocratic";
    private readonly Dictionary<string, LocalSimulatorState> _localStates = new(StringComparer.Ordinal);

    public SimulatorDevice(
        NullDeviceTransport transport,
        ILogger<SimulatorDevice> logger,
        ISimulatorStateStore? stateStore = null)
        : base(transport, logger) => _stateStore = stateStore;

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        builder.RegisterSimple("SetFlow", "Set the pump flow rate (mL/min)", ct => Task.FromResult(CapabilityInvocationResult.Successful()));
        builder.RegisterSimple("GetFlow", "Read back the current flow setpoint", ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { flowRate = _flowSetpoint, unit = "mL/min" }))));
        builder.RegisterSimple("SetTemperature", "Set the column oven temperature (°C)", ct => Task.FromResult(CapabilityInvocationResult.Successful()));
        builder.RegisterSimple("GetTemperature", "Read back the column oven temperature", ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { temperature = _temperature, unit = "°C" }))));
        builder.RegisterSimple("SetMode", "Set the pump operating mode", ct => Task.FromResult(CapabilityInvocationResult.Successful()));
        builder.RegisterSimple("GetMode", "Read back the pump operating mode", ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { mode = _pumpMode }))));
    }

    public override Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public async Task<SimulatorObservation> ApplySetFlowAsync(Guid commandId, double flowRate, CancellationToken cancellationToken = default)
    {
        if (!double.IsFinite(flowRate) || flowRate < 0 || flowRate > 10 || Math.Abs(flowRate * 10 - Math.Round(flowRate * 10)) > 0.000001)
            throw new ArgumentOutOfRangeException(nameof(flowRate));
        const string stateKey = "flowRate";
        var value = flowRate.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        return await ApplyAndPersistAsync(stateKey, commandId, value, "mL/min", () => _flowSetpoint = flowRate, cancellationToken);
    }

    public async Task<SimulatorObservation> ApplySetTemperatureAsync(Guid commandId, double temperature, CancellationToken cancellationToken = default)
    {
        if (!double.IsFinite(temperature) || temperature < 20 || temperature > 80 || temperature != Math.Truncate(temperature))
            throw new ArgumentOutOfRangeException(nameof(temperature));
        const string stateKey = "temperature";
        var value = temperature.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        return await ApplyAndPersistAsync(stateKey, commandId, value, "°C", () => _temperature = temperature, cancellationToken);
    }

    public async Task<SimulatorObservation> ApplySetModeAsync(Guid commandId, string mode, CancellationToken cancellationToken = default)
    {
        if (mode is not "Isocratic" and not "Gradient" and not "Purge")
            throw new ArgumentOutOfRangeException(nameof(mode));
        return await ApplyAndPersistAsync("mode", commandId, mode, string.Empty, () => _pumpMode = mode, cancellationToken);
    }

    /// <summary>Read-only state observation; it never invokes a mutation capability.</summary>
    public async Task<SimulatorObservation?> GetObservationAsync(string stateKey, CancellationToken cancellationToken = default)
    {
        if (_stateStore is not null)
            return await _stateStore.GetAsync("simulator", stateKey, cancellationToken);

        lock (this)
        {
            return _localStates.TryGetValue(stateKey, out var state)
                ? new SimulatorObservation("simulator", stateKey, state.CommandId, state.CanonicalValue, state.Unit, state.Generation, state.ObservedAtUtc)
                : null;
        }
    }

    public IReadOnlyList<CoreCapabilityDescriptor> GetRichCapabilities() =>
    [
        new("SetFlow", "Set Flow", "Set the pump flow rate", CoreCapabilityCategory.Control,
            [new CoreParameterSchema("flowRate", "Flow Rate", CoreParameterType.Numeric, "mL/min", 0, 10, 0.1, null, 1.0)], false, false, 500),
        new("GetFlow", "Get Flow", "Read back the current flow setpoint", CoreCapabilityCategory.Measurement, [], false, false, 200,
            Returns: [new CoreParameterSchema("flowRate", "Flow Rate", CoreParameterType.Numeric, "mL/min", 0, 10, 0.1, null, null)]),
        new("SetTemperature", "Set Temperature", "Set the column oven temperature", CoreCapabilityCategory.Control,
            [new CoreParameterSchema("temperature", "Temperature", CoreParameterType.Numeric, "°C", 20, 80, 1, null, 40.0)], false, false, 3000),
        new("GetTemperature", "Get Temperature", "Read back the column oven temperature", CoreCapabilityCategory.Measurement, [], false, false,
            Returns: [new CoreParameterSchema("temperature", "Temperature", CoreParameterType.Numeric, "°C", 20, 80, 1, null, null)]),
        new("SetMode", "Set Mode", "Set the pump operating mode", CoreCapabilityCategory.Control,
            [new CoreParameterSchema("mode", "Mode", CoreParameterType.Enum, "", null, null, null, ["Isocratic", "Gradient", "Purge"], "Isocratic")], false, false),
        new("GetMode", "Get Mode", "Read back the pump operating mode", CoreCapabilityCategory.Measurement, [], false, false,
            Returns: [new CoreParameterSchema("mode", "Mode", CoreParameterType.Enum, "", null, null, null, ["Isocratic", "Gradient", "Purge"], null)])
    ];

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            yield return new TrendSample { TrendName = "simulator.flowRate", Value = _flowSetpoint, Timestamp = now, Unit = "mL/min" };
            yield return new TrendSample { TrendName = "simulator.temperature", Value = _temperature, Timestamp = now, Unit = "°C" };
            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }

    private async Task<SimulatorObservation> ApplyAndPersistAsync(
        string stateKey,
        Guid commandId,
        string canonicalValue,
        string unit,
        Action effect,
        CancellationToken cancellationToken)
    {
        LocalSimulatorState local;
        lock (this)
        {
            if (_localStates.TryGetValue(stateKey, out var existing) && existing.CommandId == commandId)
                return new SimulatorObservation("simulator", stateKey, existing.CommandId, existing.CanonicalValue, existing.Unit, existing.Generation, existing.ObservedAtUtc);
            effect();
            local = new LocalSimulatorState(commandId, canonicalValue, unit,
                _localStates.TryGetValue(stateKey, out var prior) ? prior.Generation + 1 : 1,
                DateTimeOffset.UtcNow);
            _localStates[stateKey] = local;
        }

        if (_stateStore is not null)
            return await _stateStore.ApplyAsync("simulator", stateKey, commandId, canonicalValue, unit, cancellationToken);
        Logger.LogInformation("Simulator committed command {CommandId} at generation {Generation}", commandId, local.Generation);
        return new SimulatorObservation("simulator", stateKey, local.CommandId, local.CanonicalValue, local.Unit, local.Generation, local.ObservedAtUtc);
    }

    private sealed record LocalSimulatorState(Guid CommandId, string CanonicalValue, string Unit, long Generation, DateTimeOffset ObservedAtUtc);
}
