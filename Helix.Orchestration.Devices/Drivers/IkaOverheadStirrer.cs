using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the IKA Overhead Stirrer (EuroStar/Ministar series).
/// Serial protocol, BaudRate 9600, DataBits 8, Parity None, command terminator \r\n.
/// Write commands use "CMD \r\n" (space before terminator), read commands use "CMD\r\n".
/// Limits: [0, 2000] rpm.
/// </summary>
public class IkaOverheadStirrer : DeviceAdapterBase
{
    protected readonly double[] AvailableLimits = [0, 2000];
    private int _ikaSetPoint;

    public IkaOverheadStirrer(SerialDeviceTransport transport, ILogger<IkaOverheadStirrer> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start — "START_4 \r\n" then SetPoint
        builder.RegisterSimple(
            "Start",
            "Start the IKA Overhead Stirrer motor",
            async ct =>
            {
                await Transport.SendReceiveAsync(Encode("START_4 \r\n"), ct);
                return await SetPointInternal(_ikaSetPoint, ct);
            });

        // Stop — "STOP_4 \r\n" then check speed
        builder.RegisterSimple(
            "Stop",
            "Stop the IKA Overhead Stirrer motor",
            async ct =>
            {
                await Transport.SendReceiveAsync(Encode("STOP_4 \r\n"), ct);
                var (info, speed) = await GetActualSpeedInternal(ct);
                if (info)
                    return CapabilityInvocationResult.Failed("Stop command did not return a success state");
                if (!(Math.Abs(speed) < 5))
                    return CapabilityInvocationResult.Failed("Stop command did not set properly");
                return CapabilityInvocationResult.Successful();
            });

        // SetPoint — "OUT_SP_4 {rpm} \r\n", verify with "IN_SP_4\r\n"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetPoint",
                "Set the speed setpoint on the IKA Overhead Stirrer",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"rpm\":{\"type\":\"number\",\"minimum\":0,\"maximum\":2000}},\"required\":[\"rpm\"]}"),
            async (parameters, _, ct) =>
            {
                var rpm = parameters.GetProperty("rpm").GetDouble();
                return await SetPointInternal((int)rpm, ct);
            });

        // GetSetPoint — "IN_SP_4\r\n" → "0.0 4" split by space, index 0
        builder.RegisterSimple(
            "GetSetPoint",
            "Get the current speed setpoint from the IKA Overhead Stirrer",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("IN_SP_4\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Set point command did not return a response");
                var parts = resp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (!double.TryParse(parts[0], out var setPoint))
                    return CapabilityInvocationResult.Failed("Could not parse setpoint response");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(setPoint));
            });

        // GetTorque — "IN_PV_5\r\n" → "0.00 5" split, index 0
        builder.RegisterSimple(
            "GetTorque",
            "Get the current torque from the IKA Overhead Stirrer",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_5\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Command did not return a response");
                var parts = resp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (!double.TryParse(parts[0], out var torque))
                    return CapabilityInvocationResult.Failed("Could not parse torque response");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(torque));
            });

        // GetSetPointLimits
        builder.RegisterSimple(
            "GetSetPointLimits",
            "Get the min and max speed setpoint limits for the IKA Overhead Stirrer",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { min = AvailableLimits[0], max = AvailableLimits[1] }))));
    }

    private async Task<CapabilityInvocationResult> SetPointInternal(int rpm, System.Threading.CancellationToken ct)
    {
        if (rpm < AvailableLimits[0] || rpm > AvailableLimits[1])
            return CapabilityInvocationResult.Failed("Set point outside of valid range");
        await Transport.SendReceiveAsync(Encode($"OUT_SP_4 {rpm} \r\n"), ct);
        var (info, setPoint) = await GetSetPointInternal(ct);
        if (!info)
            return CapabilityInvocationResult.Failed("Set point command did not return a response");
        if (!(Math.Abs(setPoint - rpm) < 5))
            return CapabilityInvocationResult.Failed("Set point command did not set properly");
        _ikaSetPoint = rpm;
        return CapabilityInvocationResult.Successful();
    }

    protected async Task<(bool success, double value)> GetSetPointInternal(System.Threading.CancellationToken ct)
    {
        var resp = Decode(await Transport.SendReceiveAsync(Encode("IN_SP_4\r\n"), ct));
        if (string.IsNullOrEmpty(resp)) return (false, 0);
        var parts = resp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
        return double.TryParse(parts[0], out var sp) ? (true, sp) : (false, 0);
    }

    protected async Task<(bool failed, double speed)> GetActualSpeedInternal(System.Threading.CancellationToken ct)
    {
        var resp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_4\r\n"), ct));
        if (string.IsNullOrEmpty(resp)) return (true, 0);
        var parts = resp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
        return double.TryParse(parts[0], out var speed) ? (false, speed) : (true, 0);
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            // Torque
            string? torqueResp = null;
            try { torqueResp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_5\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "IkaOverheadStirrer: failed to poll IN_PV_5"); }

            if (!string.IsNullOrEmpty(torqueResp))
            {
                var parts = torqueResp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], out var torque))
                    yield return new TrendSample { TrendName = "ikaoverheadstirrer.torque", Value = torque, Timestamp = now, Unit = "Ncm" };
            }

            // Actual speed
            string? speedResp = null;
            try { speedResp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_4\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "IkaOverheadStirrer: failed to poll IN_PV_4"); }

            if (!string.IsNullOrEmpty(speedResp))
            {
                var parts = speedResp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], out var speed))
                    yield return new TrendSample { TrendName = "ikaoverheadstirrer.speed", Value = speed, Timestamp = now, Unit = "rpm" };
            }

            // Setpoint speed
            string? setResp = null;
            try { setResp = Decode(await Transport.SendReceiveAsync(Encode("IN_SP_4\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "IkaOverheadStirrer: failed to poll IN_SP_4"); }

            if (!string.IsNullOrEmpty(setResp))
            {
                var parts = setResp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], out var setSp))
                    yield return new TrendSample { TrendName = "ikaoverheadstirrer.setpointspeed", Value = setSp, Timestamp = now, Unit = "rpm" };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
