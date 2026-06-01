using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Masterflex L/S Digital peristaltic pump.
/// Serial protocol, BaudRate 4800, DataBits 7, Parity Odd, command terminator \r.
/// Commands use STX (\x02), pump ID "02", and CR (\r = \x0D).
/// Responses: ACK=\x06 for success, "P99" for failure.
/// </summary>
public sealed class MasterFlexLsDigital : DeviceAdapterBase
{
    private const string Stx = "\x02";
    private const string Id = "02";
    private readonly double[] _availableLimits = [0, 600];
    private double _setPoint;

    public MasterFlexLsDigital(SerialDeviceTransport transport, ILogger<MasterFlexLsDigital> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    private static bool IsAck(string resp) =>
        resp.Length > 0 && BitConverter.ToString(Encoding.ASCII.GetBytes(resp[0].ToString())) == "06";

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start — "\x02P02G0\r" → ACK=\x06
        builder.RegisterSimple(
            "Start",
            "Start the Masterflex L/S Digital pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"{Stx}P{Id}G0\r"), ct));
                if (IsAck(resp))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Masterflex Ls Digital maybe not have started correctly");
            });

        // Stop — "\x02P02H\r" → ACK
        builder.RegisterSimple(
            "Stop",
            "Stop the Masterflex L/S Digital pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"{Stx}P{Id}H\r"), ct));
                if (IsAck(resp))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Masterflex Ls Digital maybe not have stopped correctly");
            });

        // SetPoint — "\x02P02S+{rpm}\r" or "\x02P02S-{rpm}\r" → ACK
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetPoint",
                "Set the speed setpoint (rpm) on the Masterflex L/S Digital pump (0-600)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"rpm\":{\"type\":\"number\",\"minimum\":0,\"maximum\":600}},\"required\":[\"rpm\"]}"),
            async (parameters, _, ct) =>
            {
                var rpm = parameters.GetProperty("rpm").GetDouble();
                if (rpm < _availableLimits[0] || rpm > _availableLimits[1])
                    return CapabilityInvocationResult.Failed("Set point is outside of bounds");
                // Halt first
                await Transport.SendReceiveAsync(Encode($"{Stx}P{Id}H\r"), ct);
                var sign = rpm >= 0 ? "+" : "-";
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"{Stx}P{Id}S{sign}{rpm}\r"), ct));
                _setPoint = rpm;
                if (IsAck(resp))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Masterflex Ls Digital maybe not have stopped correctly");
            });

        // GetSetPoint — "\x02P02S\r" → "<STX>S+075.0\r"
        builder.RegisterSimple(
            "GetSetPoint",
            "Get the current speed setpoint from the Masterflex L/S Digital pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"{Stx}P{Id}S\r"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Set point string did not return correctly");
                if (resp.Length < 3)
                    return CapabilityInvocationResult.Failed("Set point string did not return correctly");
                if (!double.TryParse(resp[2..], out var sp))
                    return CapabilityInvocationResult.Failed("Could not parse setpoint");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(sp));
            });

        // GetSetPointLimits
        builder.RegisterSimple(
            "GetSetPointLimits",
            "Get the min and max speed setpoint limits for the Masterflex L/S Digital pump",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { min = _availableLimits[0], max = _availableLimits[1] }))));
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            // Cumulative revolutions — "\x02P02C\r"
            string? cumResp = null;
            try { cumResp = Decode(await Transport.SendReceiveAsync(Encode($"{Stx}P{Id}C\r"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "MasterFlexLsDigital: failed to poll C"); }

            if (!string.IsNullOrEmpty(cumResp) && cumResp.Length >= 3 && double.TryParse(cumResp[2..], out var cumRev))
                yield return new TrendSample { TrendName = "masterflexlsdigital.cumulativerevolutions", Value = cumRev, Timestamp = now, Unit = "rpm" };

            yield return new TrendSample { TrendName = "masterflexlsdigital.setpointspeed", Value = _setPoint, Timestamp = now, Unit = "rpm" };

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
