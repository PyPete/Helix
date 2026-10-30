using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Transports.Opc;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Mettler-Toledo FBRM (Focused Beam Reflectance Measurement) probe.
/// Communicates via OPC-UA using the Mettler IMettlerOpcCommand abstraction.
/// OpcName: "iCFBRM" (BaseDeviceOpcName in POP.DeviceCore.Drivers.Fbrm).
/// has_trends=NO — StreamTrendsAsync is not overridden.
/// </summary>
public sealed class Fbrm : DeviceAdapterBase
{
    private readonly IMettlerOpcCommand _opc;

    // Active probe selected by CreateSubscription
    private string _activeProbe = string.Empty;

    /// <summary>
    /// Initializes a new instance of <see cref="Fbrm"/> for production use.
    /// </summary>
    /// <param name="transport">The OPC-UA transport (stub; required by DeviceAdapterBase).</param>
    /// <param name="opc">The Mettler OPC command interface.</param>
    /// <param name="logger">Logger instance.</param>
    public Fbrm(OpcDeviceTransport transport, IMettlerOpcCommand opc, ILogger<Fbrm> logger)
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
    internal Fbrm(IDeviceTransport transport, IMettlerOpcCommand opc, ILogger<Fbrm> logger)
        : base(transport, logger)
    {
        _opc = opc;
    }

    // ── Capability registration ───────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // 1. FindActiveSystems — discover active iCFBRM probes
        builder.RegisterSimple(
            "FindActiveSystems",
            "Discover active probes connected to the FBRM iCFBRM OPC server",
            ct =>
            {
                var probes = _opc.ConnectedDevices("ns=2;s=Local.iCFBRM", "Experiment", false);

                var activeList = probes.Where(DeviceReal).ToList();

                return Task.FromResult(
                    CapabilityInvocationResult.Successful(
                        JsonSerializer.SerializeToElement(activeList)));
            });

        // 2. CreateSubscription — select a probe for monitoring
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "CreateSubscription",
                "Select an active FBRM probe by name for data monitoring",
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
            "Check whether the FBRM iCFBRM OPC server is active and connected",
            ct =>
            {
                var isConnected = _opc.GetAtNode<bool>("ns=2;Local.iCFBRM.ConnectionStatus");
                return Task.FromResult(
                    isConnected
                        ? CapabilityInvocationResult.Successful()
                        : CapabilityInvocationResult.Failed("FBRM software is not active"));
            });
    }

    // Note: StreamTrendsAsync is NOT overridden (has_trends=NO per RESEARCH.md).
    // The base class empty implementation is correct.

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Determines whether an iCFBRM probe is live (connection active and has a valid experiment name).
    /// Matches POP.DeviceCore.Drivers.Fbrm.DeviceReal logic exactly.
    /// </summary>
    private bool DeviceReal(string deviceName)
    {
        var result = _opc.GetAtNode<bool>("ns=2;Local.iCFBRM.ConnectionStatus");
        if (!result) return false;

        var experimentName = _opc.GetAtNode<string>($"ns=2;Local.iCFBRM.{deviceName}.ExperimentName");

        return !string.IsNullOrEmpty(experimentName) && experimentName != "BadOutOfService";
    }
}
