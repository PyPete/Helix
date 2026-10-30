using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the New Era 1000 syringe pump.
/// Serial protocol, BaudRate 19200, command terminator \r.
/// Commands end with " \r" (space before CR). Responses wrapped in STX/ETX.
/// Note: In POP, NewEra1000 delegates to NewEraBaseMethods helper class.
/// In Helix, the logic from NewEraBaseMethods is implemented directly here (no helper class).
/// </summary>
public sealed class NewEra1000 : DeviceAdapterBase
{
    public NewEra1000(SerialDeviceTransport transport, ILogger<NewEra1000> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    private static bool ParseResponse(string resp)
    {
        if (string.IsNullOrEmpty(resp)) return false;
        return resp.Contains("S") || resp.Contains("P") || resp.Contains("I");
    }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start — "RUN \r" → response contains "I" for infusing
        builder.RegisterSimple(
            "Start",
            "Start infusion on the New Era 1000 syringe pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("RUN \r"), ct));
                if (resp.Contains("I"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Start may not have executed correctly");
            });

        // Stop — "STP \r" twice; second response contains "S" for stopped
        builder.RegisterSimple(
            "Stop",
            "Stop the New Era 1000 syringe pump (call twice to fully stop from pause)",
            async ct =>
            {
                await Transport.SendReceiveAsync(Encode("STP \r"), ct);
                var resp = Decode(await Transport.SendReceiveAsync(Encode("STP \r"), ct));
                if (resp.Contains("S"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Device may not have stopped correctly");
            });

        // Pause — "STP \r" once; response contains "P"
        builder.RegisterSimple(
            "Pause",
            "Pause the New Era 1000 syringe pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("STP \r"), ct));
                if (resp.Contains("P"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Device may not have paused correctly");
            });

        // SetVolume — "VOL {vol} \r"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetVolume",
                "Set the volume to be dispensed on the New Era 1000 syringe pump",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"volume\":{\"type\":\"number\"}},\"required\":[\"volume\"]}"),
            async (parameters, _, ct) =>
            {
                var volume = parameters.GetProperty("volume").GetDouble();
                var truncVol = Math.Truncate(volume * 100) / 100.0;
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"VOL {truncVol} \r"), ct));
                if (ParseResponse(resp))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Set volume command did not execute successfully");
            });

        // SetRate — "RAT {rate} {units} \r"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetRate",
                "Set the pumping rate on the New Era 1000 syringe pump",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"rate\":{\"type\":\"number\"},\"units\":{\"type\":\"string\"}},\"required\":[\"rate\",\"units\"]}"),
            async (parameters, _, ct) =>
            {
                var rate = parameters.GetProperty("rate").GetDouble();
                var units = parameters.GetProperty("units").GetString() ?? "MM";
                var truncRate = Math.Truncate(rate * 100) / 100.0;
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"RAT {truncRate} {units} \r"), ct));
                if (ParseResponse(resp))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Set rate did not return correctly");
            });

        // SetDiameter — "DIA {diameter} \r"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetDiameter",
                "Set the syringe diameter on the New Era 1000",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"diameter\":{\"type\":\"number\"}},\"required\":[\"diameter\"]}"),
            async (parameters, _, ct) =>
            {
                var diameter = parameters.GetProperty("diameter").GetDouble();
                var truncDiam = Math.Truncate(diameter * 100) / 100.0;
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"DIA {truncDiam} \r"), ct));
                if (ParseResponse(resp))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Set diameter command did not execute successfully");
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            // Get rate — "* RAT \r"
            string? rateResp = null;
            try { rateResp = Decode(await Transport.SendReceiveAsync(Encode("* RAT \r"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "NewEra1000: failed to poll * RAT"); }

            if (!string.IsNullOrEmpty(rateResp) && ParseResponse(rateResp))
            {
                var stripped = rateResp.Replace("\x02", "").Replace("\x03", "");
                var split = stripped.Split('S', 'I', 'P');
                if (split.Length >= 2)
                {
                    var match = Regex.Match(split[1], @"[a-zA-Z]");
                    if (match.Success && double.TryParse(split[1][..match.Index], out var rate))
                        yield return new TrendSample { TrendName = "newera1000.transferrate", Value = rate, Timestamp = now, Unit = split[1].Substring(match.Index, Math.Min(2, split[1].Length - match.Index)) };
                }
            }

            // Get dispensed volume — "* DIS \r"
            string? disResp = null;
            try { disResp = Decode(await Transport.SendReceiveAsync(Encode("* DIS \r"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "NewEra1000: failed to poll * DIS"); }

            if (!string.IsNullOrEmpty(disResp) && ParseResponse(disResp))
            {
                var stripped = disResp.Replace("\x02", "").Replace("\x03", "");
                // Format: "00II0.020W0.000ML" - infused then withdrawn
                var iIdx = stripped.IndexOf("I", StringComparison.Ordinal);
                if (iIdx >= 0)
                {
                    var afterI = stripped[(iIdx + 1)..];
                    var match = Regex.Match(afterI, @"[a-zA-Z]");
                    if (match.Success && double.TryParse(afterI[..match.Index], out var vol))
                        yield return new TrendSample { TrendName = "newera1000.dispensedvolume", Value = vol, Timestamp = now, Unit = "ml" };
                }
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
