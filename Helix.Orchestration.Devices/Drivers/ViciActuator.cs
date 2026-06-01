using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Vici valve actuator.
/// Serial protocol, BaudRate 9600, command terminator \r.
/// Position A or B. Commands: GO{pos}\r, CP\r (current position), DT{ms}\r (set delay), TT\r (switch+return).
/// </summary>
public sealed class ViciActuator : DeviceAdapterBase
{
    private int _delayMs = 1000;

    public ViciActuator(SerialDeviceTransport transport, ILogger<ViciActuator> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // GoTo — "GO{position}\r" (A or B)
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "GoTo",
                "Move the Vici Actuator to position A or B",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"position\":{\"type\":\"string\",\"enum\":[\"A\",\"B\"]}},\"required\":[\"position\"]}"),
            async (parameters, _, ct) =>
            {
                var pos = parameters.GetProperty("position").GetString() ?? "A";
                await Transport.SendReceiveAsync(Encode($"GO{pos}\r"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // GetCurrentPosition — "CP\r" → "CPA\x0D" or "CPB\x0D"
        builder.RegisterSimple(
            "GetCurrentPosition",
            "Get the current position of the Vici Actuator (A or B)",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("CP\r"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Command did not return correct response");
                if (resp.Contains("?"))
                    return CapabilityInvocationResult.Failed("Command did not return correct response");
                if (resp.Length < 3)
                    return CapabilityInvocationResult.Failed("Command did not return correct response");
                var pos = resp[2].ToString();
                if (pos != "A" && pos != "B")
                    return CapabilityInvocationResult.Failed("Command did not return correct response");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(pos));
            });

        // SetDelay — "DT{ms}\r", stores internally too
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetDelay",
                "Set the delay interval (ms) for the Vici Actuator switch-then-return (0-65000)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"milliseconds\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":65000}},\"required\":[\"milliseconds\"]}"),
            async (parameters, _, ct) =>
            {
                var ms = parameters.GetProperty("milliseconds").GetInt32();
                if (ms is < 0 or > 65000)
                    return CapabilityInvocationResult.Failed("Setpoint is outside of the available range");
                _delayMs = ms;
                await Transport.SendReceiveAsync(Encode($"DT{ms}\r"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // SwitchThenReturn — "TT\r" then wait for stored delay
        builder.RegisterSimple(
            "SwitchThenReturn",
            "Toggle the Vici Actuator to the opposite position then return after the stored delay",
            async ct =>
            {
                await Transport.SendReceiveAsync(Encode("TT\r"), ct);
                await Task.Delay(_delayMs, ct);
                return CapabilityInvocationResult.Successful();
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            string? resp = null;
            try { resp = Decode(await Transport.SendReceiveAsync(Encode("CP\r"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "ViciActuator: failed to poll CP"); }

            if (!string.IsNullOrEmpty(resp) && resp.Length >= 3 && !resp.Contains("?"))
            {
                var pos = resp[2].ToString();
                if (pos == "A" || pos == "B")
                    yield return new TrendSample { TrendName = "viciactuator.position", Value = pos, Timestamp = now, Unit = null };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
