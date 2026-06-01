using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Arduino Valves multi-relay controller.
/// Serial protocol, BaudRate 9600, no command terminator.
/// has_trends=NO — does NOT override StreamTrendsAsync.
/// Note: uses numeric valve indices (0-based) for CP/OP/CL commands.
/// </summary>
public sealed class ArduinoValves : DeviceAdapterBase
{
    public ArduinoValves(SerialDeviceTransport transport, ILogger<ArduinoValves> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // GetNumberOfValves
        builder.RegisterSimple(
            "GetNumberOfValves",
            "Get the number of relay valves controlled by the Arduino Valves",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("NV"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Number of valves returned incorrectly");
                if (!int.TryParse(resp, out var count))
                    return CapabilityInvocationResult.Failed($"Unexpected response: {resp}");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(count));
            });

        // GoToOpen — OP{numericIndex}
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "GoToOpen",
                "Set a relay to the OPEN position (OP{index})",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"valve\":{\"type\":\"integer\",\"minimum\":0}},\"required\":[\"valve\"]}"),
            async (parameters, _, ct) =>
            {
                var valve = parameters.GetProperty("valve").GetInt32();
                await Transport.SendReceiveAsync(Encode($"OP{valve}"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // GoToClosed — CL{numericIndex}
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "GoToClosed",
                "Set a relay to the CLOSED position (CL{index})",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"valve\":{\"type\":\"integer\",\"minimum\":0}},\"required\":[\"valve\"]}"),
            async (parameters, _, ct) =>
            {
                var valve = parameters.GetProperty("valve").GetInt32();
                await Transport.SendReceiveAsync(Encode($"CL{valve}"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // GetCurrentPosition — CP{numericIndex} returns "1"=Closed "0"=Open
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "GetCurrentPosition",
                "Get the current Open/Closed position of a relay (CP{index})",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"valve\":{\"type\":\"integer\",\"minimum\":0}},\"required\":[\"valve\"]}"),
            async (parameters, _, ct) =>
            {
                var valve = parameters.GetProperty("valve").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"CP{valve}"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Current position returned incorrectly");
                string? position = null;
                if (resp.Contains("1")) position = "Closed";
                else if (resp.Contains("0")) position = "Open";
                if (position is null)
                    return CapabilityInvocationResult.Failed("Current position returned incorrectly");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(position));
            });

        // GetUniqueId
        builder.RegisterSimple(
            "GetUniqueId",
            "Get the serial number of the Arduino Valves",
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
