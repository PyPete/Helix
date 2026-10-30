using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Arduino pH probe.
/// Serial protocol, BaudRate 9600, no command terminator.
/// </summary>
public sealed class ArduinopH : DeviceAdapterBase
{
    public ArduinopH(SerialDeviceTransport transport, ILogger<ArduinopH> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // EnterCalibration
        builder.RegisterSimple(
            "EnterCalibration",
            "Enter calibration mode on the Arduino pH probe",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("EP"), ct));
                if (string.IsNullOrEmpty(resp) || !resp.Contains("Calibration Mode"))
                    return CapabilityInvocationResult.Failed("Calibration did not enter successfully");
                return CapabilityInvocationResult.Successful();
            });

        // ExitCalibration
        builder.RegisterSimple(
            "ExitCalibration",
            "Exit calibration mode on the Arduino pH probe",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("XP"), ct));
                if (string.IsNullOrEmpty(resp) || !resp.Contains("Successful"))
                    return CapabilityInvocationResult.Failed("Calibration did not exit successfully");
                return CapabilityInvocationResult.Successful();
            });

        // Calibrate
        builder.RegisterSimple(
            "Calibrate",
            "Calibrate the Arduino pH probe with buffer solution",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("CB"), ct));
                if (string.IsNullOrEmpty(resp) || !resp.Contains("Buffer Solution"))
                    return CapabilityInvocationResult.Failed("Calibration did not exit successfully");
                return CapabilityInvocationResult.Successful();
            });

        // GetReading
        builder.RegisterSimple(
            "GetReading",
            "Get a pH reading from the Arduino pH probe",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("MP"), ct));
                if (!double.TryParse(resp, out var value))
                    return CapabilityInvocationResult.Failed("Arduino conductivity did not return correctly");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(value));
            });

        // GetUniqueId
        builder.RegisterSimple(
            "GetUniqueId",
            "Get the serial number of the Arduino pH probe",
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
            try { resp = Decode(await Transport.SendReceiveAsync(Encode("MP"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "ArduinopH: failed to poll MP"); }

            if (!string.IsNullOrEmpty(resp) && double.TryParse(resp, out var value))
                yield return new TrendSample { TrendName = "arduinoph.ph", Value = value, Timestamp = now, Unit = "" };

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
