using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Arduino Pressure sensor.
/// Serial protocol, BaudRate 9600, no command terminator.
/// </summary>
public sealed class ArduinoPressure : DeviceAdapterBase
{
    public ArduinoPressure(SerialDeviceTransport transport, ILogger<ArduinoPressure> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // GetNumberOfSensors
        builder.RegisterSimple(
            "GetNumberOfSensors",
            "Get the number of pressure sensors connected to the Arduino",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("NS"), ct));
                if (!int.TryParse(resp, out var count))
                    return CapabilityInvocationResult.Failed($"Unexpected response: {resp}");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(count));
            });

        // GetPressure (parameterized by sensor index)
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "GetPressure",
                "Get the pressure reading from a specific sensor (1-based index)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"sensor\":{\"type\":\"integer\",\"minimum\":1}},\"required\":[\"sensor\"]}"),
            async (parameters, _, ct) =>
            {
                var sensor = parameters.GetProperty("sensor").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"PR{sensor}"), ct));
                if (resp == null || resp == "?")
                    return CapabilityInvocationResult.Failed("Response returned incorrectly");
                if (!double.TryParse(resp, out var value))
                    return CapabilityInvocationResult.Failed($"Unexpected response: {resp}");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(value));
            });

        // GetUniqueId
        builder.RegisterSimple(
            "GetUniqueId",
            "Get the serial number of the Arduino Pressure sensor",
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

            string? nsResp = null;
            try { nsResp = Decode(await Transport.SendReceiveAsync(Encode("NS"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "ArduinoPressure: failed to poll NS"); }

            if (!string.IsNullOrEmpty(nsResp) && int.TryParse(nsResp, out var count))
            {
                for (var i = 1; i <= count; i++)
                {
                    string? presResp = null;
                    try { presResp = Decode(await Transport.SendReceiveAsync(Encode($"PR{i}"), ct)); }
                    catch (Exception ex) { Logger.LogWarning(ex, "ArduinoPressure: failed to poll PR{Sensor}", i); }

                    if (!string.IsNullOrEmpty(presResp) && presResp != "?" && double.TryParse(presResp, out var pressure))
                        yield return new TrendSample { TrendName = $"arduinopressure.pressure.{i}", Value = pressure, Timestamp = now, Unit = "bar" };
                }
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
