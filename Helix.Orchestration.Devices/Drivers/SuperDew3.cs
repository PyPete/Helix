using System.Runtime.CompilerServices;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the SuperDew 3 moisture sensor.
/// Serial protocol, BaudRate 9600, raw byte command protocol.
/// Poll command: [255, 255, 255, 255, 255, 2, 0, 24, 1, 0, 27]
/// Response: 15 bytes, float at bytes 11-14 (reversed little-endian).
/// </summary>
public sealed class SuperDew3 : DeviceAdapterBase
{
    private static readonly byte[] PollCommand = [255, 255, 255, 255, 255, 2, 0, 24, 1, 0, 27];

    public SuperDew3(SerialDeviceTransport transport, ILogger<SuperDew3> logger)
        : base(transport, logger) { }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // GetMoisture — send raw poll command, parse float from bytes 11-14 (reversed)
        builder.RegisterSimple(
            "GetMoisture",
            "Get the current moisture reading from the SuperDew 3 sensor",
            async ct =>
            {
                var resp = await Transport.SendReceiveAsync(PollCommand, ct);
                if (resp.Length == 0)
                    return CapabilityInvocationResult.Failed("Command returned empty");
                if (resp.Length != 15)
                    return CapabilityInvocationResult.Failed("Command returned too short");
                var floatArray = new[] { resp[14], resp[13], resp[12], resp[11] };
                var moisture = BitConverter.ToSingle(floatArray, 0);
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement((double)moisture));
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            byte[]? resp = null;
            try { resp = await Transport.SendReceiveAsync(PollCommand, ct); }
            catch (Exception ex) { Logger.LogWarning(ex, "SuperDew3: failed to poll moisture"); }

            if (resp != null && resp.Length == 15)
            {
                var floatArray = new[] { resp[14], resp[13], resp[12], resp[11] };
                var moisture = BitConverter.ToSingle(floatArray, 0);
                yield return new TrendSample { TrendName = "superdew3.moisture", Value = (double)moisture, Timestamp = now, Unit = "%" };
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
