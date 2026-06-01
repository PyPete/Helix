using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the IKA Ultra Turrax disperser.
/// Serial protocol, BaudRate 9600, command terminator \r\n.
/// Like IkaOverheadStirrer but parses split[1] instead of split[0], and limits are [3000, 25000] rpm.
/// </summary>
public sealed class IkaUltraTurrax : DeviceAdapterBase
{
    private readonly double[] _availableLimits = [3000, 25000];
    private int _ikaSetPoint;

    public IkaUltraTurrax(SerialDeviceTransport transport, ILogger<IkaUltraTurrax> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start — "START_4 \r\n" then SetPoint
        builder.RegisterSimple(
            "Start",
            "Start the IKA Ultra Turrax disperser",
            async ct =>
            {
                await Transport.SendReceiveAsync(Encode("START_4 \r\n"), ct);
                return await SetPointInternal(_ikaSetPoint, ct);
            });

        // Stop — "STOP_4 \r\n" then check speed
        builder.RegisterSimple(
            "Stop",
            "Stop the IKA Ultra Turrax disperser",
            async ct =>
            {
                await Transport.SendReceiveAsync(Encode("STOP_4 \r\n"), ct);
                var resp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_4\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Stop command did not return a response");
                var parts = resp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2 || !double.TryParse(parts[1], out var speed))
                    return CapabilityInvocationResult.Failed("Stop command did not set properly");
                if (!(Math.Abs(speed) < 5))
                    return CapabilityInvocationResult.Failed("Stop command did not set properly");
                return CapabilityInvocationResult.Successful();
            });

        // SetPoint — "OUT_SP_4 {rpm} \r\n", verify with "IN_SP_4\r\n" → split[1]
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetPoint",
                "Set the speed setpoint on the IKA Ultra Turrax",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"rpm\":{\"type\":\"number\",\"minimum\":3000,\"maximum\":25000}},\"required\":[\"rpm\"]}"),
            async (parameters, _, ct) =>
            {
                var rpm = parameters.GetProperty("rpm").GetDouble();
                return await SetPointInternal((int)rpm, ct);
            });

        // GetSetPoint — "IN_SP_4\r\n" → split[1]
        builder.RegisterSimple(
            "GetSetPoint",
            "Get the current speed setpoint from the IKA Ultra Turrax",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("IN_SP_4\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Set point command did not return a response");
                var parts = resp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2 || !double.TryParse(parts[1], out var setPoint))
                    return CapabilityInvocationResult.Failed("Could not parse setpoint response");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(setPoint));
            });

        // GetTorque — "IN_PV_5\r\n" → split[1]
        builder.RegisterSimple(
            "GetTorque",
            "Get the current torque from the IKA Ultra Turrax",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_5\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Command did not return a response");
                var parts = resp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2 || !double.TryParse(parts[1], out var torque))
                    return CapabilityInvocationResult.Failed("Could not parse torque response");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(torque));
            });

        // GetSetPointLimits
        builder.RegisterSimple(
            "GetSetPointLimits",
            "Get the min and max speed setpoint limits for the IKA Ultra Turrax",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { min = _availableLimits[0], max = _availableLimits[1] }))));
    }

    private async Task<CapabilityInvocationResult> SetPointInternal(int rpm, System.Threading.CancellationToken ct)
    {
        if (rpm < _availableLimits[0] || rpm > _availableLimits[1])
            return CapabilityInvocationResult.Failed("Set point outside of valid range");
        await Transport.SendReceiveAsync(Encode($"OUT_SP_4 {rpm} \r\n"), ct);
        var verifyResp = Decode(await Transport.SendReceiveAsync(Encode("IN_SP_4\r\n"), ct));
        if (string.IsNullOrEmpty(verifyResp))
            return CapabilityInvocationResult.Failed("Set point command did not return a response");
        var parts = verifyResp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !double.TryParse(parts[1], out var setPoint))
            return CapabilityInvocationResult.Failed("Set point command did not return a response");
        if (!(Math.Abs(setPoint - rpm) < 5))
            return CapabilityInvocationResult.Failed("Set point command did not set properly");
        _ikaSetPoint = rpm;
        return CapabilityInvocationResult.Successful();
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            // Torque — "IN_PV_5\r\n" → split[1]
            string? torqueResp = null;
            try { torqueResp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_5\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "IkaUltraTurrax: failed to poll IN_PV_5"); }
            if (!string.IsNullOrEmpty(torqueResp))
            {
                var parts = torqueResp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && double.TryParse(parts[1], out var torque))
                    yield return new TrendSample { TrendName = "ikaultraturrax.torque", Value = torque, Timestamp = now, Unit = "Ncm" };
            }

            // Actual speed — "IN_PV_4\r\n" → split[1]
            string? speedResp = null;
            try { speedResp = Decode(await Transport.SendReceiveAsync(Encode("IN_PV_4\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "IkaUltraTurrax: failed to poll IN_PV_4"); }
            if (!string.IsNullOrEmpty(speedResp))
            {
                var parts = speedResp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && double.TryParse(parts[1], out var speed))
                    yield return new TrendSample { TrendName = "ikaultraturrax.speed", Value = speed, Timestamp = now, Unit = "rpm" };
            }

            // Setpoint — "IN_SP_4\r\n" → split[1]
            string? setResp = null;
            try { setResp = Decode(await Transport.SendReceiveAsync(Encode("IN_SP_4\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "IkaUltraTurrax: failed to poll IN_SP_4"); }
            if (!string.IsNullOrEmpty(setResp))
            {
                var parts = setResp.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && double.TryParse(parts[1], out var setSp))
                    yield return new TrendSample { TrendName = "ikaultraturrax.setpointspeed", Value = setSp, Timestamp = now, Unit = "rpm" };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
