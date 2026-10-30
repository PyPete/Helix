using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Vici Multi-Valve Actuator.
/// Serial protocol, BaudRate 9600, command terminator \r.
/// Commands prefix with valve ID: "ACP\r", "AGO A\r", "ADT{ms}\r".
/// has_trends=NO — does NOT override StreamTrendsAsync.
/// </summary>
public sealed class ViciMultiValveActuator : DeviceAdapterBase
{
    public ViciMultiValveActuator(SerialDeviceTransport transport, ILogger<ViciMultiValveActuator> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // GetNumberOfValves — polls each valve ID (A-J) with {id}VR\r and counts responses
        builder.RegisterSimple(
            "GetNumberOfValves",
            "Get the number of valves on the Vici Multi-Valve Actuator",
            async ct =>
            {
                var count = 0;
                foreach (var id in new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" })
                {
                    var resp = Decode(await Transport.SendReceiveAsync(Encode($"{id}VR\r"), ct));
                    if (!string.IsNullOrEmpty(resp) && resp.Contains("MAIN"))
                        count++;
                }
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(count));
            });

        // GoTo — "{valve}GO{position}\r"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "GoTo",
                "Move a Vici valve to position A or B",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"valve\":{\"type\":\"string\"},\"position\":{\"type\":\"string\",\"enum\":[\"A\",\"B\"]}},\"required\":[\"valve\",\"position\"]}"),
            async (parameters, _, ct) =>
            {
                var valve = parameters.GetProperty("valve").GetString() ?? "A";
                var pos = parameters.GetProperty("position").GetString() ?? "A";
                if (pos != "A" && pos != "B")
                    return CapabilityInvocationResult.Failed("Position is not correct");
                await Transport.SendReceiveAsync(Encode($"{valve}GO{pos}\r"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // GetCurrentPosition — "{valve}CP\r" → "CPA\x0D"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "GetCurrentPosition",
                "Get the current position (A or B) of a specific valve",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"valve\":{\"type\":\"string\"}},\"required\":[\"valve\"]}"),
            async (parameters, _, ct) =>
            {
                var valve = parameters.GetProperty("valve").GetString() ?? "A";
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"{valve}CP\r"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("No response from device");
                if (resp.Contains("?"))
                    return CapabilityInvocationResult.Failed("Command did not return correct response");
                if (resp.Length < 3)
                    return CapabilityInvocationResult.Failed("Command did not return correct response");
                var pos = resp[2].ToString();
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(pos == "A" ? "A" : "B"));
            });

        // SetDelay — "{valve}DT{ms}\r"
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetDelay",
                "Set the delay interval (ms) for a Vici valve (0-65000)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"valve\":{\"type\":\"string\"},\"milliseconds\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":65000}},\"required\":[\"valve\",\"milliseconds\"]}"),
            async (parameters, _, ct) =>
            {
                var valve = parameters.GetProperty("valve").GetString() ?? "A";
                var ms = parameters.GetProperty("milliseconds").GetInt32();
                if (ms is < 0 or > 65000)
                    return CapabilityInvocationResult.Failed("Delay time set outside of acceptable range");
                await Transport.SendReceiveAsync(Encode($"{valve}DT{ms}\r"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // GetUniqueId
        builder.RegisterSimple(
            "GetUniqueId",
            "Get the unique identifier for the Vici Multi-Valve Actuator",
            ct => Task.FromResult(CapabilityInvocationResult.Failed("Can't get unique information on device")));
    }

    // has_trends=NO — base class default (no-op) is correct; do NOT override StreamTrendsAsync
}
