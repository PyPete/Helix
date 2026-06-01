using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class KernPcbTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task GetWeightNow_SendsWCommand_ReturnsWeight()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("        1.0 g  \r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new KernPcb(transport, NullLogger<KernPcb>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetWeightNow", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("w \r\n"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
    }

    [Fact]
    public async Task GetWeightNow_WithMinus_ReturnsWeight()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead(" -       1.0 g  \r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new KernPcb(transport, NullLogger<KernPcb>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetWeightNow", Json("{}"));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetWeightNow_ErrorResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("           Error\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new KernPcb(transport, NullLogger<KernPcb>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetWeightNow", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("Could not get value", result.ErrorMessage);
    }

    [Fact]
    public async Task GetStableWeight_SendsSCommand_ReturnsWeight()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("        2.5 g  \r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new KernPcb(transport, NullLogger<KernPcb>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetStableWeight", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("s \r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Tare_SendsTCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new KernPcb(transport, NullLogger<KernPcb>.Instance);

        var result = await driver.InvokeCapabilityAsync("Tare", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("t \r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsWeightSample()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("        1.0 g  \r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new KernPcb(transport, NullLogger<KernPcb>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 1) break;
        }

        Assert.Single(samples);
        Assert.Equal("kernpcb.weight", samples[0].TrendName);
    }
}
