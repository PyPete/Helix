using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Arduino Temperature sensor (K-type thermocouple).
/// Serial protocol, BaudRate 9600, no command terminator.
/// </summary>
public sealed class ArduinoTemp : DeviceAdapterBase
{
    public ArduinoTemp(SerialDeviceTransport transport, ILogger<ArduinoTemp> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // GetTemp
        builder.RegisterSimple(
            "GetTemp",
            "Get the current temperature reading from the Arduino temperature sensor",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("TP"), ct));
                if (resp == null || resp == "?")
                    return CapabilityInvocationResult.Failed("Respond returned incorrectly");
                if (!double.TryParse(resp, out var value))
                    return CapabilityInvocationResult.Failed($"Unexpected response: {resp}");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(value));
            });

        // GetUniqueId
        builder.RegisterSimple(
            "GetUniqueId",
            "Get the serial number of the Arduino temperature sensor",
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
            try { resp = Decode(await Transport.SendReceiveAsync(Encode("TP"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "ArduinoTemp: failed to poll TP"); }

            if (!string.IsNullOrEmpty(resp) && resp != "?" && double.TryParse(resp, out var value))
                yield return new TrendSample { TrendName = "arduinotemp.temperature", Value = value, Timestamp = now, Unit = "C" };

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
