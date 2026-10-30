using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Arduino Conductivity probe.
/// Serial protocol, BaudRate 9600, no command terminator.
/// </summary>
public sealed class ArduinoConductivity : DeviceAdapterBase
{
    public ArduinoConductivity(SerialDeviceTransport transport, ILogger<ArduinoConductivity> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // EnterCalibration
        builder.RegisterSimple(
            "EnterCalibration",
            "Enter calibration mode on the Arduino Conductivity probe",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("EC"), ct));
                if (string.IsNullOrEmpty(resp) || !resp.Contains("Calibration Mode"))
                    return CapabilityInvocationResult.Failed("Calibration did not enter successfully");
                return CapabilityInvocationResult.Successful();
            });

        // ExitCalibration
        builder.RegisterSimple(
            "ExitCalibration",
            "Exit calibration mode on the Arduino Conductivity probe",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("XC"), ct));
                if (string.IsNullOrEmpty(resp) || !resp.Contains("Successful"))
                    return CapabilityInvocationResult.Failed("Calibration did not exit successfully");
                return CapabilityInvocationResult.Successful();
            });

        // Calibrate
        builder.RegisterSimple(
            "Calibrate",
            "Calibrate the Arduino Conductivity probe with buffer solution",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("CC"), ct));
                if (string.IsNullOrEmpty(resp) || !resp.Contains("Successful"))
                    return CapabilityInvocationResult.Failed("Calibration did not exit successfully");
                return CapabilityInvocationResult.Successful();
            });

        // GetReading
        builder.RegisterSimple(
            "GetReading",
            "Get a conductivity reading from the Arduino Conductivity probe",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("MC"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Arduino conductivity did not return correctly");
                if (!double.TryParse(resp, out var value))
                    return CapabilityInvocationResult.Failed($"Unexpected response: {resp}");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(value));
            });

        // GetUniqueId (serial number)
        builder.RegisterSimple(
            "GetUniqueId",
            "Get the serial number of the Arduino Conductivity probe",
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
            try { resp = Decode(await Transport.SendReceiveAsync(Encode("MC"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "ArduinoConductivity: failed to poll MC"); }

            if (!string.IsNullOrEmpty(resp) && double.TryParse(resp, out var value))
                yield return new TrendSample { TrendName = "arduinoconductivity.conductivity", Value = value, Timestamp = now, Unit = "mS/cm" };

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
