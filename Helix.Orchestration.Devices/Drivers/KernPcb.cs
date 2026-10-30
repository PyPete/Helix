using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Kern PCB precision balance.
/// Serial protocol, BaudRate 9600, CmdTerminator \r\n (commands use " \r\n" suffix).
/// </summary>
public sealed class KernPcb : DeviceAdapterBase
{
    private static readonly SearchValues<char> SMyChars = SearchValues.Create("0123456789");

    public KernPcb(SerialDeviceTransport transport, ILogger<KernPcb> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // GetWeightNow — "w \r\n"
        builder.RegisterSimple(
            "GetWeightNow",
            "Get the current weight reading from the Kern PCB balance (stable or unstable)",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("w \r\n"), ct));
                var (success, weight, unit) = InterpretString(resp);
                if (!success)
                    return CapabilityInvocationResult.Failed("Could not get value");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { weight, unit }));
            });

        // GetStableWeight — "s \r\n"
        builder.RegisterSimple(
            "GetStableWeight",
            "Get the stable weight reading from the Kern PCB balance (waits for stability)",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("s \r\n"), ct));
                var (success, weight, unit) = InterpretString(resp);
                if (!success)
                    return CapabilityInvocationResult.Failed("Could not get value");
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { weight, unit }));
            });

        // Tare — "t \r\n"
        builder.RegisterSimple(
            "Tare",
            "Tare the Kern PCB balance (reset to zero)",
            async ct =>
            {
                await Transport.SendReceiveAsync(Encode("t \r\n"), ct);
                return CapabilityInvocationResult.Successful();
            });
    }

    /// <summary>
    /// Parse the Kern PCB response string.
    /// Format: M | S | N1..N10 | B | U1..U3 | CR | LF (stable)
    ///         M | S | N1..N10 | B | B | B | B | CR | LF (unstable)
    ///         B*11 | E | r | r | o | r | CR | LF (fault)
    /// </summary>
    private static (bool success, double value, string unit) InterpretString(string text)
    {
        if (text.Contains("Error") || string.IsNullOrEmpty(text))
            return (false, 0, string.Empty);

        var minusIdx = text.IndexOf('-');
        var idx = text.AsSpan().IndexOfAny(SMyChars);
        if (idx == -1)
            return (false, 0, string.Empty);

        if (minusIdx != -1)
            text = text[minusIdx..idx].Replace(" ", string.Empty) + text[idx..];
        else
            text = text[idx..];

        var splitText = text.Split(' ');
        if (splitText.Length == 2)
            return (true, Convert.ToDouble(splitText[0]), splitText[1]);
        return (true, Convert.ToDouble(splitText[0]), "g");
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            string? resp = null;
            try { resp = Decode(await Transport.SendReceiveAsync(Encode("w \r\n"), ct)); }
            catch (Exception ex) { Logger.LogWarning(ex, "KernPcb: failed to poll w"); }

            if (!string.IsNullOrEmpty(resp))
            {
                var (success, weight, unit) = InterpretString(resp);
                if (success)
                    yield return new TrendSample { TrendName = "kernpcb.weight", Value = weight, Timestamp = now, Unit = unit };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
