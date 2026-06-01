using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Tcp;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Microsaic MiD mass spectrometer.
/// Communicates over a TCP transport using the Masscape HTTP-style ASCII protocol.
/// Command terminator is "\n\n" (two newlines), matching POP.DeviceCore.Drivers.Microsaic CmdEnd.
/// </summary>
public sealed class Microsaic : DeviceAdapterBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="Microsaic"/> using a TCP transport.
    /// </summary>
    /// <param name="transport">The TCP transport to the Microsaic device.</param>
    /// <param name="logger">Logger instance.</param>
    public Microsaic(TcpDeviceTransport transport, ILogger<Microsaic> logger)
        : base(transport, logger)
    {
    }

    /// <summary>
    /// Internal constructor for testing with a mock transport.
    /// </summary>
    /// <param name="transport">The device transport (may be a test double).</param>
    /// <param name="logger">Logger instance.</param>
    internal Microsaic(IDeviceTransport transport, ILogger<Microsaic> logger)
        : base(transport, logger)
    {
    }

    // ── Encoding helpers ──────────────────────────────────────────────────────

    /// <summary>Encodes a command string to ASCII bytes, appending the "\n\n" terminator (CmdEnd).</summary>
    private static byte[] Encode(string command) => Encoding.ASCII.GetBytes(command + "\n\n");

    /// <summary>Decodes a response byte array to an ASCII string with newlines stripped.</summary>
    private static string Decode(byte[] response) => Encoding.ASCII.GetString(response).Replace("\n", string.Empty).Trim();

    /// <summary>Decodes a response byte array to an ASCII string preserving newlines (for multi-line parsing).</summary>
    private static string DecodeLines(byte[] response) => Encoding.ASCII.GetString(response).Trim();

    // ── Capability registration ───────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // 1. GetAllStatusDetails
        builder.RegisterSimple(
            "GetAllStatusDetails",
            "Get all Microsaic MiD device status details (MiDas, makeup pump, acquisition parameters, method settings)",
            async ct =>
            {
                // Get MiDas status
                var midasResp = await WaitForResponseAsync("GET MIDAS_STATUS", "Run", ct);
                if (midasResp is null)
                    return CapabilityInvocationResult.Failed("Could not get device status");

                // Get makeup pump status
                var makeupResp = await WaitForMakeupPumpAsync(ct);
                if (makeupResp is null)
                    return CapabilityInvocationResult.Failed("Could not get device status");

                // Get acquisition parameters
                var acqResp = await WaitForResponseAsync("GET ACQUISITION_PARAMETERS", "Ion", ct);
                if (acqResp is null)
                    return CapabilityInvocationResult.Failed("Could not get device status");

                // Get method settings
                var methodBytes = await Transport.SendReceiveAsync(Encode("GET METHOD_SETTINGS\nEIC"), ct);
                var methodResp = DecodeLines(methodBytes);
                var stringOkMethod = string.IsNullOrEmpty(methodResp) || !methodResp.Contains("Overall");
                if (stringOkMethod)
                    return CapabilityInvocationResult.Failed("Could not get device status");

                // Parse acquisition ion mode
                var acqLines = acqResp.Split('\n');
                var ionMode = acqLines[0].Split(':').Length > 1 ? acqLines[0].Split(':')[1].Trim() : string.Empty;
                var tuneMode = ionMode.Equals("NEGATIVE", StringComparison.OrdinalIgnoreCase) ? "Negative" : "Positive";

                // Parse method settings
                var methodLines = methodResp.Split('\n');
                var start = TryParseDoubleFromLine(methodLines, 6);
                var stop = TryParseDoubleFromLine(methodLines, 7);
                var step = TryParseDoubleFromLine(methodLines, 8);
                var scanTime = TryParseDoubleFromLine(methodLines, 9);

                // Parse makeup pump
                var makeupLines = makeupResp.Split('\n');
                var onOff = makeupLines[0];
                double.TryParse(makeupLines[1].Trim(), out var makeupFlow);
                double.TryParse(makeupLines[2].Trim(), out var makeupPressure);

                // Parse MiDas status
                var idx = midasResp.IndexOf("Run", StringComparison.InvariantCulture);
                var midasTrimmed = midasResp[idx..].Replace("TRUE\n\n", string.Empty);
                var midasLines = midasTrimmed.Split('\n');
                var runState = midasLines[0].Split(':').Length > 1 ? midasLines[0].Split(':')[1].Trim() : string.Empty;

                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new
                {
                    runState,
                    makeupPumpOn = onOff.Equals("ON", StringComparison.OrdinalIgnoreCase),
                    makeupFlowUlMin = makeupFlow,
                    makeupPressureBar = makeupPressure,
                    ionMode = tuneMode,
                    lowerMz = start,
                    upperMz = stop,
                    massStep = step,
                    scanTimeMs = scanTime
                }));
            });

        // 2. SetAttenuation
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetAttenuation",
                "Set the MiDas attenuator level",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"attenuation\":{\"type\":\"integer\"}},\"required\":[\"attenuation\"]}"),
            async (parameters, _, ct) =>
            {
                var attenuation = parameters.GetProperty("attenuation").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"PUT SET_MIDAS_ATTENUATOR\n{attenuation}\nFALSE"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Set attenuation may not have worked correctly");
                return resp == "OK"
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed("Set attenuation may not have worked correctly");
            });

        // 3. StartMra
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "StartMra",
                "Start the MiDas MRA (Multiple Reaction Attenuator) at a given attenuation level",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"attenuation\":{\"type\":\"integer\"}},\"required\":[\"attenuation\"]}"),
            async (parameters, _, ct) =>
            {
                var attenuation = parameters.GetProperty("attenuation").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"PUT SET_MIDAS_ATTENUATOR\n{attenuation}\nTRUE"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Mra may not have started correctly");
                return resp == "OK"
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed("Mra may not have started correctly");
            });

        // 4. StopMra
        builder.RegisterSimple(
            "StopMra",
            "Stop the MiDas MRA (Multiple Reaction Attenuator)",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("PUT MIDAS_ATTENUATOR\nSTOP"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Mra may not have stopped correctly");
                return resp == "OK"
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed("Mra may not have stopped correctly");
            });

        // 5. SetMakeUpFlowRate
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetMakeUpFlowRate",
                "Set the MiDas makeup pump flow rate in ul/min",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"flowRateUlMin\":{\"type\":\"integer\",\"minimum\":0}},\"required\":[\"flowRateUlMin\"]}"),
            async (parameters, _, ct) =>
            {
                var flowRate = parameters.GetProperty("flowRateUlMin").GetInt32();
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"PUT SET_MIDAS_MAKEUP_PUMP_FLOW_RATE\n{flowRate}"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Set Makeup flow may not have executed correctly");
                return resp == "OK"
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed("Set Makeup flow may not have executed correctly");
            });

        // 6. StartMakeUpFlow
        builder.RegisterSimple(
            "StartMakeUpFlow",
            "Start the MiDas makeup pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("PUT MIDAS_MAKEUP_PUMP\nSTART"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Makeup flow may not have started correctly");
                return resp == "OK"
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed("Makeup flow may not have started correctly");
            });

        // 7. StopMakeUpFlow
        builder.RegisterSimple(
            "StopMakeUpFlow",
            "Stop the MiDas makeup pump",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("PUT MIDAS_MAKEUP_PUMP\nSTOP"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Makeup flow may not have stopped correctly");
                return resp == "OK"
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed("Makeup flow may not have stopped correctly");
            });

        // 8. InitialiseFullScanParameters
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "InitialiseFullScanParameters",
                "Set full scan acquisition parameters (mass range, step, scan time, tune mode)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"lowerMz\":{\"type\":\"number\"},\"upperMz\":{\"type\":\"number\"},\"massStep\":{\"type\":\"number\"},\"scanTimeMs\":{\"type\":\"number\"},\"tuneMode\":{\"type\":\"string\",\"enum\":[\"Positive\",\"Negative\"]}},\"required\":[\"lowerMz\",\"upperMz\",\"massStep\",\"scanTimeMs\",\"tuneMode\"]}"),
            async (parameters, _, ct) =>
            {
                var lowerMz = parameters.GetProperty("lowerMz").GetDouble();
                var upperMz = parameters.GetProperty("upperMz").GetDouble();
                var massStep = parameters.GetProperty("massStep").GetDouble();
                var scanTime = parameters.GetProperty("scanTimeMs").GetDouble();
                var tuneMode = parameters.GetProperty("tuneMode").GetString() ?? "Negative";

                // Calculate count time (from POP source, page 113 formula)
                const double settleTime = 0.05;
                const double endOfScanDwell = 100;
                var numPoints = (upperMz - lowerMz) / massStep + 1;
                var countTime = (scanTime - endOfScanDwell) / numPoints - settleTime;

                var spectrumCmd = $"PUT SPECTRUM_PARAMETERS\n{lowerMz}\n{upperMz}\n{massStep}\n{countTime}";
                var resp = Decode(await Transport.SendReceiveAsync(Encode(spectrumCmd), ct));

                var tuneTune = tuneMode.Equals("Negative", StringComparison.OrdinalIgnoreCase)
                    ? "PUT ACQUISITION_PARAMETERS\nnegative.tun\nTRUE"
                    : "PUT ACQUISITION_PARAMETERS\npositive.tun\nTRUE";
                await Transport.SendReceiveAsync(Encode(tuneTune), ct);

                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Initialisation of the full scan may not have been successful");
                return resp == "OK"
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed("Initialisation of the full scan may not have been successful");
            });

        // 9. StopDataAcquisition
        builder.RegisterSimple(
            "StopDataAcquisition",
            "Stop the current immediate data acquisition",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("PUT STOP_IMMEDIATE"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Stop data acquisition may not have been successfully");
                return resp != "FALSE"
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed("Stop data acquisition may not have been successfully");
            });
    }

    // ── Trend streaming ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Start immediate acquisition
        await Transport.SendReceiveAsync(
            Encode("PUT ACQUIRING_IMMEDIATE\nSPECTRUM\nREPETITIVE\nPopExp"),
            cancellationToken).ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            byte[]? rawData = null;
            try
            {
                rawData = await Transport.SendReceiveAsync(Encode("GET LIVE_DATA"), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Microsaic: failed to poll GET LIVE_DATA; skipping trend sample");
                continue;
            }

            if (rawData == null) continue;

            var data = DecodeLines(rawData);

            if (data.Contains("NO_DATA") || string.IsNullOrEmpty(data))
                continue;

            var now = DateTimeOffset.UtcNow;

            TrendSample? sample = null;
            try
            {
                // Parse spectrum data: lines 5..^4 contain "mass intensity" pairs
                var splitData = data.Split('\n');
                if (splitData.Length < 6) continue;

                var intensities = new List<double>();
                foreach (var dataPoint in splitData[5..^4])
                {
                    var tmpSplit = dataPoint.Split(' ');
                    if (double.TryParse(tmpSplit[^1], out var dbl))
                        intensities.Add(dbl);
                }

                if (intensities.Count == 0) continue;

                // Serialize as JSON array for the Value field
                sample = new TrendSample
                {
                    TrendName = "microsaic.spectrum.raw",
                    Value = JsonSerializer.Serialize(intensities),
                    Timestamp = now,
                    Unit = "counts"
                };
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Microsaic: failed to parse GET LIVE_DATA response; skipping trend sample");
            }

            if (sample is not null)
                yield return sample;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Polls a command until the response contains <paramref name="expectedToken"/> or times out (5 s).
    /// Returns null on timeout.
    /// </summary>
    private async Task<string?> WaitForResponseAsync(string command, string expectedToken, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        string? resp = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var bytes = await Transport.SendReceiveAsync(Encode(command), ct);
            resp = DecodeLines(bytes);
            if (!string.IsNullOrEmpty(resp) && resp.Contains(expectedToken))
                return resp;
        }
        return null;
    }

    /// <summary>
    /// Polls GET MIDAS_MAKEUP_PUMP_STATUS until response contains "ON" or "OFF" or times out (5 s).
    /// Returns null on timeout.
    /// </summary>
    private async Task<string?> WaitForMakeupPumpAsync(CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var bytes = await Transport.SendReceiveAsync(Encode("GET MIDAS_MAKEUP_PUMP_STATUS"), ct);
            var resp = DecodeLines(bytes);
            if (!string.IsNullOrEmpty(resp) && (resp.Contains("ON") || resp.Contains("OFF")))
                return resp;
        }
        return null;
    }

    /// <summary>
    /// Parses a double from a colon-separated line in the format "Label : value unit".
    /// Returns 0 on parse failure.
    /// </summary>
    private static double TryParseDoubleFromLine(string[] lines, int lineIndex)
    {
        if (lineIndex >= lines.Length) return 0;
        var parts = lines[lineIndex].Split(':');
        if (parts.Length < 2) return 0;
        var valuePart = parts[1].Split(' ');
        if (valuePart.Length < 2) return 0;
        return double.TryParse(valuePart[1], out var result) ? result : 0;
    }
}
