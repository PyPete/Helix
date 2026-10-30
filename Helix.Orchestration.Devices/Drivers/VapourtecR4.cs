using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Vapourtec R4 flow chemistry platform.
/// Communicates over a serial transport using the Vapourtec ASCII protocol.
/// </summary>
public sealed class VapourtecR4 : DeviceAdapterBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="VapourtecR4"/>.
    /// </summary>
    /// <param name="transport">The serial transport to the R4 device.</param>
    /// <param name="logger">Logger instance.</param>
    public VapourtecR4(SerialDeviceTransport transport, ILogger<VapourtecR4> logger)
        : base(transport, logger)
    {
    }

    // ── Encoding helpers ─────────────────────────────────────────────────────

    private static byte[] Encode(string command) => Encoding.ASCII.GetBytes(command);

    private static string Decode(byte[] response) => Encoding.ASCII.GetString(response).Trim();

    // ── Capability registration ───────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // 1. Start
        builder.RegisterSimple(
            "Start",
            "Start the Vapourtec R4 pumps running",
            async ct =>
            {
                var resp1 = Decode(await Transport.SendReceiveAsync(Encode("PN \r\n"), ct));
                if (resp1.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed($"Device error: {resp1}");
                if (!resp1.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed(resp1);

                var resp2 = Decode(await Transport.SendReceiveAsync(Encode("FW PN \r\n"), ct));
                if (resp2.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed($"Device error: {resp2}");

                return CapabilityInvocationResult.Successful();
            });

        // 2. Stop
        builder.RegisterSimple(
            "Stop",
            "Stop the Vapourtec R4 pumps",
            async ct =>
            {
                var resp1 = Decode(await Transport.SendReceiveAsync(Encode("PF\r\n"), ct));
                if (resp1.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed($"Device error: {resp1}");
                if (!resp1.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed(resp1);

                var resp2 = Decode(await Transport.SendReceiveAsync(Encode("FW PF\r\n"), ct));
                if (resp2.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed($"Device error: {resp2}");

                return CapabilityInvocationResult.Successful();
            });

        // 3. SetFlowRate
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetFlowRate",
                "Set the flow rate for a pump channel (0-3) in ml/min (0.0-9.99)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"pump\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":3},\"rateMlMin\":{\"type\":\"number\",\"minimum\":0,\"maximum\":9.99}},\"required\":[\"pump\",\"rateMlMin\"]}"),
            async (parameters, _, ct) =>
            {
                var pump = parameters.GetProperty("pump").GetInt32();
                var rateMlMin = parameters.GetProperty("rateMlMin").GetDouble();

                if (pump < 0 || pump > 3)
                    return CapabilityInvocationResult.Failed($"Pump index {pump} is out of range [0,3]");

                rateMlMin = Math.Clamp(rateMlMin, 0.0, 9.99);
                var rateInt = (int)(rateMlMin * 1000);

                string cmd;
                if (pump <= 1)
                    cmd = $"FR {pump} {rateInt} \r\n";
                else
                    cmd = $"FW FR {pump - 2} {rateInt} \r\n";

                var resp = Decode(await Transport.SendReceiveAsync(Encode(cmd), ct));
                if (resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed($"Device error: {resp}");
                if (!resp.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed(resp);

                return CapabilityInvocationResult.Successful();
            });

        // 4. SetTemperature
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetTemperature",
                "Set the target temperature for a reactor (0-150 deg C)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"reactor\":{\"type\":\"integer\"},\"targetCelsius\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":150}},\"required\":[\"reactor\",\"targetCelsius\"]}"),
            async (parameters, _, ct) =>
            {
                var reactor = parameters.GetProperty("reactor").GetInt32();
                var targetCelsius = parameters.GetProperty("targetCelsius").GetInt32();

                if (targetCelsius < 0 || targetCelsius > 150)
                    return CapabilityInvocationResult.Failed($"targetCelsius {targetCelsius} is out of range [0,150]");

                var cmd = $"R4 ST {reactor} {targetCelsius} \r\n";
                var resp = Decode(await Transport.SendReceiveAsync(Encode(cmd), ct));
                if (resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed($"Device error: {resp}");
                if (!resp.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed(resp);

                return CapabilityInvocationResult.Successful();
            });

        // 5. SelectReagent
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SelectReagent",
                "Select the reagent valve position for a channel (0-3)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"channel\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":3}},\"required\":[\"channel\"]}"),
            async (parameters, _, ct) =>
            {
                var channel = parameters.GetProperty("channel").GetInt32();
                string? cmd = channel switch
                {
                    0 => "KP 1\r\n",
                    1 => "KP 3\r\n",
                    2 => "FW KP 1\r\n",
                    3 => "FW KP 3\r\n",
                    _ => null
                };
                if (cmd is null)
                    return CapabilityInvocationResult.Failed($"Channel {channel} is out of range [0,3]");

                var resp = Decode(await Transport.SendReceiveAsync(Encode(cmd), ct));
                if (resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed($"Device error: {resp}");
                if (!resp.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed(resp);

                return CapabilityInvocationResult.Successful();
            });

        // 6. SelectSolvent
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SelectSolvent",
                "Select the solvent valve position for a channel (0-3)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"channel\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":3}},\"required\":[\"channel\"]}"),
            async (parameters, _, ct) =>
            {
                var channel = parameters.GetProperty("channel").GetInt32();
                string? cmd = channel switch
                {
                    0 => "KP 0\r\n",
                    1 => "KP 2\r\n",
                    2 => "FW KP 0\r\n",
                    3 => "FW KP 2\r\n",
                    _ => null
                };
                if (cmd is null)
                    return CapabilityInvocationResult.Failed($"Channel {channel} is out of range [0,3]");

                var resp = Decode(await Transport.SendReceiveAsync(Encode(cmd), ct));
                if (resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed($"Device error: {resp}");
                if (!resp.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed(resp);

                return CapabilityInvocationResult.Successful();
            });

        // 7. SelectLoopFill
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SelectLoopFill",
                "Select the loop fill valve position for a loop (0-3)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"loop\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":3}},\"required\":[\"loop\"]}"),
            async (parameters, _, ct) =>
            {
                var loop = parameters.GetProperty("loop").GetInt32();
                string? cmd = loop switch
                {
                    0 => "KP 4\r\n",
                    1 => "KP 6\r\n",
                    2 => "FW KP 4\r\n",
                    3 => "FW KP 6\r\n",
                    _ => null
                };
                if (cmd is null)
                    return CapabilityInvocationResult.Failed($"Loop {loop} is out of range [0,3]");

                var resp = Decode(await Transport.SendReceiveAsync(Encode(cmd), ct));
                if (resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed($"Device error: {resp}");
                if (!resp.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed(resp);

                return CapabilityInvocationResult.Successful();
            });

        // 8. SelectLoopInject
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SelectLoopInject",
                "Select the loop inject valve position for a loop (0-3)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"loop\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":3}},\"required\":[\"loop\"]}"),
            async (parameters, _, ct) =>
            {
                var loop = parameters.GetProperty("loop").GetInt32();
                string? cmd = loop switch
                {
                    0 => "KP 5\r\n",
                    1 => "KP 7\r\n",
                    2 => "FW KP 5\r\n",
                    3 => "FW KP 7\r\n",
                    _ => null
                };
                if (cmd is null)
                    return CapabilityInvocationResult.Failed($"Loop {loop} is out of range [0,3]");

                var resp = Decode(await Transport.SendReceiveAsync(Encode(cmd), ct));
                if (resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed($"Device error: {resp}");
                if (!resp.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed(resp);

                return CapabilityInvocationResult.Successful();
            });

        // 9. GetFirmwareVersion
        builder.RegisterSimple(
            "GetFirmwareVersion",
            "Query the R4 firmware version string",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("GV\r\n"), ct));
                if (resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed($"Device error: {resp}");

                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(resp));
            });

        // 10. GetTemperatures
        builder.RegisterSimple(
            "GetTemperatures",
            "Query reactor temperatures A-D from the R4 (only valid readings returned)",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("HT\r\n"), ct));
                if (resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    return CapabilityInvocationResult.Failed($"Device error: {resp}");

                var jsonElement = ParseTemperatures(resp);
                return CapabilityInvocationResult.Successful(jsonElement);
            });
    }

    // ── Parsing helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Parses an HT response into a JSON object with reactor temperature readings.
    /// Response format: entries separated by '&amp;'; each entry is comma-separated fields
    /// where field 0 is timestamp and fields [2,4,6,8] are reactor temps (x10).
    /// </summary>
    private static JsonElement ParseTemperatures(string response)
    {
        var entry = response
            .Split('&')
            .LastOrDefault(e => !string.IsNullOrWhiteSpace(e));

        var dict = new Dictionary<string, double>();

        if (entry is not null)
        {
            var fields = entry.Split(',');
            (int index, string label)[] reactorMap =
            [
                (2, "A"),
                (4, "B"),
                (6, "C"),
                (8, "D"),
            ];

            foreach (var (idx, label) in reactorMap)
            {
                if (idx < fields.Length &&
                    double.TryParse(fields[idx], out var raw) &&
                    raw is not -1000.0)
                {
                    dict[label] = raw / 10.0;
                }
            }
        }

        return JsonSerializer.SerializeToElement(dict);
    }

    /// <summary>
    /// Parses the last entry of an HF response for pump flow rates.
    /// Field indices [1] and [2] represent pump A and B flow (raw / 1000 = ml/min).
    /// </summary>
    private static (double? pumpA, double? pumpB) ParseFlowRates(string response)
    {
        var entry = response
            .Split('&')
            .LastOrDefault(e => !string.IsNullOrWhiteSpace(e));

        if (entry is null)
            return (null, null);

        var fields = entry.Split(',');

        double? pumpA = null;
        double? pumpB = null;

        if (fields.Length > 1 && double.TryParse(fields[1], out var rawA))
            pumpA = rawA / 1000.0;

        if (fields.Length > 2 && double.TryParse(fields[2], out var rawB))
            pumpB = rawB / 1000.0;

        return (pumpA, pumpB);
    }

    // ── Trend streaming ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            // Temperature trends (HT)
            string? htResponse = null;
            try
            {
                htResponse = Decode(await Transport.SendReceiveAsync(Encode("HT\r\n"), cancellationToken));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "VapourtecR4: failed to poll HT; skipping temperature trends");
            }

            if (!string.IsNullOrWhiteSpace(htResponse) &&
                !htResponse.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
            {
                var entry = htResponse
                    .Split('&')
                    .LastOrDefault(e => !string.IsNullOrWhiteSpace(e));

                if (entry is not null)
                {
                    var fields = entry.Split(',');
                    (int index, string label)[] reactorMap =
                    [
                        (2, "A"),
                        (4, "B"),
                        (6, "C"),
                        (8, "D"),
                    ];

                    foreach (var (idx, label) in reactorMap)
                    {
                        if (idx < fields.Length &&
                            double.TryParse(fields[idx], out var raw) &&
                            raw is not -1000.0)
                        {
                            yield return new TrendSample
                            {
                                TrendName = $"vapourtec.temperature.{label}",
                                Value = raw / 10.0,
                                Timestamp = now,
                                Unit = "C"
                            };
                        }
                    }
                }
            }

            // Flow trends (HF)
            string? hfResponse = null;
            try
            {
                hfResponse = Decode(await Transport.SendReceiveAsync(Encode("HF\r\n"), cancellationToken));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "VapourtecR4: failed to poll HF; skipping flow trends");
            }

            if (!string.IsNullOrWhiteSpace(hfResponse) &&
                !hfResponse.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
            {
                var (pumpA, pumpB) = ParseFlowRates(hfResponse);

                if (pumpA.HasValue)
                    yield return new TrendSample
                    {
                        TrendName = "vapourtec.flow.pumpA",
                        Value = pumpA.Value,
                        Timestamp = now,
                        Unit = "ml/min"
                    };

                if (pumpB.HasValue)
                    yield return new TrendSample
                    {
                        TrendName = "vapourtec.flow.pumpB",
                        Value = pumpB.Value,
                        Timestamp = now,
                        Unit = "ml/min"
                    };
            }

            // Poll every 2 seconds
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
        }
    }
}
