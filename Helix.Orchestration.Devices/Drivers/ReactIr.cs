using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Transports.Opc;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Mettler-Toledo React-IR spectroscopy probe.
/// Communicates via OPC-UA using the Mettler IMettlerOpcCommand abstraction.
/// OpcName: "iCIR" (BaseDeviceOpcName in POP.DeviceCore.Drivers.ReactIr).
/// has_trends=NO — StreamTrendsAsync is not overridden.
/// </summary>
public sealed class ReactIr : DeviceAdapterBase
{
    private readonly IMettlerOpcCommand _opc;

    // Active probe selected by CreateSubscription
    private string _activeProbe = string.Empty;

    /// <summary>
    /// Initializes a new instance of <see cref="ReactIr"/> for production use.
    /// </summary>
    /// <param name="transport">The OPC-UA transport (stub; required by DeviceAdapterBase).</param>
    /// <param name="opc">The Mettler OPC command interface.</param>
    /// <param name="logger">Logger instance.</param>
    public ReactIr(OpcDeviceTransport transport, IMettlerOpcCommand opc, ILogger<ReactIr> logger)
        : base(transport, logger)
    {
        _opc = opc;
    }

    /// <summary>
    /// Internal constructor for testing with a stub transport.
    /// </summary>
    /// <param name="transport">The device transport (may be a test double).</param>
    /// <param name="opc">The Mettler OPC command interface (may be a test double).</param>
    /// <param name="logger">Logger instance.</param>
    internal ReactIr(IDeviceTransport transport, IMettlerOpcCommand opc, ILogger<ReactIr> logger)
        : base(transport, logger)
    {
        _opc = opc;
    }

    // ── Capability registration ───────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // 1. FindActiveSystems — discover active iCIR probes
        builder.RegisterSimple(
            "FindActiveSystems",
            "Discover active probes connected to the React-IR iCIR OPC server",
            ct =>
            {
                var probes = _opc.ConnectedDevices("ns=2;s=Local.iCIR", "Probe");

                var activeList = probes.Where(DeviceReal).ToList();

                return Task.FromResult(
                    CapabilityInvocationResult.Successful(
                        JsonSerializer.SerializeToElement(activeList)));
            });

