using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Heidolph overhead stirrer.
/// Serial protocol, BaudRate 9600, command terminator \r\n.
/// Commands: r (actual speed), s (set speed), m (torque), R{speed} (start at speed), R0000 (stop).
/// </summary>
public sealed class Heidolph : DeviceAdapterBase
{
    private readonly double[] _availableLimits = [10, 2000];
    private int _currentSpeed;

    public Heidolph(SerialDeviceTransport transport, ILogger<Heidolph> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start — "R{speed}\r\n" → response matches "^R[0-9]{4}"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "Start",
                "Start the Heidolph stirrer at the current speed setpoint",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"rpm\":{\"type\":\"number\",\"minimum\":10,\"maximum\":2000}},\"required\":[\"rpm\"]}"),
            async (parameters, _, ct) =>
            {
                var rpm = parameters.GetProperty("rpm").GetDouble();
                if (rpm < _availableLimits[0] || rpm > _availableLimits[1])
                    return CapabilityInvocationResult.Failed("Speed is outside limits");
                _currentSpeed = (int)rpm;
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"R{_currentSpeed:D4}\r\n"), ct));
                var parts = resp.Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    return CapabilityInvocationResult.Failed("Command returned incorrectly from device");
                if (!Regex.IsMatch(parts[0], "^[R][0-9]{4}"))
                    return CapabilityInvocationResult.Failed("Command returned was of an incorrect type");
                return CapabilityInvocationResult.Successful();
            });

        // Stop — "R0000\r\n"
        builder.RegisterSimple(
            "Stop",
            "Stop the Heidolph stirrer",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("R0000\r\n"), ct));
                var parts = resp.Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    return CapabilityInvocationResult.Failed("Command returned incorrectly from device");
                return CapabilityInvocationResult.Successful();
            });

        // SetPoint — "R{speed}\r\n" with confirmation
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetPoint",
                "Set the speed setpoint on the Heidolph stirrer",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"rpm\":{\"type\":\"number\",\"minimum\":10,\"maximum\":2000}},\"required\":[\"rpm\"]}"),
            async (parameters, _, ct) =>
            {
                var rpm = parameters.GetProperty("rpm").GetDouble();
                if (rpm < _availableLimits[0] || rpm > _availableLimits[1])
                    return CapabilityInvocationResult.Failed("Set point is outside of bounds");
                _currentSpeed = (int)rpm;
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"R{_currentSpeed:D4}\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Command returned empty");
                var parts = resp.Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries);
                if (!Regex.IsMatch(parts[0], "^[R][0-9]{4}"))
                    return CapabilityInvocationResult.Failed("Incorrect form of the returned command");
                return CapabilityInvocationResult.Successful();
            });

        // GetSetPoint — "s\r\n" → "SET: xxxx"
        builder.RegisterSimple(
            "GetSetPoint",
            "Get the current speed setpoint from the Heidolph stirrer",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("s\r\n"), ct));
                if (resp.Length < 9)
                    return CapabilityInvocationResult.Failed("Command returned incorrectly from device");
                var setPointIdx = resp.IndexOf("SET", StringComparison.Ordinal);
                if (setPointIdx < 0)
                    return CapabilityInvocationResult.Failed("Command returned incorrectly from device");
                var speed = resp[(setPointIdx + 4)..];
                if (!int.TryParse(speed.Trim(), out var speedVal))
                    return CapabilityInvocationResult.Failed("Could not parse setpoint response");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement((double)speedVal));
            });

        // GetActualSpeed — "r\r\n" → "RPM: xxxx"
        builder.RegisterSimple(
            "GetActualSpeed",
            "Get the current actual speed from the Heidolph stirrer",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("r\r\n"), ct));
                if (resp.Length < 9)
                    return CapabilityInvocationResult.Failed("Command returned incorrectly from device");
                var actualIdx = resp.IndexOf("RPM", StringComparison.Ordinal);
                if (actualIdx < 0)
                    return CapabilityInvocationResult.Failed("Command returned incorrectly from device");
                var speed = resp[(actualIdx + 4)..];
                if (!int.TryParse(speed.Trim(), out var speedVal))
                    return CapabilityInvocationResult.Failed("Could not parse speed response");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement((double)speedVal));
            });

        // GetTorque — "m\r\n" → "NCM: xxxx"
        builder.RegisterSimple(
            "GetTorque",
            "Get the current torque from the Heidolph stirrer",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("m\r\n"), ct));
                if (resp.Length < 9)
                    return CapabilityInvocationResult.Failed("Command returned incorrectly from device");
                var torqueIdx = resp.IndexOf("NCM", StringComparison.Ordinal);
                if (torqueIdx < 0)
                    return CapabilityInvocationResult.Failed("Command returned incorrectly from device");
                var torque = resp[(torqueIdx + 4)..];
                if (!int.TryParse(torque.Trim(), out var torqueVal))
                    return CapabilityInvocationResult.Failed("Could not parse torque response");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement((double)torqueVal));
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            // Poll torque
            string? torqueResp = null;
            try { torqueResp = Decode(await Transport.SendReceiveAsync(Encode("m\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "Heidolph: failed to poll m"); }

            if (!string.IsNullOrEmpty(torqueResp))
            {
                var idx = torqueResp.IndexOf("NCM", StringComparison.Ordinal);
                if (idx >= 0 && int.TryParse(torqueResp[(idx + 4)..].Trim(), out var torque))
                    yield return new TrendSample { TrendName = "heidolph.torque", Value = (double)torque, Timestamp = now, Unit = "Nmm" };
            }

            // Poll actual speed
            string? speedResp = null;
            try { speedResp = Decode(await Transport.SendReceiveAsync(Encode("r\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "Heidolph: failed to poll r"); }

            if (!string.IsNullOrEmpty(speedResp))
            {
                var idx = speedResp.IndexOf("RPM", StringComparison.Ordinal);
                if (idx >= 0 && int.TryParse(speedResp[(idx + 4)..].Trim(), out var speed))
                    yield return new TrendSample { TrendName = "heidolph.speed", Value = (double)speed, Timestamp = now, Unit = "rpm" };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
