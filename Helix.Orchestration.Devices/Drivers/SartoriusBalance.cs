using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Sartorius precision balance.
/// Serial protocol, BaudRate 1200, DataBits 7, Parity Odd, command terminator \r\n.
/// Commands use ESC (\x1B) prefix: "\x1BP\r\n" (weight), "\x1BT\r\n" (tare), "\x1Bx1_\r\n" (serial).
/// </summary>
public sealed class SartoriusBalance : DeviceAdapterBase
{
    private const string Esp = "\x1B";

    public SartoriusBalance(SerialDeviceTransport transport, ILogger<SartoriusBalance> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r); // Do NOT trim - Sartorius response length is significant

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // GetWeight — "\x1BP\r\n" → "N     -     0.04   g\r\n" (22 chars)
        builder.RegisterSimple(
            "GetWeight",
            "Get the current weight reading from the Sartorius balance",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"{Esp}P\r\n"), ct));
                var (success, weight, unit) = InterpretWeightString(resp);
                if (!success)
                    return CapabilityInvocationResult.Failed("Balance did not return correct value");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { weight, unit }));
            });

        // Tare — "\x1BT\r\n"
        builder.RegisterSimple(
            "Tare",
            "Tare the Sartorius balance",
            async ct =>
            {
                await Transport.SendReceiveAsync(Encode($"{Esp}T\r\n"), ct);
                return CapabilityInvocationResult.Successful();
            });

        // GetUniqueId — "\x1Bx1_\r\n"
        builder.RegisterSimple(
            "GetUniqueId",
            "Get the model/serial number of the Sartorius balance",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode($"{Esp}x1_\r\n"), ct));
                if (string.IsNullOrEmpty(resp))
                    return CapabilityInvocationResult.Failed("Unique Id could not be found");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(resp.Replace("\r\n", "")));
            });
    }

    /// <summary>
    /// Parse Sartorius balance weight string.
    /// Expected: 22-char string like "N     -     0.04   g\r\n"
    /// Fields: 0..5 = status, 6..15 = weight value, 17..18 = unit
    /// </summary>
    private static (bool success, double weight, string unit) InterpretWeightString(string resp)
    {
        if (resp.Length < 20) return (false, 0, string.Empty);
        var clean = resp.Replace("+", "");
        if (!double.TryParse(clean[6..16].Replace(" ", ""), out var weight)) return (false, 0, string.Empty);
        var unit = clean[17..19];
        return (true, weight, unit);
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            string? resp = null;
            try { resp = Decode(await Transport.SendReceiveAsync(Encode($"{Esp}P\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "SartoriusBalance: failed to poll P"); }

            if (!string.IsNullOrEmpty(resp))
            {
                var (success, weight, unit) = InterpretWeightString(resp);
                if (success)
                    yield return new TrendSample { TrendName = "sartoriusbalance.weight", Value = weight, Timestamp = now, Unit = unit };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}

