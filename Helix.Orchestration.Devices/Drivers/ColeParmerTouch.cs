using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Cole Parmer Touch syringe pump.
/// Serial protocol, BaudRate 115200, command terminator \r.
/// Note: POP test file has all tests commented out — capabilities ported from POP source only.
/// </summary>
public sealed class ColeParmerTouch : DeviceAdapterBase
{
    public ColeParmerTouch(SerialDeviceTransport transport, ILogger<ColeParmerTouch> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start (infusion) — "0irun\r"
        builder.RegisterSimple(
            "StartInfusion",
            "Start infusion on the Cole Parmer Touch syringe pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("0irun\r"), ct));
                if (resp.Contains("T*"))
                    return CapabilityInvocationResult.Failed("Start is already completed");
                if (!resp.Contains("00>"))
                    return CapabilityInvocationResult.Failed("Not started properly");
                return CapabilityInvocationResult.Successful();
            });

        // Stop — "0stop\r"
        builder.RegisterSimple(
            "Stop",
            "Stop the Cole Parmer Touch syringe pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("0stop\r"), ct));
                return CapabilityInvocationResult.Successful();
            });

        // GetInfusedVolume — "0ivolume\r"
        builder.RegisterSimple(
            "GetInfusedVolume",
            "Get the cumulative infused volume from the Cole Parmer Touch",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("0ivolume\r"), ct));
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

            string? resp = null;
            try { resp = Decode(await Transport.SendReceiveAsync(Encode("0ivolume\r"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "ColeParmerTouch: failed to poll 0ivolume"); }

            if (!string.IsNullOrEmpty(resp))
                yield return new TrendSample { TrendName = "coleparmertouch.infusedvolume", Value = resp, Timestamp = now, Unit = null };

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
