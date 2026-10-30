using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class MettlerBalanceTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task Tare_SendsZiCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("Z A\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new MettlerBalance(transport, NullLogger<MettlerBalance>.Instance);

        var result = await driver.InvokeCapabilityAsync("Tare", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("ZI\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task GetWeightNow_SendsSiCommand_ReturnsWeight()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("S S     100.00 g\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new MettlerBalance(transport, NullLogger<MettlerBalance>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetWeightNow", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("SI\r\n"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("100", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetWeightNow_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new MettlerBalance(transport, NullLogger<MettlerBalance>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetWeightNow", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsWeightSample()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("S S     100.00 g\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new MettlerBalance(transport, NullLogger<MettlerBalance>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 1) break;
        }

        Assert.Single(samples);
        Assert.Equal("mettlerbalance.weight", samples[0].TrendName);
    }
}
