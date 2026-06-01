using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the IKA RCT Digital hotplate stirrer.
/// Serial protocol, BaudRate 9600, DataBits 7, Parity Even, command terminator \r\n.
/// Commands: "OUT_SP_4 {rpm}\r\n" (stir setpoint), "IN_SP_4\r\n" (stir getpoint),
///           "OUT_SP_1 {temp}\r\n" (temp setpoint), "IN_SP_1\r\n" (temp getpoint),
///           "IN_PV_4\r\n" (actual speed), "IN_PV_2\r\n" (hotplate temp),
///           "IN_PV_1\r\n" (external PT100 temp),
///           "START_4\r\n" / "STOP_4\r\n" (stir), "START_1\r\n" / "STOP_1\r\n" (heater).
/// </summary>
public sealed class IkaDigitalHotPlate : DeviceAdapterBase
{
    private const double MaxTemp = 310.0;

    public IkaDigitalHotPlate(SerialDeviceTransport transport, ILogger<IkaDigitalHotPlate> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    private static double ParseFirstToken(string resp)
    {
        var parts = resp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
        return double.TryParse(parts[0], out var v) ? v : double.NaN;
    }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // StirringSetPoint — "OUT_SP_4 {rpm}\r\n", verify with "IN_SP_4\r\n"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "StirringSetPoint",
                "Set the stirrer speed setpoint on the IKA RCT Digital hotplate (0-1500 rpm)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"rpm\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1500}},\"required\":[\"rpm\"]}"),
            async (parameters, _, ct) =>
            {
                var rpm = parameters.GetProperty("rpm").GetDouble();
                if (rpm is < 0 or > 1500)
                    return CapabilityInvocationResult.Failed("Setpoint is outside the feasible range");
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"OUT_SP_4 {rpm}\r\n"), ct));
                var verifyResp = Decode(await Transport.SendReceiveAsync(Encode("IN_SP_4\r\n"), ct));
                if (string.IsNullOrEmpty(verifyResp))
                    return CapabilityInvocationResult.Failed("Cannot validate the SetPoint.");
                var verifyVal = ParseFirstToken(verifyResp);
                if (double.IsNaN(verifyVal) || !(Math.Abs(verifyVal - rpm) < 0.1))
                    return CapabilityInvocationResult.Failed("Set point command did not set properly");
                return CapabilityInvocationResult.Successful();
            });

        // NewSetPoint (temperature) — "OUT_SP_1 {temp}\r\n", verify with "IN_SP_1\r\n"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "NewSetPoint",
                "Set the temperature setpoint on the IKA RCT Digital hotplate (0-310 C)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"celsius\":{\"type\":\"number\",\"minimum\":0,\"maximum\":310}},\"required\":[\"celsius\"]}"),
            async (parameters, _, ct) =>
            {
                var celsius = parameters.GetProperty("celsius").GetDouble();
                if (celsius < 0 || celsius > MaxTemp)
                    return CapabilityInvocationResult.Failed("Set point outside of valid range");
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"OUT_SP_1 {celsius}\r\n"), ct));
                var verifyResp = Decode(await Transport.SendReceiveAsync(Encode("IN_SP_1\r\n"), ct));
                if (string.IsNullOrEmpty(verifyResp))
                    return CapabilityInvocationResult.Failed("Cannot validate the SetPoint.");
                var verifyVal = ParseFirstToken(verifyResp);
                if (double.IsNaN(verifyVal) || !(Math.Abs(verifyVal - celsius) < 0.1))
                    return CapabilityInvocationResult.Failed("Set point command did not set properly");
                return CapabilityInvocationResult.Successful();
            });

        // StartStirrer — "START_4\r\n"
        builder.RegisterSimple(
            "StartStirrer",
            "Start the stirrer motor on the IKA RCT Digital hotplate",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("START_4\r\n"), ct));
                if (resp.Contains("RCT Digital"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Start stirrer may not have worked correctly");
            });

        // StopStirring — "STOP_4\r\n"
        builder.RegisterSimple(
            "StopStirring",
            "Stop the stirrer motor on the IKA RCT Digital hotplate",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("STOP_4\r\n"), ct));
                if (resp.Contains("RCT Digital"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Stop stirrer may not have worked correctly");
            });

        // StartHeater — "START_1\r\n"
        builder.RegisterSimple(
            "StartHeater",
            "Start the heater on the IKA RCT Digital hotplate",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("START_1\r\n"), ct));
                if (resp.Contains("RCT Digital"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Start heater may not have worked correctly");
            });

        // StopHeating — "STOP_1\r\n"
        builder.RegisterSimple(
            "StopHeating",
            "Stop the heater on the IKA RCT Digital hotplate",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("STOP_1\r\n"), ct));
                if (resp.Contains("RCT Digital"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Stop heating may not have worked correctly");
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            // Temperature setpoint
            string? setpointResp = null;
            try { setpointResp = Decode(await Transport.SendReceiveAsync(Encode("IN_SP_1\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "IkaDigitalHotPlate: failed to poll IN_SP_1"); }
            if (!string.IsNullOrEmpty(setpointResp))
            {
                var v = ParseFirstToken(setpointResp);
                if (!double.IsNaN(v)) yield return new TrendSample { TrendName = "ikadigitalhotplate.temperaturesetpoint", Value = v, Timestamp = now, Unit = "C" };
            }

            // Hotplate temperature
            string? tempResp = null;
            try { tempResp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_2\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "IkaDigitalHotPlate: failed to poll IN_PV_2"); }
            if (!string.IsNullOrEmpty(tempResp))
            {
                var v = ParseFirstToken(tempResp);
                if (!double.IsNaN(v)) yield return new TrendSample { TrendName = "ikadigitalhotplate.temperature", Value = v, Timestamp = now, Unit = "C" };
            }

            // External PT100 temperature
            string? extTempResp = null;
            try { extTempResp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_1\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "IkaDigitalHotPlate: failed to poll IN_PV_1"); }
            if (!string.IsNullOrEmpty(extTempResp))
            {
                var v = ParseFirstToken(extTempResp);
                if (!double.IsNaN(v)) yield return new TrendSample { TrendName = "ikadigitalhotplate.externaltemperature", Value = v, Timestamp = now, Unit = "C" };
            }

            // Actual stirrer speed
            string? speedResp = null;
            try { speedResp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_4\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "IkaDigitalHotPlate: failed to poll IN_PV_4"); }
            if (!string.IsNullOrEmpty(speedResp))
            {
                var v = ParseFirstToken(speedResp);
                if (!double.IsNaN(v)) yield return new TrendSample { TrendName = "ikadigitalhotplate.speed", Value = v, Timestamp = now, Unit = "rpm" };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
