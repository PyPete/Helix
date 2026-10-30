using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Lambda Doser solids doser.
/// Serial protocol, BaudRate 2400, Parity Odd, command terminator \r.
/// Commands use a checksum: sum of ASCII values, last 2 hex digits uppercase.
/// </summary>
public sealed class LambdaDoser : DeviceAdapterBase
{
    private readonly double[] _availableLimits = [0, 999];
    private int _speed;
    private bool _isOn;

    public LambdaDoser(SerialDeviceTransport transport, ILogger<LambdaDoser> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    private static string CommandBuilder(string command)
    {
        var total = command.Sum(c => (int)Convert.ToByte(c));
        return Convert.ToString(total, 16)[^2..].ToUpper();
    }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start — "#0201r{rpm}{checksum}\r", verify with "#0201G2D\r"
        builder.RegisterSimple(
            "Start",
            "Start the Lambda Doser at the current speed setpoint",
            async ct =>
            {
                _isOn = true;
                var rateString = _speed.ToString().PadLeft(3, '0');
                var checkByte = CommandBuilder("#0201r" + rateString);
                await Transport.SendReceiveAsync(Encode($"#0201r{rateString}{checkByte}\r"), ct);
                var resp = Decode(await Transport.SendReceiveAsync(Encode("#0201G2D\r"), ct));
                if (resp.Contains($"<0102r{rateString}"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Device may not have started correctly");
            });

        // Stop — "#0201s{checksum}\r", verify with "#0201G2D\r"
        builder.RegisterSimple(
            "Stop",
            "Stop the Lambda Doser",
            async ct =>
            {
                if (!_isOn)
                    return CapabilityInvocationResult.Successful();
                var checkByte = CommandBuilder("#0201s");
                await Transport.SendReceiveAsync(Encode($"#0201s{checkByte}\r"), ct);
                var resp = Decode(await Transport.SendReceiveAsync(Encode("#0201G2D\r"), ct));
                _isOn = false;
                if (resp.Contains("<0102r00001"))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Device may not have stopped correctly");
            });

        // SetPoint — stores speed, optionally sends command if on
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetPoint",
                "Set the dose rate setpoint on the Lambda Doser (0-999 rpm)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"setpoint\":{\"type\":\"number\",\"minimum\":0,\"maximum\":999}},\"required\":[\"setpoint\"]}"),
            async (parameters, _, ct) =>
            {
                var setpoint = parameters.GetProperty("setpoint").GetDouble();
                if (setpoint < _availableLimits[0] || setpoint > _availableLimits[1])
                    return CapabilityInvocationResult.Failed("Setpoint outside of limits");
                _speed = (int)setpoint;
                if (!_isOn) return CapabilityInvocationResult.Successful();
                var rateString = Convert.ToString(setpoint, CultureInfo.InvariantCulture).PadLeft(3, '0');
                var checkByte = CommandBuilder("#0201r" + rateString);
                await Transport.SendReceiveAsync(Encode($"#0201r{rateString}{checkByte}\r"), ct);
                var resp = Decode(await Transport.SendReceiveAsync(Encode("#0201G2D\r"), ct));
                if (resp.Contains("<0102r" + rateString))
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Device may not have set correctly");
            });

        // GetSetPoint
        builder.RegisterSimple(
            "GetSetPoint",
            "Get the current dose rate setpoint from the Lambda Doser",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement((double)_speed))));

        // GetSetPointLimits
        builder.RegisterSimple(
            "GetSetPointLimits",
            "Get the min and max dose rate limits for the Lambda Doser",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { min = _availableLimits[0], max = _availableLimits[1] }))));
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            yield return new TrendSample { TrendName = "lambdadoser.setpointspeed", Value = (double)_speed, Timestamp = now, Unit = "rpm" };

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
