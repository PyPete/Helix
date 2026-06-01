using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Vacuubrand Vario vacuum pump.
/// Serial protocol, BaudRate 9600, command terminator \r\n.
/// Commands: START \r\n, STOP 1\r\n, IN_PV_1\r\n (pressure), IN_PV_2\r\n (frequency).
/// SetPoint stores the pressure setpoint (range 1-1060 mbar).
/// </summary>
public sealed class Vario : DeviceAdapterBase
{
    private readonly double[] _availableLimits = [1, 1060];
    private int _setPoint;

    public Vario(SerialDeviceTransport transport, ILogger<Vario> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start — "START \r\n" (space before terminator, no response expected)
        builder.RegisterSimple(
            "Start",
            "Start the Vacuubrand Vario vacuum pump",
            async ct =>
            {
                await Transport.SendReceiveAsync(Encode("START \r\n"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // Stop — "STOP 1\r\n"
        builder.RegisterSimple(
            "Stop",
            "Stop the Vacuubrand Vario vacuum pump",
            async ct =>
            {
                await Transport.SendReceiveAsync(Encode("STOP 1\r\n"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // Vent — "OUT_VENT 1\r\n"
        builder.RegisterSimple(
            "Vent",
            "Vent the Vacuubrand Vario vacuum pump",
            async ct =>
            {
                await Transport.SendReceiveAsync(Encode("OUT_VENT 1\r\n"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // SetPoint — stores pressure setpoint, sends OUT_SP_V command
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetPoint",
                "Set the pressure setpoint on the Vario (1-1060 mbar)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"mbar\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":1060}},\"required\":[\"mbar\"]}"),
            async (parameters, _, ct) =>
            {
                var mbar = parameters.GetProperty("mbar").GetInt32();
                if (mbar < _availableLimits[0] || mbar > _availableLimits[1])
                    return CapabilityInvocationResult.Failed("Pressure is outside of device range");
                _setPoint = mbar;
                var pressStr = mbar switch
                {
                    >= 1000 => $"{mbar}",
                    >= 100 => $"0{mbar}",
                    >= 10 => $"00{mbar}",
                    _ => $"000{mbar}"
                };
                await Transport.SendReceiveAsync(Encode($"OUT_SP_V {pressStr}\r\n"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // GetSetPoint — returns stored setpoint or polls IN_PV_1
        builder.RegisterSimple(
            "GetSetPoint",
            "Get the current pressure setpoint from the Vario",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_1\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement((double)_setPoint));
                var clean = resp.Replace("\r\n", "").Replace("Hz", "").Replace("z", "");
                var parts = clean.Split(' ');
                if (parts.Length >= 1 && double.TryParse(parts[0], out var pressure))
                    return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(pressure));
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement((double)_setPoint));
            });

        // GetSetPointLimits
        builder.RegisterSimple(
            "GetSetPointLimits",
            "Get the min and max pressure setpoint limits for the Vario",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { min = _availableLimits[0], max = _availableLimits[1] }))));
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            // Actual pressure — "IN_PV_1\r\n" → "1017 mbar\r\n"
            string? pressResp = null;
            try { pressResp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_1\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "Vario: failed to poll IN_PV_1"); }

            if (!string.IsNullOrEmpty(pressResp))
            {
                var clean = pressResp.Replace("\r\n", "").Replace("Hz", "").Replace("z", "");
                var parts = clean.Split(' ');
                if (parts.Length >= 2 && double.TryParse(parts[0], out var pressure))
                    yield return new TrendSample { TrendName = "vario.pressure", Value = pressure, Timestamp = now, Unit = parts[1] };
            }

            // Actual frequency — "IN_PV_2\r\n" → "00.0 Hz\r\n"
            string? freqResp = null;
            try { freqResp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_2\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "Vario: failed to poll IN_PV_2"); }

            if (!string.IsNullOrEmpty(freqResp))
            {
                var parts = freqResp.Replace("\r\n", "").Split(' ');
                if (parts.Length >= 2 && double.TryParse(parts[0], out var freq))
                    yield return new TrendSample { TrendName = "vario.frequency", Value = freq, Timestamp = now, Unit = parts[1] };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
