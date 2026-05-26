using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Tcp;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Knauer Azura HPLC pump.
/// Communicates over a TCP transport using the Knauer ASCII protocol.
/// </summary>
public sealed class KnauerAzura : DeviceAdapterBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="KnauerAzura"/> using a TCP transport.
    /// </summary>
    /// <param name="transport">The TCP transport to the Knauer Azura device.</param>
    /// <param name="logger">Logger instance.</param>
    public KnauerAzura(TcpDeviceTransport transport, ILogger<KnauerAzura> logger)
        : base(transport, logger)
    {
    }

    /// <summary>
    /// Internal constructor for testing with a mock transport.
    /// </summary>
    /// <param name="transport">The device transport (may be a test double).</param>
    /// <param name="logger">Logger instance.</param>
    internal KnauerAzura(IDeviceTransport transport, ILogger<KnauerAzura> logger)
        : base(transport, logger)
    {
    }

    // ── Encoding helpers ─────────────────────────────────────────────────────

    private static byte[] Encode(string command) => Encoding.ASCII.GetBytes(command + "\r");

    private static string Decode(byte[] response) => Encoding.ASCII.GetString(response).Trim();

    private static bool IsOk(string response) =>
        response.Contains(":OK", StringComparison.OrdinalIgnoreCase);

    // ── Capability registration ───────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // 1. RemoteControl
        builder.RegisterSimple(
            "RemoteControl",
            "Put the KnauerAzura pump into remote control mode",
            async ct =>
            {
                var response = Decode(await Transport.SendReceiveAsync(Encode("REMOTE:60000000"), ct));
                return IsOk(response)
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed(response);
            });

        // 2. Start
        builder.RegisterSimple(
            "Start",
            "Start the KnauerAzura pump method and release hold",
            async ct =>
            {
                var response = Decode(await Transport.SendReceiveAsync(Encode("START:2,2"), ct));
                if (!IsOk(response))
                    return CapabilityInvocationResult.Failed(response);

                var unholdResponse = Decode(await Transport.SendReceiveAsync(Encode("UNHOLD"), ct));
                return IsOk(unholdResponse)
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed(unholdResponse);
            });

        // 3. Stop
        builder.RegisterSimple(
            "Stop",
            "Stop the KnauerAzura pump and all data output",
            async ct =>
            {
                var response = Decode(await Transport.SendReceiveAsync(Encode("STOP:1,1,1"), ct));
                return IsOk(response)
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed(response);
            });

        // 4. SetFlowRate
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetFlowRate",
                "Set pump flow rate and solvent gradient",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"flowRateMlMin\":{\"type\":\"number\",\"minimum\":0,\"maximum\":20},\"percA\":{\"type\":\"number\",\"minimum\":0,\"maximum\":100},\"percB\":{\"type\":\"number\",\"minimum\":0,\"maximum\":100},\"percC\":{\"type\":\"number\",\"minimum\":0,\"maximum\":100},\"percD\":{\"type\":\"number\",\"minimum\":0,\"maximum\":100}},\"required\":[\"flowRateMlMin\",\"percA\",\"percB\",\"percC\",\"percD\"]}"),
            async (parameters, _, ct) =>
            {
                var flowRateMlMin = parameters.GetProperty("flowRateMlMin").GetDouble();
                var percA = parameters.GetProperty("percA").GetDouble();
                var percB = parameters.GetProperty("percB").GetDouble();
                var percC = parameters.GetProperty("percC").GetDouble();
                var percD = parameters.GetProperty("percD").GetDouble();

                if (percA + percB + percC + percD > 100)
                    return CapabilityInvocationResult.Failed("Sum of percentages exceeds 100");

                var flowUlMin = (int)(flowRateMlMin * 1000);
                var cmd = $"RAMP:{flowUlMin},0,{(int)percA},{(int)percB},{(int)percC},{(int)percD},0,0,0,0,0,0,0,0,1";

                var response = Decode(await Transport.SendReceiveAsync(Encode(cmd), ct));
                return IsOk(response)
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed(response);
            });

        // 5. GetPumpStatus
        builder.RegisterSimple(
            "GetPumpStatus",
            "Query KnauerAzura pump state, flow rate, and pressure",
            async ct =>
            {
                var response = Decode(await Transport.SendReceiveAsync(Encode("STATUS?"), ct));
                try
                {
                    var parts = response.Split(':');
                    var fields = parts.Length > 1 ? parts[1].Split(',') : Array.Empty<string>();

                    var pumpStateInt = int.Parse(fields[2]);
                    var stateName = pumpStateInt switch
                    {
                        0 => "Initialising",
                        1 => "Off",
                        2 => "Idle",
                        3 => "Running",
                        4 => "Holding",
                        5 => "Purging",
                        6 => "Standby",
                        8 => "Failed",
                        9 => "RunFinished",
                        10 => "Equilibrating",
                        _ => pumpStateInt.ToString()
                    };

                    var flowRateMlMin = int.Parse(fields[4]) / 1000.0;
                    var percA = double.Parse(fields[5]);
                    var percB = double.Parse(fields[6]);
                    var percC = double.Parse(fields[7]);
                    var percD = double.Parse(fields[8]);
                    var pressureBar = int.Parse(fields[17]) / 100.0;

                    return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new
                    {
                        state = stateName,
                        flowRateMlMin,
                        percA,
                        percB,
                        percC,
                        percD,
                        pressureBar
                    }));
                }
                catch (Exception)
                {
                    return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { raw = response }));
                }
            });

        // 6. GetIdentity
        builder.RegisterSimple(
            "GetIdentity",
            "Query the KnauerAzura pump identity string",
            async ct =>
            {
                var response = Decode(await Transport.SendReceiveAsync(Encode("IDENTIFY?"), ct));
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(response));
            });
    }

    // ── Trend streaming ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            string? statusResponse = null;
            try
            {
                statusResponse = Decode(await Transport.SendReceiveAsync(Encode("STATUS?"), cancellationToken));
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "KnauerAzura: failed to poll STATUS?; skipping trend sample");
                continue;
            }

            if (string.IsNullOrWhiteSpace(statusResponse))
                continue;

            var now = DateTimeOffset.UtcNow;

            double flowRateMlMin;
            double pressureBar;
            string stateName;

            try
            {
                var parts = statusResponse.Split(':');
                var fields = parts.Length > 1 ? parts[1].Split(',') : Array.Empty<string>();

                var pumpStateInt = int.Parse(fields[2]);
                stateName = pumpStateInt switch
                {
                    0 => "Initialising",
                    1 => "Off",
                    2 => "Idle",
                    3 => "Running",
                    4 => "Holding",
                    5 => "Purging",
                    6 => "Standby",
                    8 => "Failed",
                    9 => "RunFinished",
                    10 => "Equilibrating",
                    _ => pumpStateInt.ToString()
                };

                flowRateMlMin = int.Parse(fields[4]) / 1000.0;
                pressureBar = int.Parse(fields[17]) / 100.0;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "KnauerAzura: failed to parse STATUS? response '{Response}'; skipping trend sample", statusResponse);
                continue;
            }

            yield return new TrendSample
            {
                TrendName = "knauer.pump.flowrate",
                Value = flowRateMlMin,
                Timestamp = now,
                Unit = "ml/min"
            };

            yield return new TrendSample
            {
                TrendName = "knauer.pump.pressure",
                Value = pressureBar,
                Timestamp = now,
                Unit = "bar"
            };

            yield return new TrendSample
            {
                TrendName = "knauer.pump.state",
                Value = stateName,
                Timestamp = now,
                Unit = null
            };
        }
    }
}
