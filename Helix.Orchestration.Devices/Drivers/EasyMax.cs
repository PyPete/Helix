using System.Runtime.CompilerServices;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Transports.Opc;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Mettler-Toledo EasyMax reaction platform.
/// Communicates via OPC-UA using the Mettler IMettlerOpcCommand abstraction.
/// OpcName: "iControl" (BaseDeviceOpcName in POP.DeviceCore.Drivers.EasyMax).
/// </summary>
public sealed class EasyMax : DeviceAdapterBase
{
    private readonly IMettlerOpcCommand _opc;

    // Active system (experiment) selected by CreateSubscription
    private string _activeSystem = string.Empty;

    /// <summary>
    /// Initializes a new instance of <see cref="EasyMax"/> for production use.
    /// </summary>
    /// <param name="transport">The OPC-UA transport (stub; required by DeviceAdapterBase).</param>
    /// <param name="opc">The Mettler OPC command interface.</param>
    /// <param name="logger">Logger instance.</param>
    public EasyMax(OpcDeviceTransport transport, IMettlerOpcCommand opc, ILogger<EasyMax> logger)
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
    internal EasyMax(IDeviceTransport transport, IMettlerOpcCommand opc, ILogger<EasyMax> logger)
        : base(transport, logger)
    {
        _opc = opc;
    }

    // ── Capability registration ───────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // 1. FindActiveSystems — discover active iControl experiments
        builder.RegisterSimple(
            "FindActiveSystems",
            "Discover active experiments connected to the EasyMax iControl OPC server",
            ct =>
            {
                var experiments = _opc.ConnectedDevices("ns=2;s=Local.iControl", "Experiment");

                var activeSystems = experiments
                    .Where(DeviceReal)
                    .ToList();

                return Task.FromResult(
                    CapabilityInvocationResult.Successful(
                        JsonSerializer.SerializeToElement(activeSystems)));
            });

        // 2. CreateSubscription — select an experiment system for trend polling
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "CreateSubscription",
                "Select an active iControl experiment by name for trend data polling",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"systemName\":{\"type\":\"string\"}},\"required\":[\"systemName\"]}"),
            (parameters, _, ct) =>
            {
                var systemName = parameters.GetProperty("systemName").GetString() ?? string.Empty;

                if (!DeviceReal(systemName))
                    return Task.FromResult(CapabilityInvocationResult.Failed("System is not live or connected"));

                _activeSystem = systemName;

                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // 3. IsDevice — check OPC connection status
        builder.RegisterSimple(
            "IsDevice",
            "Check whether the EasyMax iControl OPC server is active and connected",
            ct =>
            {
                var isConnected = _opc.GetAtNode<bool>("ns=2;Local.iControl.ConnectionStatus");
                return Task.FromResult(
                    isConnected
                        ? CapabilityInvocationResult.Successful()
                        : CapabilityInvocationResult.Failed("EasyMax software is not active"));
            });

        // 4. GetTrendValues — read current trend values from the active experiment
        builder.RegisterSimple(
            "GetTrendValues",
            "Read current trend values (temperature, stirrer speed, etc.) from the active EasyMax experiment",
            ct =>
            {
                if (string.IsNullOrEmpty(_activeSystem))
                    return Task.FromResult(CapabilityInvocationResult.Failed("No active system selected; call CreateSubscription first"));

                var trends = _opc.GetTrendsAtNode($"ns=2;s=Local.iControl.{_activeSystem}.Trends");

                var result = trends.Select(t => new { name = t.name, value = t.value, unit = t.unit }).ToList();

                return Task.FromResult(
                    CapabilityInvocationResult.Successful(
                        JsonSerializer.SerializeToElement(result)));
            });
    }

    // ── Trend streaming ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!string.IsNullOrEmpty(_activeSystem))
            {
                List<(string name, double value, string unit)>? trends = null;
                try
                {
                    trends = _opc.GetTrendsAtNode($"ns=2;s=Local.iControl.{_activeSystem}.Trends");
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "EasyMax: failed to poll trends for system '{System}'; skipping", _activeSystem);
                }

                if (trends is not null)
                {
                    var now = DateTimeOffset.UtcNow;
                    foreach (var (name, value, unit) in trends)
                    {
                        yield return new TrendSample
                        {
                            TrendName = $"easymax.{_activeSystem}.{name}",
                            Value = value,
                            Timestamp = now,
                            Unit = string.IsNullOrEmpty(unit) ? null : unit
                        };
                    }
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Determines whether an iControl experiment is live (has a valid experiment name and is not BadOutOfService).
    /// Matches POP.DeviceCore.Drivers.EasyMax.DeviceReal logic exactly.
    /// </summary>
    private bool DeviceReal(string experiment)
    {
        var experimentName = _opc.GetAtNode<string>($"ns=2;Local.iControl.{experiment}.ExperimentName");
        var documentName = _opc.GetAtNode<string>($"ns=2;Local.iControl.{experiment}.DocumentName");

        if (string.IsNullOrEmpty(experimentName) || experimentName == "BadOutOfService")
        {
            return documentName != "BadOutOfService" && !string.IsNullOrEmpty(documentName);
        }
        return true;
    }
}
