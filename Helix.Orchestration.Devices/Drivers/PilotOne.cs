using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Huber Pilot One heater/chiller.
/// Serial protocol, BaudRate 9600, command terminator \r\n.
/// PB commands: {MttvvvvCRLF (10 chars total).
/// Start: {M140001\r\n, Stop: {M140000\r\n. Response: {S...
/// </summary>
public sealed class PilotOne : DeviceAdapterBase
{
    private const string MasterRequest = "{M";
    private const string SlaveResponse = "{S";
    private const string QueryCmd = "****";
    private readonly double[] _availableLimits = [-151.0, 326.67];

    public PilotOne(SerialDeviceTransport transport, ILogger<PilotOne> logger)
        : base(transport, logger) { }

    private static byte[] EncodeCmd(string cmd) => Encoding.Default.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start — "{M140001\r\n"
        builder.RegisterSimple(
            "Start",
            "Start temperature control on the Huber Pilot One",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(EncodeCmd($"{MasterRequest}14{1:X4}\r\n"), ct));
                if (resp.StartsWith(SlaveResponse))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Huber command to start did not return correctly");
            });

        // Stop — "{M140000\r\n"
        builder.RegisterSimple(
            "Stop",
            "Stop temperature control on the Huber Pilot One",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(EncodeCmd($"{MasterRequest}14{0:X4}\r\n"), ct));
                if (resp.StartsWith(SlaveResponse))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Huber command to stop did not return correctly");
            });

        // SetPoint — "{M00{temp:X4}\r\n" (setpoint address 00)
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetPoint",
                "Set the temperature setpoint on the Huber Pilot One",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"celsius\":{\"type\":\"number\",\"minimum\":-151,\"maximum\":326.67}},\"required\":[\"celsius\"]}"),
            async (parameters, _, ct) =>
            {
                var celsius = parameters.GetProperty("celsius").GetDouble();
                if (celsius < _availableLimits[0] || celsius > _availableLimits[1])
                    return CapabilityInvocationResult.Failed("Temperature setpoint outside of limits");
                var tempHex = (int)(celsius * 100);
                var resp = Decode(await Transport.SendReceiveAsync(EncodeCmd($"{MasterRequest}00{tempHex:X4}\r\n"), ct));
                if (resp.StartsWith(SlaveResponse))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Setpoint may not have been set correctly");
            });

        // GetInternalTemp — "{M01****\r\n" (query with ****)
        builder.RegisterSimple(
            "GetInternalTemp",
            "Get the internal temperature from the Huber Pilot One",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(EncodeCmd($"{MasterRequest}01{QueryCmd}\r\n"), ct));
                if (!resp.StartsWith(SlaveResponse) || resp.Length < 7)
                    return CapabilityInvocationResult.Failed("Internal temperature did not return correctly");
                if (int.TryParse(resp.Substring(3, 4), System.Globalization.NumberStyles.HexNumber, null, out var raw))
                    return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(raw / 100.0));
                return CapabilityInvocationResult.Failed("Could not parse temperature response");
            });

        // GetSetPointLimits
        builder.RegisterSimple(
            "GetSetPointLimits",
            "Get the min and max temperature setpoint limits for the Huber Pilot One",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { min = _availableLimits[0], max = _availableLimits[1] }))));
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            // Internal temperature
            string? intResp = null;
            try { intResp = Decode(await Transport.SendReceiveAsync(EncodeCmd($"{MasterRequest}01{QueryCmd}\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "PilotOne: failed to poll internal temperature"); }

            if (!string.IsNullOrEmpty(intResp) && intResp.StartsWith(SlaveResponse) && intResp.Length >= 7)
            {
                if (int.TryParse(intResp.Substring(3, 4), System.Globalization.NumberStyles.HexNumber, null, out var raw))
                    yield return new TrendSample { TrendName = "pilotone.internaltemperature", Value = raw / 100.0, Timestamp = now, Unit = "C" };
            }

            // Setpoint
            string? spResp = null;
            try { spResp = Decode(await Transport.SendReceiveAsync(EncodeCmd($"{MasterRequest}00{QueryCmd}\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "PilotOne: failed to poll setpoint"); }

            if (!string.IsNullOrEmpty(spResp) && spResp.StartsWith(SlaveResponse) && spResp.Length >= 7)
            {
                if (int.TryParse(spResp.Substring(3, 4), System.Globalization.NumberStyles.HexNumber, null, out var raw))
                    yield return new TrendSample { TrendName = "pilotone.setpointtemperature", Value = raw / 100.0, Timestamp = now, Unit = "C" };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
