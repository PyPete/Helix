using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Arduino Signal Tower multi-relay controller.
/// Serial protocol, BaudRate 9600, no command terminator.
/// has_trends=NO — does NOT override StreamTrendsAsync.
/// </summary>
public sealed class ArduinoSignalTower : DeviceAdapterBase
{
    public ArduinoSignalTower(SerialDeviceTransport transport, ILogger<ArduinoSignalTower> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // GetNumberOfValves
        builder.RegisterSimple(
            "GetNumberOfValves",
            "Get the number of relay valves controlled by the Arduino Signal Tower",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("NV"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Number of valves returned incorrectly");
                if (!int.TryParse(resp, out var count))
                    return CapabilityInvocationResult.Failed($"Unexpected response: {resp}");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(count));
            });

        // GoToOn — set relay to ON position (OP{valve})
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "GoToOn",
                "Set a relay to the ON position",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"valve\":{\"type\":\"string\"}},\"required\":[\"valve\"]}"),
            async (parameters, _, ct) =>
            {
                var valve = parameters.GetProperty("valve").GetString() ?? "";
                await Transport.SendReceiveAsync(Encode($"OP{valve}"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // GoToOff — set relay to OFF position (CL{valve})
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "GoToOff",
                "Set a relay to the OFF position",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"valve\":{\"type\":\"string\"}},\"required\":[\"valve\"]}"),
            async (parameters, _, ct) =>
            {
                var valve = parameters.GetProperty("valve").GetString() ?? "";
                await Transport.SendReceiveAsync(Encode($"CL{valve}"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // GetCurrentPosition
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "GetCurrentPosition",
                "Get the current ON/OFF position of a relay",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"valve\":{\"type\":\"string\"}},\"required\":[\"valve\"]}"),
            async (parameters, _, ct) =>
            {
                var valve = parameters.GetProperty("valve").GetString() ?? "";
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"CP{valve}"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Current position returned incorrectly");
                string? position = resp switch
                {
                    "1" => "On",
                    "0" => "Off",
                    _ => null
                };
                if (position is null)
                    return CapabilityInvocationResult.Failed("Current position returned incorrectly");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(position));
            });

        // SetDelay
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetDelay",
                "Set the delay interval for a relay (0-9999 ms)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"valve\":{\"type\":\"string\"},\"milliseconds\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":9999}},\"required\":[\"valve\",\"milliseconds\"]}"),
            async (parameters, _, ct) =>
            {
                var valve = parameters.GetProperty("valve").GetString() ?? "";
                var ms = parameters.GetProperty("milliseconds").GetInt32();
                if (ms is < 0 or > 9999)
                    return CapabilityInvocationResult.Failed("Delay is outside of the range of 0 to 9999");
                await Transport.SendReceiveAsync(Encode($"DT{valve}{ms}"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // SwitchThenReturn
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SwitchThenReturn",
                "Toggle a relay then return after the preset delay",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"valve\":{\"type\":\"string\"}},\"required\":[\"valve\"]}"),
            async (parameters, _, ct) =>
            {
                var valve = parameters.GetProperty("valve").GetString() ?? "";
                await Transport.SendReceiveAsync(Encode($"TT{valve}"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // GetUniqueId
        builder.RegisterSimple(
            "GetUniqueId",
            "Get the serial number of the Arduino Signal Tower",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("SR"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Serial number returned incorrectly");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(resp));
            });
    }

    // has_trends=NO — base class default (no-op) is correct; do NOT override StreamTrendsAsync
}