        // 2. CreateSubscription — select a probe for monitoring
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "CreateSubscription",
                "Select an active React-IR probe by name for data monitoring",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"probeName\":{\"type\":\"string\"}},\"required\":[\"probeName\"]}"),
            (parameters, _, ct) =>
            {
                var probeName = parameters.GetProperty("probeName").GetString() ?? string.Empty;

                if (!DeviceReal(probeName))
                    return Task.FromResult(CapabilityInvocationResult.Failed("Node name is not active"));

                _activeProbe = probeName;

                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // 3. IsDevice — check OPC connection status
        builder.RegisterSimple(
            "IsDevice",
            "Check whether the React-IR iCIR OPC server is active and connected",
            ct =>
            {
                var isConnected = _opc.GetAtNode<bool>("ns=2;Local.iCIR.ConnectionStatus");
                return Task.FromResult(
                    isConnected
                        ? CapabilityInvocationResult.Successful()
                        : CapabilityInvocationResult.Failed("React-IR software is not active"));
            });

        // 4. UpdateSamplingInterval — set the probe sampling interval in seconds
        // Corresponds to ReactIr.UpdateSamplingInterval(probe, newInterval) in POP source.
        // Node IDs from POP: ns=2;s=Local.iCIR.{probe}.Methods (objectId) and
        //                    ns=2;s=Local.iCIR.{probe}.Methods.SetSamplingInterval (methodId)
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "UpdateSamplingInterval",
                "Set the React-IR probe sampling interval in seconds",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"probeName\":{\"type\":\"string\"},\"intervalSeconds\":{\"type\":\"integer\",\"minimum\":1}},\"required\":[\"probeName\",\"intervalSeconds\"]}"),
            (parameters, _, ct) =>
            {
                var probeName = parameters.GetProperty("probeName").GetString() ?? _activeProbe;
                var newInterval = parameters.GetProperty("intervalSeconds").GetInt32();

                _opc.CallMethod(
                    $"ns=2;s=Local.iCIR.{probeName}.Methods",
                    $"ns=2;s=Local.iCIR.{probeName}.Methods.SetSamplingInterval",
                    newInterval);

                var actualInterval = _opc.GetAtNode<int>($"ns=2;Local.iCIR.{probeName}.CurrentSamplingInterval");

                return Task.FromResult(
                    newInterval == actualInterval
                        ? CapabilityInvocationResult.Successful()
                        : CapabilityInvocationResult.Failed("Sampling rate may not have updated"));
            });

        // 5. GetSamplingRate — read the current probe sampling interval
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "GetSamplingRate",
                "Read the current React-IR probe sampling interval in seconds",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"probeName\":{\"type\":\"string\"}},\"required\":[]}"),
            (parameters, _, ct) =>
            {
                var probeName = parameters.TryGetProperty("probeName", out var pn) && pn.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? pn.GetString() ?? _activeProbe
                    : _activeProbe;

                var interval = _opc.GetAtNode<int>($"ns=2;Local.iCIR.{probeName}.CurrentSamplingInterval");

                return Task.FromResult(
                    CapabilityInvocationResult.Successful(
                        JsonSerializer.SerializeToElement(new { probeName, intervalSeconds = interval })));
            });

        // 6. Pause — pause the active probe acquisition
        // TriggerCommand: objectId = ns=2;s=Local.iCIR.{probe}.Methods,
        //                 methodId = ns=2;s=Local.iCIR.{probe}.Methods.Pause
        // Then read ProbeStatus node to confirm "Paused"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "Pause",
                "Pause the React-IR probe acquisition; uses the active probe if probeName is omitted",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"probeName\":{\"type\":\"string\"}},\"required\":[]}"),
            (parameters, _, ct) =>
            {
                var probeName = parameters.TryGetProperty("probeName", out var pn) && pn.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? pn.GetString() ?? _activeProbe
                    : _activeProbe;

                _opc.CallMethod(
                    $"ns=2;s=Local.iCIR.{probeName}.Methods",
                    $"ns=2;s=Local.iCIR.{probeName}.Methods.Pause");

                var status = _opc.GetAtNode<string>($"ns=2;Local.iCIR.{probeName}.ProbeStatus");

                return Task.FromResult(
                    status.Equals("Paused", StringComparison.OrdinalIgnoreCase)
                        ? CapabilityInvocationResult.Successful()
                        : CapabilityInvocationResult.Failed("The probe may have not have paused"));
            });

        // 7. Resume — resume the active probe acquisition
        // TriggerCommand: objectId = ns=2;s=Local.iCIR.{probe}.Methods,
        //                 methodId = ns=2;s=Local.iCIR.{probe}.Methods.Resume
        // Then read ProbeStatus node to confirm "Running"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "Resume",
                "Resume the React-IR probe acquisition; uses the active probe if probeName is omitted",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"probeName\":{\"type\":\"string\"}},\"required\":[]}"),
            (parameters, _, ct) =>
            {
                var probeName = parameters.TryGetProperty("probeName", out var pn) && pn.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? pn.GetString() ?? _activeProbe
                    : _activeProbe;

                _opc.CallMethod(
                    $"ns=2;s=Local.iCIR.{probeName}.Methods",
                    $"ns=2;s=Local.iCIR.{probeName}.Methods.Resume");

                var status = _opc.GetAtNode<string>($"ns=2;Local.iCIR.{probeName}.ProbeStatus");

                return Task.FromResult(
                    status.Equals("Running", StringComparison.OrdinalIgnoreCase)
                        ? CapabilityInvocationResult.Successful()
                        : CapabilityInvocationResult.Failed("The probe may have not have resumed"));
            });
    }

    // Note: StreamTrendsAsync is NOT overridden (has_trends=NO per RESEARCH.md).
    // The base class empty implementation is correct.

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Determines whether a React-IR probe is live (connection active and valid experiment/document name).
    /// Matches POP.DeviceCore.Drivers.ReactIr.DeviceReal logic exactly.
    /// </summary>
    private bool DeviceReal(string deviceName)
    {
        var result = _opc.GetAtNode<bool>("ns=2;Local.iCIR.ConnectionStatus");
        if (!result) return false;

        var experimentName = _opc.GetAtNode<string>($"ns=2;Local.iCIR.{deviceName}.ExperimentName");
        var documentName = _opc.GetAtNode<string>($"ns=2;Local.iCIR.{deviceName}.DocumentName");

        if (string.IsNullOrEmpty(experimentName) || experimentName == "BadOutOfService")
        {
            return !string.IsNullOrEmpty(documentName);
        }
        return true;
    }
}
