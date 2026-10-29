using System.Runtime.CompilerServices;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Null;
using Microsoft.Extensions.Logging;
using CoreCapabilityDescriptor = Helix.Orchestration.Core.Capabilities.CapabilityDescriptor;
using CoreCapabilityCategory = Helix.Orchestration.Core.Capabilities.CapabilityCategory;
using CoreParameterSchema = Helix.Orchestration.Core.Capabilities.ParameterSchema;
using CoreParameterType = Helix.Orchestration.Core.Capabilities.ParameterType;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// In-process simulator device for development and UAT.
/// Exposes realistic capabilities with typed parameters so the WorkflowBuilder
/// palette and DeviceAction node render correctly without physical hardware.
/// </summary>
public sealed class SimulatorDevice : DeviceAdapterBase
{
    private double _flowSetpoint;
    private double _temperature = 25.0;
    private string _pumpMode = "Isocratic";
    private Guid? _lastSetFlowCommandId;

    public SimulatorDevice(NullDeviceTransport transport, ILogger<SimulatorDevice> logger)
        : base(transport, logger) { }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // SetFlow — Numeric parameter
        builder.RegisterSimple(
            "SetFlow",
            "Set the pump flow rate (mL/min)",
            ct =>
            {
                Logger.LogInformation("Simulator: SetFlow called (setpoint={Setpoint})", _flowSetpoint);
                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // GetFlow — returns current setpoint
        builder.RegisterSimple(
            "GetFlow",
            "Read back the current flow setpoint",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(
                JsonSerializer.SerializeToElement(new { flowRate = _flowSetpoint, unit = "mL/min" }))));

        // SetTemperature — Numeric parameter
        builder.RegisterSimple(
            "SetTemperature",
            "Set the column oven temperature (°C)",
            ct =>
            {
                Logger.LogInformation("Simulator: SetTemperature called (temp={Temp})", _temperature);
                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // SetPumpMode — Enum parameter
        builder.RegisterSimple(
            "SetPumpMode",
            "Set the pump operating mode",
            ct =>
            {
                Logger.LogInformation("Simulator: SetPumpMode called (mode={Mode})", _pumpMode);
                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // Start
        builder.RegisterSimple(
            "Start",
            "Start the simulator device",
            ct => Task.FromResult(CapabilityInvocationResult.Successful()));

        // Stop
        builder.RegisterSimple(
            "Stop",
            "Stop the simulator device",
            ct => Task.FromResult(CapabilityInvocationResult.Successful()));
    }

    /// <summary>
    /// Returns CapabilityDescriptor records with full ParameterSchema metadata
    /// so the WorkflowBuilder DeviceAction node renders the correct input widgets.
    /// </summary>
    public override Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    /// <summary>Applies the sole Phase 01 mutable simulator capability after durable admission.</summary>
    public Task ApplySetFlowAsync(Guid commandId, double flowRate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!double.IsFinite(flowRate) || flowRate < 0 || flowRate > 10)
            throw new ArgumentOutOfRangeException(nameof(flowRate));
        lock (this)
        {
            if (_lastSetFlowCommandId == commandId)
                return Task.CompletedTask;
            _flowSetpoint = flowRate;
            _lastSetFlowCommandId = commandId;
        }
        Logger.LogInformation("Simulator: committed SetFlow {Setpoint} for command {CommandId}", flowRate, commandId);
        return Task.CompletedTask;
    }

    public IReadOnlyList<CoreCapabilityDescriptor> GetRichCapabilities() =>
    [
        new CoreCapabilityDescriptor(
            "SetFlow",
            "Set Flow",
            "Set the pump flow rate",
            CoreCapabilityCategory.Control,
            [
                new CoreParameterSchema("flowRate", "Flow Rate", CoreParameterType.Numeric, "mL/min",
                    Min: 0, Max: 10, Step: 0.1, Options: null, DefaultValue: 1.0)
            ],
            IsDestructive: false,
            RequiresActiveRunspace: false,
            EstimatedDurationMs: 500),

        new CoreCapabilityDescriptor(
            "GetFlow",
            "Get Flow",
            "Read back the current flow setpoint",
            CoreCapabilityCategory.Measurement,
            [],
            IsDestructive: false,
            RequiresActiveRunspace: false,
            EstimatedDurationMs: 200,
            Returns:
            [
                new CoreParameterSchema("flowRate", "Flow Rate", CoreParameterType.Numeric, "mL/min",
                    Min: 0, Max: 10, Step: 0.1, Options: null, DefaultValue: null)
            ]),

        new CoreCapabilityDescriptor(
            "SetTemperature",
            "Set Temperature",
            "Set the column oven temperature",
            CoreCapabilityCategory.Control,
            [
                new CoreParameterSchema("temperature", "Temperature", CoreParameterType.Numeric, "°C",
                    Min: 20, Max: 80, Step: 1, Options: null, DefaultValue: 40.0)
            ],
            IsDestructive: false,
            RequiresActiveRunspace: false,
            EstimatedDurationMs: 3000),

        new CoreCapabilityDescriptor(
            "SetPumpMode",
            "Set Pump Mode",
            "Set the pump operating mode",
            CoreCapabilityCategory.Control,
            [
                new CoreParameterSchema("mode", "Mode", CoreParameterType.Enum, "",
                    Min: null, Max: null, Step: null,
                    Options: ["Isocratic", "Gradient", "Purge"],
                    DefaultValue: "Isocratic")
            ],
            IsDestructive: false,
            RequiresActiveRunspace: false),

        new CoreCapabilityDescriptor(
            "Start",
            "Start",
            "Start the simulator device",
            CoreCapabilityCategory.Control,
            [],
            IsDestructive: false,
            RequiresActiveRunspace: false,
            EstimatedDurationMs: 1000),

        new CoreCapabilityDescriptor(
            "Stop",
            "Stop",
            "Stop the simulator device",
            CoreCapabilityCategory.Control,
            [],
            IsDestructive: false,
            RequiresActiveRunspace: false,
            EstimatedDurationMs: 500),
    ];

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            yield return new TrendSample { TrendName = "simulator.flowRate", Value = _flowSetpoint, Timestamp = now, Unit = "mL/min" };
            yield return new TrendSample { TrendName = "simulator.temperature", Value = _temperature + (Random.Shared.NextDouble() - 0.5) * 0.2, Timestamp = now, Unit = "°C" };

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
