using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Endress+Hauser CML18 conductivity sensor.
/// Serial protocol, BaudRate 9600, CmdTerminator \r\n.
/// Response format: M;DeviceId;SerialNum;TrendName;Value;Unit;Temperature;TempValue;TempUnit;RawName;RawValue;RawUnit;Timestamp;SerialNum
/// </summary>
public sealed class Cml18 : DeviceAdapterBase
{
    public Cml18(SerialDeviceTransport transport, ILogger<Cml18> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // GetMeasurement — reads the latest measurement from the CML18
        builder.RegisterSimple(
            "GetMeasurement",
            "Get the latest conductivity/temperature measurement from the CML18",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("M\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("No response from device");
                var parsed = ParseResponse(resp);
                if (parsed is null)
                    return CapabilityInvocationResult.Failed($"Could not parse response: {resp}");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new
                {
                    trendName = parsed.Value.TrendName,
                    value = parsed.Value.TrendValue,
                    unit = parsed.Value.TrendUnit,
                    temperature = parsed.Value.Temperature,
                    temperatureUnit = parsed.Value.TemperatureUnit,
                    serialNum = parsed.Value.SerialNum
                }));
            });
    }

    /// <summary>
    /// Parses a single CML18 response line.
    /// Format: M;DeviceId;SerialNum;TrendName;Value;Unit;Temperature;TempValue;TempUnit;RawName;RawValue;RawUnit;Timestamp;SerialNum
    /// </summary>
    private static (string TrendName, double TrendValue, string TrendUnit, double Temperature, string TemperatureUnit, string SerialNum)? ParseResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
            return null;

        // Handle multiple lines — take the last valid one
        var lines = response.Split('\n');
        foreach (var line in lines.Reverse())
        {
            var split = line.Trim().Split(';');
            if (split.Length < 14)
                continue;

            var trendName = split[3];
            var unit = split[5].Replace("??", "");
            var tempUnit = split[8].Replace("??", "");
            var serialNum = split[^1].Trim();

            if (!double.TryParse(split[4], out var value)) continue;
            if (!double.TryParse(split[7], out var temp)) continue;

            return (trendName, value, unit, temp, tempUnit, serialNum);
        }

        return null;
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            string? resp = null;
            try { resp = Decode(await Transport.SendReceiveAsync(Encode("M\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "Cml18: failed to poll M"); }

            if (!string.IsNullOrEmpty(resp))
            {
                var parsed = ParseResponse(resp);
                if (parsed.HasValue)
                {
                    yield return new TrendSample { TrendName = $"cml18.{parsed.Value.TrendName.ToLower()}", Value = parsed.Value.TrendValue, Timestamp = now, Unit = parsed.Value.TrendUnit };
                    yield return new TrendSample { TrendName = "cml18.temperature", Value = parsed.Value.Temperature, Timestamp = now, Unit = "C" };
                }
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
