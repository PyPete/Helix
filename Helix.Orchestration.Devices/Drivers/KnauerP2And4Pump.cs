using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Knauer P2 and P4 high-pressure pump.
/// Serial protocol, BaudRate 9600, command terminator \r (NOT \r\n — just \r).
/// Commands: "ON\r", "OFF\r", "FLOW:{ul/min}\r", "FLOW?\r", "PRESSURE?\r", "REMOTE\r".
/// Limits: [0.1, 10.0] ml/min.
/// </summary>
public sealed class KnauerP2And4Pump : DeviceAdapterBase
{
    private readonly double[] _availableLimits = [0.1, 10.0];

    public KnauerP2And4Pump(SerialDeviceTransport transport, ILogger<KnauerP2And4Pump> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start — "ON\r" → contains "OK"
        builder.RegisterSimple(
            "Start",
            "Start the Knauer P2/P4 pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("ON\r"), ct));
                if (!string.IsNullOrEmpty(resp) && resp.Contains("OK"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Start device may not have executed successfully");
            });

        // Stop — "OFF\r" → contains "OK"
        builder.RegisterSimple(
            "Stop",
            "Stop the Knauer P2/P4 pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("OFF\r"), ct));
                if (!string.IsNullOrEmpty(resp) && resp.Contains("OK"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Stop device may not have executed successfully");
            });

        // SetPoint — "FLOW:{ul/min}\r" → contains "OK"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetPoint",
                "Set the flow rate setpoint in ml/min (0.1-10.0)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"setpoint\":{\"type\":\"number\",\"minimum\":0.1,\"maximum\":10}},\"required\":[\"setpoint\"]}"),
            async (parameters, _, ct) =>
            {
                var setpoint = parameters.GetProperty("setpoint").GetDouble();
                var newFlow = (int)(setpoint * 1000.0);
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"FLOW:{newFlow}\r"), ct));
                if (!string.IsNullOrEmpty(resp) && resp.Contains("OK"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Setpoint did not execute correctly");
            });

        // GetSetPoint — "FLOW?\r" → "FLOW:5310"
        builder.RegisterSimple(
            "GetSetPoint",
            "Get the current flow rate setpoint in ml/min",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("FLOW?\r"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Get flow command returned empty");
                var parts = resp.Split(':');
                if (parts.Length < 2 || !double.TryParse(parts[1], out var flow))
                    return CapabilityInvocationResult.Failed("Flow did not read correctly");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(flow / 1000.0));
            });

        // GetPressure — "PRESSURE?\r" → "PRESSURE:5310"
        builder.RegisterSimple(
            "GetPressure",
            "Get the current pressure in bar",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("PRESSURE?\r"), ct));
                if (string.IsNullOrEmpty(resp) || resp.Contains("ERROR"))
                    return CapabilityInvocationResult.Failed("Pressure did not read correctly");
                var parts = resp.Split(':');
                if (parts.Length < 2 || !double.TryParse(parts[1], out var pressure))
                    return CapabilityInvocationResult.Failed("Pressure did not read correctly");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(pressure / 10.0 / 10.0));
            });

        // GetSetPointLimits
        builder.RegisterSimple(
            "GetSetPointLimits",
            "Get the min and max flow setpoint limits for the Knauer P2/P4 pump",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { min = _availableLimits[0], max = _availableLimits[1] }))));
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            string? flowResp = null;
            try { flowResp = Decode(await Transport.SendReceiveAsync(Encode("FLOW?\r"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "KnauerP2And4Pump: failed to poll FLOW?"); }
            if (!string.IsNullOrEmpty(flowResp))
            {
                var parts = flowResp.Split(':');
                if (parts.Length >= 2 && double.TryParse(parts[1], out var flow))
                    yield return new TrendSample { TrendName = "knauerp2and4pump.flow", Value = flow / 1000.0, Timestamp = now, Unit = "ml/min" };
            }

            string? pressResp = null;
            try { pressResp = Decode(await Transport.SendReceiveAsync(Encode("PRESSURE?\r"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "KnauerP2And4Pump: failed to poll PRESSURE?"); }
            if (!string.IsNullOrEmpty(pressResp) && !pressResp.Contains("ERROR"))
            {
                var parts = pressResp.Split(':');
                if (parts.Length >= 2 && double.TryParse(parts[1], out var pressure))
                    yield return new TrendSample { TrendName = "knauerp2and4pump.pressure", Value = pressure / 10.0 / 10.0, Timestamp = now, Unit = "bar" };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
