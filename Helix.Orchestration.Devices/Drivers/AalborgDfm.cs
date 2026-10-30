using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Aalborg DFM mass flow meter.
/// Serial protocol, BaudRate 9600, command terminator \r.
/// </summary>
public sealed class AalborgDfm : DeviceAdapterBase
{
    public AalborgDfm(SerialDeviceTransport transport, ILogger<AalborgDfm> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // GetTemperature
        builder.RegisterSimple(
            "GetTemperature",
            "Get the internal temperature from the Aalborg DFM",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("TR\r"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("No response from device");
                var parts = resp.Split(' ');
                if (parts.Length < 2 || !double.TryParse(parts[0], out var value))
                    return CapabilityInvocationResult.Failed($"Unexpected response: {resp}");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { value, unit = parts[1] }));
            });

        // GetPressure
        builder.RegisterSimple(
            "GetPressure",
            "Get the internal pressure from the Aalborg DFM",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("PR\r"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("No response from device");
                var parts = resp.Split(' ');
                if (parts.Length < 2 || !double.TryParse(parts[0], out var value))
                    return CapabilityInvocationResult.Failed($"Unexpected response: {resp}");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { value, unit = parts[1] }));
            });

        // GetFlow
        builder.RegisterSimple(
            "GetFlow",
            "Get the internal flow rate from the Aalborg DFM",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("F\r"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("No response from device");
                var parts = resp.Split(' ');
                if (parts.Length < 2 || !double.TryParse(parts[0], out var value))
                    return CapabilityInvocationResult.Failed($"Unexpected response: {resp}");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { value, unit = parts[1] }));
            });

        // GetUniqueId (serial number via MR1)
        builder.RegisterSimple(
            "GetUniqueId",
            "Read the serial number from the Aalborg DFM memory",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("MR1\r"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Device did not respond");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(resp));
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            // Pressure
            string? pressResp = null;
            try { pressResp = Decode(await Transport.SendReceiveAsync(Encode("PR\r"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "AalborgDfm: failed to poll PR"); }

            if (!string.IsNullOrEmpty(pressResp))
            {
                var parts = pressResp.Split(' ');
                if (parts.Length >= 2 && double.TryParse(parts[0], out var pressure))
                    yield return new TrendSample { TrendName = "aalborgdfm.pressure", Value = pressure, Timestamp = now, Unit = parts[1] };
            }

            // Flow
            string? flowResp = null;
            try { flowResp = Decode(await Transport.SendReceiveAsync(Encode("F\r"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "AalborgDfm: failed to poll F"); }

            if (!string.IsNullOrEmpty(flowResp))
            {
                var parts = flowResp.Split(' ');
                if (parts.Length >= 2 && double.TryParse(parts[0], out var flow))
                    yield return new TrendSample { TrendName = "aalborgdfm.flow", Value = flow, Timestamp = now, Unit = parts[1] };
            }

            // Temperature
            string? tempResp = null;
            try { tempResp = Decode(await Transport.SendReceiveAsync(Encode("TR\r"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "AalborgDfm: failed to poll TR"); }

            if (!string.IsNullOrEmpty(tempResp))
            {
                var parts = tempResp.Split(' ');
                if (parts.Length >= 2 && double.TryParse(parts[0], out var temp))
                    yield return new TrendSample { TrendName = "aalborgdfm.temperature", Value = temp, Timestamp = now, Unit = parts[1] };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
