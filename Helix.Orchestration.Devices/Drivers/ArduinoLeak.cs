using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Arduino Leak detector.
/// Serial protocol, BaudRate 9600, no command terminator.
/// </summary>
public sealed class ArduinoLeak : DeviceAdapterBase
{
    public ArduinoLeak(SerialDeviceTransport transport, ILogger<ArduinoLeak> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // GetLeak
        builder.RegisterSimple(
            "GetLeak",
            "Get the leak status from the Arduino Leak detector (0=OK, 1=BAD)",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("LK"), ct));
                if (string.IsNullOrEmpty(resp) || resp == "?")
                    return CapabilityInvocationResult.Failed("Arduino leak did not return correctly");
                double value = resp switch
                {
                    "OK" => 0,
                    "BAD" => 1,
                    _ => -1
                };
                if (value < 0)
                    return CapabilityInvocationResult.Failed("Arduino leak could not interpret response");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(value));
            });

        // GetUniqueId (serial number)
        builder.RegisterSimple(
            "GetUniqueId",
            "Get the serial number of the Arduino Leak detector",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("SR"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Serial number returned incorrectly");
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
            try { resp = Decode(await Transport.SendReceiveAsync(Encode("LK"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "ArduinoLeak: failed to poll LK"); }

            if (!string.IsNullOrEmpty(resp) && resp != "?")
            {
                double value = resp switch
                {
                    "OK" => 0,
                    "BAD" => 1,
                    _ => -1
                };
                if (value >= 0)
                    yield return new TrendSample { TrendName = "arduinoleak.status", Value = value, Timestamp = now, Unit = "" };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
