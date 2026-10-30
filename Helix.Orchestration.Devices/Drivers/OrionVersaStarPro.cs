using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Orion Versa Star Pro pH/conductivity meter.
/// Serial protocol, BaudRate 115200, command terminator \r.
/// GetTrends: SETCHANNEL 1, SETREADTYPE 1, 4, GETMEAS (→ parse pH).
/// </summary>
public sealed class OrionVersaStarPro : DeviceAdapterBase
{
    private string _uniqueId = string.Empty;

    public OrionVersaStarPro(SerialDeviceTransport transport, ILogger<OrionVersaStarPro> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Stop — "STOP\r" → contains "ST"
        builder.RegisterSimple(
            "Stop",
            "Stop continuous measurement from the Orion Versa Star Pro",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("STOP\r"), ct));
                if (string.IsNullOrEmpty(resp) || !resp.Contains("ST"))
                    return CapabilityInvocationResult.Failed("Set read type did not return correctly");
                return CapabilityInvocationResult.Successful();
            });

        // GetMeasurement — SETCHANNEL 1 + SETREADTYPE 1, 4 + GETMEAS
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "GetMeasurement",
                "Get a single measurement from the Orion Versa Star Pro",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"channel\":{\"type\":\"integer\",\"minimum\":1,\"default\":1}},\"required\":[]}"),
            async (parameters, _, ct) =>
            {
                var channel = parameters.TryGetProperty("channel", out var ch) ? ch.GetInt32() : 1;
                var setChResp = Decode(await Transport.SendReceiveAsync(Encode($"SETCHANNEL {channel}\r"), ct));
                if (string.IsNullOrEmpty(setChResp) || !setChResp.Contains("SE"))
                    return CapabilityInvocationResult.Failed("Set channel did not work correctly");
                await Transport.SendReceiveAsync(Encode($"SETREADTYPE {channel}, 4\r"), ct);
                var meas = Decode(await Transport.SendReceiveAsync(Encode("GETMEAS\r"), ct));
                if (string.IsNullOrEmpty(meas)) return CapabilityInvocationResult.Failed("No measurement returned");
                var parts = meas.Split(',');
                var phIdx = Array.IndexOf(parts, "PH");
                if (phIdx == -1) return CapabilityInvocationResult.Failed("pH value not found");
                if (!double.TryParse(parts[phIdx + 1], out var ph)) return CapabilityInvocationResult.Failed("Could not parse pH");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(ph));
            });

        // GetSystemInfo — "SYSTEM\r"
        builder.RegisterSimple(
            "GetSystemInfo",
            "Get system info from the Orion Versa Star Pro",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("SYSTEM\r"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("No response from device");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(resp));
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            string? ph = null;
            try
            {
                var setChResp = Decode(await Transport.SendReceiveAsync(Encode("SETCHANNEL 1\r"), ct));
                if (!string.IsNullOrEmpty(setChResp) && setChResp.Contains("SE"))
                {
                    await Transport.SendReceiveAsync(Encode("SETREADTYPE 1, 4\r"), ct);
                    var meas = Decode(await Transport.SendReceiveAsync(Encode("GETMEAS\r"), ct));
                    if (!string.IsNullOrEmpty(meas))
                    {
                        var parts = meas.Split(',');
                        var phIdx = Array.IndexOf(parts, "PH");
                        if (phIdx >= 0 && phIdx + 1 < parts.Length)
                            ph = parts[phIdx + 1];
                    }
                }
            }
            catch (Exception ex) { Logger.LogWarning(ex, "OrionVersaStarPro: failed to poll measurement"); }

            if (!string.IsNullOrEmpty(ph) && double.TryParse(ph, out var phVal))
                yield return new TrendSample { TrendName = "orionversastarpro.ph", Value = phVal, Timestamp = now, Unit = "" };

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
