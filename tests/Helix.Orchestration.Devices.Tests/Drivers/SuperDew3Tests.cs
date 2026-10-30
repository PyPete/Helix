using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class SuperDew3Tests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] MakeMoistureResponse(float moisture)
    {
        // 15-byte response: bytes 11-14 are the float in reversed order
        var resp = new byte[15];
        var floatBytes = BitConverter.GetBytes(moisture);
        resp[11] = floatBytes[3];
        resp[12] = floatBytes[2];
        resp[13] = floatBytes[1];
        resp[14] = floatBytes[0];
        return resp;
    }

    [Fact]
    public async Task GetMoisture_ValidResponse_ReturnsMoistureValue()
    {
        var port = new FakeSerialPortConnection();
        // Enqueue a 15-byte response representing moisture = 42.5%
        var resp = MakeMoistureResponse(42.5f);
        port.EnqueueRead(Encoding.Latin1.GetString(resp));
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new SuperDew3(transport, NullLogger<SuperDew3>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetMoisture", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
    }

    [Fact]
    public async Task GetMoisture_ShortResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("short\r\n"); // less than 15 bytes
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new SuperDew3(transport, NullLogger<SuperDew3>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetMoisture", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("too short", result.ErrorMessage);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsMoistureSample()
    {
        var port = new FakeSerialPortConnection();
        var resp = MakeMoistureResponse(42.5f);
        port.EnqueueRead(Encoding.Latin1.GetString(resp));
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new SuperDew3(transport, NullLogger<SuperDew3>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 1) break;
        }

        Assert.Single(samples);
        Assert.Equal("superdew3.moisture", samples[0].TrendName);
        Assert.Equal("%", samples[0].Unit);
    }
}
