using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Alicat mass flow controller.
/// Serial protocol, BaudRate 19200, command terminator \r.
/// </summary>
public sealed class Alicat : DeviceAdapterBase
{
    private double _setPoint;

    public Alicat(SerialDeviceTransport transport, ILogger<Alicat> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Start — no hardware command (returns success immediately per POP source)
        builder.RegisterSimple(
            "Start",
            "Start the Alicat mass flow controller (no hardware action)",
            ct => Task.FromResult(CapabilityInvocationResult.Successful()));

        // Stop — no hardware command
        builder.RegisterSimple(
            "Stop",
            "Stop the Alicat mass flow controller (no hardware action)",
            ct => Task.FromResult(CapabilityInvocationResult.Successful()));

        // SetPoint
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetPoint",
                "Set the flow setpoint on the Alicat",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"setpoint\":{\"type\":\"number\"}},\"required\":[\"setpoint\"]}"),
            async (parameters, _, ct) =>
            {
                var setpoint = parameters.GetProperty("setpoint").GetDouble();
                _setPoint = setpoint;
                await Transport.SendReceiveAsync(Encode($"As{setpoint}\r"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // GetSetPoint
        builder.RegisterSimple(
            "GetSetPoint",
            "Get the current flow setpoint from the Alicat",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(_setPoint))));

        // GetSetPointLimits
        builder.RegisterSimple(
            "GetSetPointLimits",
            "Get the minimum and maximum flow setpoint limits for the Alicat",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { min = 0.0, max = 10.0 }))));

        // TareAll — no hardware action
        builder.RegisterSimple(
            "TareAll",
            "Tare all readings on the Alicat (no hardware action)",
            ct => Task.FromResult(CapabilityInvocationResult.Successful()));
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            string? resp = null;
            try { resp = Decode(await Transport.SendReceiveAsync(Encode("A\r"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "Alicat: failed to poll A"); }

            if (!string.IsNullOrEmpty(resp))
            {
                var parts = resp.Split(' ');
                if (parts.Length >= 7)
                {
                    if (double.TryParse(parts[1], out var pressure))
                        yield return new TrendSample { TrendName = "alicat.pressure", Value = pressure, Timestamp = now, Unit = "bara" };
                    if (double.TryParse(parts[2], out var temp))
                        yield return new TrendSample { TrendName = "alicat.temperature", Value = temp, Timestamp = now, Unit = "C" };
                    if (double.TryParse(parts[3], out var volFlow))
                        yield return new TrendSample { TrendName = "alicat.volumetricflow", Value = volFlow, Timestamp = now, Unit = "ml/min" };
                    if (double.TryParse(parts[4], out var massFlow))
                        yield return new TrendSample { TrendName = "alicat.massflow", Value = massFlow, Timestamp = now, Unit = "g/h" };
                    if (double.TryParse(parts[5], out var setpoint))
                        yield return new TrendSample { TrendName = "alicat.setpointflow", Value = setpoint, Timestamp = now, Unit = "g/h" };
                }
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
