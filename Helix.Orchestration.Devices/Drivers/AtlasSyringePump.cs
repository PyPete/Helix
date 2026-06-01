using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Atlas Syringe Pump (dual syringe).
/// Serial protocol, BaudRate 57600, command terminator \r\n.
/// Axis: Left=0, Right=1. Port: A=1, B=2, etc.
/// </summary>
public sealed class AtlasSyringePump : DeviceAdapterBase
{
    public AtlasSyringePump(SerialDeviceTransport transport, ILogger<AtlasSyringePump> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Remote — "A1\r\n" → returns "#A"
        builder.RegisterSimple(
            "Remote",
            "Put the Atlas Syringe Pump in remote (PC) control mode",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("A1\r\n"), ct));
                if (!resp.Contains("#A"))
                    return CapabilityInvocationResult.Failed("Remote command did not execute successfully");
                return CapabilityInvocationResult.Successful();
            });

        // Stop — "X {axis}\r\n"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "Stop",
                "Stop the specified pump axis (0=Left, 1=Right)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"axis\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":1}},\"required\":[\"axis\"]}"),
            async (parameters, _, ct) =>
            {
                var axis = parameters.GetProperty("axis").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"X {axis}\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Stop command may not have executed correctly");
                var parts = resp.Split(' ');
                if (resp.Contains('X') && parts.Length > 1 && parts[1].Contains('s'))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Stop command may not have executed correctly");
            });

        // Pause — "W {axis}\r\n"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "Pause",
                "Pause pumping on the specified axis",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"axis\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":1}},\"required\":[\"axis\"]}"),
            async (parameters, _, ct) =>
            {
                var axis = parameters.GetProperty("axis").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"W {axis}\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Pause command may not have executed correctly");
                var parts = resp.Split(' ');
                if (resp.Contains('W') && parts.Length > 1 && parts[1].Contains('s'))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Pause command may not have executed correctly");
            });

        // Resume — "U {axis}\r\n"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "Resume",
                "Resume pumping on the specified axis",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"axis\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":1}},\"required\":[\"axis\"]}"),
            async (parameters, _, ct) =>
            {
                var axis = parameters.GetProperty("axis").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"U {axis}\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Resume command may not have executed correctly");
                var parts = resp.Split(' ');
                if (resp.Contains('U') && parts.Length > 1 && parts[1].Contains('s'))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Resume command may not have executed correctly");
            });

        // Pump — "P{axis} {flowUl} {volumeUl} {fillPort} {emptyPort}\r\n"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "Pump",
                "Pump volume at flow rate from fill port to empty port",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"axis\":{\"type\":\"integer\"},\"flowRateMlMin\":{\"type\":\"number\"},\"volumeMl\":{\"type\":\"number\"},\"fillPort\":{\"type\":\"integer\"},\"emptyPort\":{\"type\":\"integer\"}},\"required\":[\"axis\",\"flowRateMlMin\",\"volumeMl\",\"fillPort\",\"emptyPort\"]}"),
            async (parameters, _, ct) =>
            {
                var axis = parameters.GetProperty("axis").GetInt32();
                var flowUl = (int)parameters.GetProperty("flowRateMlMin").GetDouble() * 1000;
                var volumeUl = (int)parameters.GetProperty("volumeMl").GetDouble() * 1000;
                var fillPort = parameters.GetProperty("fillPort").GetInt32();
                var emptyPort = parameters.GetProperty("emptyPort").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"P{axis} {flowUl} {volumeUl} {fillPort} {emptyPort}\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Pump may not have executed correctly");
                var parts = resp.Split(' ');
                if (resp.Contains('P') && parts.Length > 1 && parts[1].Contains('s'))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Pump may not have executed correctly");
            });

        // Fill — "F{axis} {flowUl} {port}\r\n"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "Fill",
                "Fill syringe from port at flow rate",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"axis\":{\"type\":\"integer\"},\"flowRateMlMin\":{\"type\":\"number\"},\"port\":{\"type\":\"integer\"}},\"required\":[\"axis\",\"flowRateMlMin\",\"port\"]}"),
            async (parameters, _, ct) =>
            {
                var axis = parameters.GetProperty("axis").GetInt32();
                var flowUl = (int)(parameters.GetProperty("flowRateMlMin").GetDouble() * 1000);
                var port = parameters.GetProperty("port").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"F{axis} {flowUl} {port}\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Fill may not have executed correctly");
                var parts = resp.Split(' ');
                if (resp.Contains('F') && parts.Length > 1 && parts[1].Contains('s'))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Fill may not have executed correctly");
            });

        // Empty — "E{axis} {flowUl} {port}\r\n"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "Empty",
                "Empty syringe through port at flow rate",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"axis\":{\"type\":\"integer\"},\"flowRateMlMin\":{\"type\":\"number\"},\"port\":{\"type\":\"integer\"}},\"required\":[\"axis\",\"flowRateMlMin\",\"port\"]}"),
            async (parameters, _, ct) =>
            {
                var axis = parameters.GetProperty("axis").GetInt32();
                var flowUl = (int)(parameters.GetProperty("flowRateMlMin").GetDouble() * 1000);
                var port = parameters.GetProperty("port").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"E{axis} {flowUl} {port}\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Empty may not have executed correctly");
                var parts = resp.Split(' ');
                if (resp.Contains('E') && parts.Length > 1 && parts[1].Contains('s'))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Empty may not have executed correctly");
            });

        // CheckIfPumpIdle — "S{axis}\r\n" → "#S{axis} 0 0 ..."
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "CheckIfPumpIdle",
                "Check if the pump axis is idle (state=0 means idle)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"axis\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":1}},\"required\":[\"axis\"]}"),
            async (parameters, _, ct) =>
            {
                var axis = parameters.GetProperty("axis").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"S{axis}\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Status returned incorrectly");
                var parts = resp.Split(' ');
                if (parts.Length < 3)
                    return CapabilityInvocationResult.Failed("Status returned incorrectly");
                if (!int.TryParse(parts[2], out var state))
                    return CapabilityInvocationResult.Failed("Status returned incorrectly");
                if (state == 0)
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Pump is still busy");
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            // Poll S0 and S1 for pump status
            string? statusA = null;
            try { statusA = Decode(await Transport.SendReceiveAsync(Encode("S0\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "AtlasSyringePump: failed to poll S0"); }

            if (!string.IsNullOrEmpty(statusA))
            {
                var parts = statusA.Split(' ');
                if (parts.Length >= 8)
                {
                    if (double.TryParse(parts[6], out var rateA))
                        yield return new TrendSample { TrendName = "atlassyringepump.transferrate.pumpA", Value = rateA / 1000.0, Timestamp = now, Unit = "ml/min" };
                    if (double.TryParse(parts[5], out var volA))
                        yield return new TrendSample { TrendName = "atlassyringepump.dispensedvolume.pumpA", Value = volA / 1000.0, Timestamp = now, Unit = "ml" };
                }
            }

            string? statusB = null;
            try { statusB = Decode(await Transport.SendReceiveAsync(Encode("S1\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "AtlasSyringePump: failed to poll S1"); }

            if (!string.IsNullOrEmpty(statusB))
            {
                var parts = statusB.Split(' ');
                if (parts.Length >= 8)
                {
                    if (double.TryParse(parts[6], out var rateB))
                        yield return new TrendSample { TrendName = "atlassyringepump.transferrate.pumpB", Value = rateB / 1000.0, Timestamp = now, Unit = "ml/min" };
                    if (double.TryParse(parts[5], out var volB))
                        yield return new TrendSample { TrendName = "atlassyringepump.dispensedvolume.pumpB", Value = volB / 1000.0, Timestamp = now, Unit = "ml" };
                }
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
