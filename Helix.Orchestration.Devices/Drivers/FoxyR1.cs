using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Foxy R1 fraction collector.
/// Serial protocol, BaudRate 19200, command terminator \r\n.
/// Trend: tube position (integer cast to double).
/// </summary>
public sealed class FoxyR1 : DeviceAdapterBase
{
    public FoxyR1(SerialDeviceTransport transport, ILogger<FoxyR1> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Stop — "STOP\r\n" → contains "STOP"
        builder.RegisterSimple(
            "Stop",
            "Stop the Foxy R1 fraction collector run",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("STOP\r\n"), ct));
                if (resp.Contains("STOP"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("STOP command may not have returned correctly");
            });

        // Collect — "COLLECT\r\n" → contains "COLLECT"
        builder.RegisterSimple(
            "Collect",
            "Switch the Foxy R1 to collection mode",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("COLLECT\r\n"), ct));
                if (resp.Contains("COLLECT"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Collect may not have executed correctly");
            });

        // Drain — "DRAIN\r\n" → contains "DRAIN"
        builder.RegisterSimple(
            "Drain",
            "Switch the Foxy R1 to drain (waste) mode",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("DRAIN\r\n"), ct));
                if (resp.Contains("DRAIN"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Drain may not have returned correctly");
            });

        // Home — "HOME\r\n" → contains "HOME"
        builder.RegisterSimple(
            "Home",
            "Move the Foxy R1 to home position",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("HOME\r\n"), ct));
                if (resp.Contains("HOME"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Home sample command may not have returned correctly");
            });

        // NextSample — "NEXTTUBE\r\n" → contains "NEXTTUBE"
        builder.RegisterSimple(
            "NextSample",
            "Advance the Foxy R1 to the next sample tube",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("NEXTTUBE\r\n"), ct));
                if (resp.Contains("NEXTTUBE"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Next sample command may not have returned correctly");
            });

        // Run — "RUN\r\n" → contains "RUN"; also sends "TUBE\r\n" to get position
        builder.RegisterSimple(
            "Run",
            "Start/continue a fraction collection run on the Foxy R1",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("RUN\r\n"), ct));
                var tubeResp = Decode(await Transport.SendReceiveAsync(Encode("TUBE\r\n"), ct));
                if (!tubeResp.Contains("="))
                    return CapabilityInvocationResult.Failed("Run command could not execute properly");
                if (resp.Contains("RUN"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Run command may not have returned correctly");
            });

        // SetToPosition — "TUBE={position}\r\n" → contains "TUBE"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetToPosition",
                "Set the Foxy R1 to a specific tube position",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"position\":{\"type\":\"integer\",\"minimum\":1}},\"required\":[\"position\"]}"),
            async (parameters, _, ct) =>
            {
                var position = parameters.GetProperty("position").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"TUBE={position}\r\n"), ct));
                if (resp.Contains("TUBE"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Set position command may not have returned correctly");
            });

        // GetCurrentPosition — "TUBE\r\n" → "TUBE=45"
        builder.RegisterSimple(
            "GetCurrentPosition",
            "Get the current tube position of the Foxy R1",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("TUBE\r\n"), ct));
                var parts = resp.Replace("\r\n", "").Split('=');
                if (parts.Length <= 1)
                    return CapabilityInvocationResult.Failed("Get current tube did not return correctly");
                if (!int.TryParse(parts[1], out var tube))
                    return CapabilityInvocationResult.Failed("Get current tube did not return correctly");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(tube));
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            string? resp = null;
            try { resp = Decode(await Transport.SendReceiveAsync(Encode("TUBE\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "FoxyR1: failed to poll TUBE"); }

            if (!string.IsNullOrEmpty(resp))
            {
                var parts = resp.Replace("\r\n", "").Split('=');
                if (parts.Length > 1 && int.TryParse(parts[1], out var tube))
                    yield return new TrendSample { TrendName = "foxyr1.position", Value = (double)tube, Timestamp = now, Unit = null };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
