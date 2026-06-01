using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Mettler Toledo balance.
/// Serial protocol, BaudRate 9600, command terminator \r\n.
/// Tare uses "ZI\r\n" (→ "Z A"), GetWeight uses "SI\r\n" (→ "S S 100.00 g").
/// </summary>
public sealed class MettlerBalance : DeviceAdapterBase
{
    private string _uniqueId = string.Empty;

    public MettlerBalance(SerialDeviceTransport transport, ILogger<MettlerBalance> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Tare — "ZI\r\n" → "Z A"
        builder.RegisterSimple(
            "Tare",
            "Tare the Mettler Toledo balance (zero it)",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("ZI\r\n"), ct));
                if (resp.TrimEnd('\r', '\n') == "Z A")
                    return CapabilityInvocationResult.Successful();
                return CapabilityInvocationResult.Failed("Balancing zero may not have worked correctly");
            });

        // GetWeightNow — "SI\r\n" → "S S 100.00 g"
        builder.RegisterSimple(
            "GetWeightNow",
            "Get the current weight reading from the Mettler Toledo balance",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("SI\r\n"), ct));
                var (success, weight, unit) = ParseWeightResponse(resp);
                if (!success)
                    return CapabilityInvocationResult.Failed("Balance did not return correct value");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { weight, unit }));
            });

        // GetUniqueId
        builder.RegisterSimple(
            "GetUniqueId",
            "Get the serial number of the Mettler Toledo balance",
            ct => Task.FromResult(CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(_uniqueId))));
    }

    private static (bool success, double weight, string unit) ParseWeightResponse(string resp)
    {
        var clean = resp.Replace("\r\n", "");
        var parts = clean.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 2) return (false, 0, string.Empty);
        if (!double.TryParse(parts[2], out var weight)) return (false, 0, string.Empty);
        var unit = parts.Length >= 4 ? parts[3] : "g";
        return (true, weight, unit);
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            string? resp = null;
            try { resp = Decode(await Transport.SendReceiveAsync(Encode("SI\r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "MettlerBalance: failed to poll SI"); }

            if (!string.IsNullOrEmpty(resp))
            {
                var (success, weight, unit) = ParseWeightResponse(resp);
                if (success)
                    yield return new TrendSample { TrendName = "mettlerbalance.weight", Value = weight, Timestamp = now, Unit = unit };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
