using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Gynkotek high pressure pump.
/// Serial protocol, BaudRate 9600, no command terminator.
/// Binary encoded flow rate protocol (BCD → binary strings).
/// Limits: [0, 50] ml/min.
/// </summary>
public sealed class Gynkotek : DeviceAdapterBase
{
    private readonly double[] _availableLimits = [0, 50];

    public Gynkotek(SerialDeviceTransport transport, ILogger<Gynkotek> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start — "SF" → "starting flow"
        builder.RegisterSimple(
            "Start",
            "Start the Gynkotek pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("SF"), ct));
                if (!string.IsNullOrEmpty(resp) && resp == "starting flow")
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Start may not have executed correctly");
            });

        // Stop — "ST" → "stopping flow"
        builder.RegisterSimple(
            "Stop",
            "Stop the Gynkotek pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("ST"), ct));
                if (!string.IsNullOrEmpty(resp) && resp == "stopping flow")
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Stop may not have executed correctly");
            });

        // SetPoint — "FR{binaryEncoded}" → "setting flow"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetPoint",
                "Set the flow rate in ml/min (0-50) using binary-encoded protocol",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"setpoint\":{\"type\":\"number\",\"minimum\":0,\"maximum\":50}},\"required\":[\"setpoint\"]}"),
            async (parameters, _, ct) =>
            {
                var setpoint = parameters.GetProperty("setpoint").GetDouble();
                if (setpoint < _availableLimits[0] || setpoint > _availableLimits[1])
                    return CapabilityInvocationResult.Failed("Set point is outside of bounds");
                var newFlow = (int)(setpoint * 1000);
                var strFlow = newFlow.ToString("D3");
                var flowBinary = string.Concat(strFlow.Select(c =>
                    Convert.ToString(c - '0', 2).PadLeft(4, '0')));
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"FR{flowBinary}"), ct));
                if (!string.IsNullOrEmpty(resp) && resp == "setting flow")
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Set flow may not have executed correctly");
            });

        // GetSetPoint — "GF" → binary string; decode to ml/min
        builder.RegisterSimple(
            "GetSetPoint",
            "Get the current flow rate setpoint from the Gynkotek pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("GF"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Command returned empty");
                const int chunkSize = 4;
                var gotFlow = "";
                for (var i = 0; i < resp.Length; i += chunkSize)
                {
                    if (i + chunkSize > resp.Length) break;
                    gotFlow += Convert.ToString(Convert.ToInt32(resp.Substring(i, chunkSize), 2));
                }
                if (!double.TryParse(gotFlow, out var flowAsDbl))
                    return CapabilityInvocationResult.Failed("Could not parse flow response");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(flowAsDbl / 1000.0));
            });

        // GetSetPointLimits
        builder.RegisterSimple(
            "GetSetPointLimits",
            "Get the min and max flow setpoint limits for the Gynkotek pump",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { min = _availableLimits[0], max = _availableLimits[1] }))));

        // GetUniqueId — "SR"
        builder.RegisterSimple(
            "GetUniqueId",
            "Get the serial number of the Gynkotek pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("SR"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Serial number returned incorrectly");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(resp));
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            string? resp = null;
            try { resp = Decode(await Transport.SendReceiveAsync(Encode("GF"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "Gynkotek: failed to poll GF"); }

            if (!string.IsNullOrEmpty(resp))
            {
                const int chunkSize = 4;
                var gotFlow = "";
                for (var i = 0; i < resp.Length; i += chunkSize)
                {
                    if (i + chunkSize > resp.Length) break;
                    gotFlow += Convert.ToString(Convert.ToInt32(resp.Substring(i, chunkSize), 2));
                }
                if (double.TryParse(gotFlow, out var flowAsDbl))
                    yield return new TrendSample { TrendName = "gynkotek.flow", Value = flowAsDbl / 1000.0, Timestamp = now, Unit = "ml/min" };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
