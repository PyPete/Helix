using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Carino C09 high pressure pump.
/// Serial protocol, BaudRate 9600, command terminator \r\n.
/// Flow rates in ul/min (divide by 1000 for ml/min). Limits: [0,10] ml/min.
/// </summary>
public sealed class Carino9 : DeviceAdapterBase
{
    private readonly double[] _availableLimits = [0, 10];

    public Carino9(SerialDeviceTransport transport, ILogger<Carino9> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start — "ON\r\n" → "START:OK"
        builder.RegisterSimple(
            "Start",
            "Start the Carino C09 pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("ON\r\n"), ct));
                if (resp.Contains("START:OK"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Start may not have been successful");
            });

        // Stop — "OFF\r\n" → "STOP:OK"
        builder.RegisterSimple(
            "Stop",
            "Stop the Carino C09 pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("OFF\r\n"), ct));
                if (resp.Contains("STOP:OK"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Stop may not have been successful");
            });

        // SetPoint — "FLOW:{ul/min}\r\n" → "FLOW:OK"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetPoint",
                "Set the flow rate setpoint in ml/min (0-10)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"setpoint\":{\"type\":\"number\",\"minimum\":0,\"maximum\":10}},\"required\":[\"setpoint\"]}"),
            async (parameters, _, ct) =>
            {
                var setpoint = parameters.GetProperty("setpoint").GetDouble();
                if (setpoint < _availableLimits[0] || setpoint > _availableLimits[1])
                    return CapabilityInvocationResult.Failed("Set point is outside of bounds");
                var convertedRate = setpoint * 1000;
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"FLOW:{convertedRate}\r\n"), ct));
                if (resp.Contains("FLOW:OK"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Setpoint may not have been successful");
            });

        // GetSetPoint — "FLOW?\r\n" → "FLOW:1000"
        builder.RegisterSimple(
            "GetSetPoint",
            "Get the current flow rate setpoint in ml/min",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("FLOW?\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Command returned empty");
                if (!resp.Contains("FLOW"))
                    return CapabilityInvocationResult.Failed("Command not returned correctly");
                var parts = resp.Split(':');
                if (parts.Length < 2 || !double.TryParse(parts[1], out var flowAsDbl))
                    return CapabilityInvocationResult.Failed("Set point did not read correctly");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(flowAsDbl / 1000.0));
            });

        // GetPressure — "P?\r\n" → "P:1"
        builder.RegisterSimple(
            "GetPressure",
            "Get the current pressure in bar",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("P?\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Command returned empty");
                if (!resp.Contains("P"))
                    return CapabilityInvocationResult.Failed("Command not returned correctly");
                var parts = resp.Split(':');
                if (parts.Length < 2 || !double.TryParse(parts[1], out var pressure))
                    return CapabilityInvocationResult.Failed("Pressure did not read correctly");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(pressure));
            });

        // GetSetPointLimits
        builder.RegisterSimple(
            "GetSetPointLimits",
            "Get the min and max flow setpoint limits for the Carino C09",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { min = _availableLimits[0], max = _availableLimits[1] }))));
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            string? pressResp = null;
            try { pressResp = Decode(await Transport.SendReceiveAsync(Encode("P?\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "Carino9: failed to poll P?"); }

            if (!string.IsNullOrEmpty(pressResp) && pressResp.Contains("P"))
            {
                var parts = pressResp.Split(':');
                if (parts.Length >= 2 && double.TryParse(parts[1], out var pressure))
                    yield return new TrendSample { TrendName = "carino9.pressure", Value = pressure, Timestamp = now, Unit = "bar" };
            }

            string? flowResp = null;
            try { flowResp = Decode(await Transport.SendReceiveAsync(Encode("FLOW?\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "Carino9: failed to poll FLOW?"); }

            if (!string.IsNullOrEmpty(flowResp) && flowResp.Contains("FLOW"))
            {
                var parts = flowResp.Split(':');
                if (parts.Length >= 2 && double.TryParse(parts[1], out var flow))
                    yield return new TrendSample { TrendName = "carino9.flow", Value = flow / 1000.0, Timestamp = now, Unit = "ml/min" };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
