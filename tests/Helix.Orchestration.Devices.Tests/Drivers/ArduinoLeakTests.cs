using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class ArduinoLeakTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task GetLeak_SendsLkCommand_ReturnsOkStatus()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoLeak(transport, NullLogger<ArduinoLeak>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetLeak", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("LK"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("0", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetLeak_BadResponse_ReturnsLeakDetected()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("BAD\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoLeak(transport, NullLogger<ArduinoLeak>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetLeak", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("1", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetLeak_QuestionMarkResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("?\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoLeak(transport, NullLogger<ArduinoLeak>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetLeak", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("did not return correctly", result.ErrorMessage);
    }

    [Fact]
    public async Task GetLeak_GibberishResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("Nonsense\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoLeak(transport, NullLogger<ArduinoLeak>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetLeak", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("interpret response", result.ErrorMessage);
    }

    [Fact]
    public async Task GetUniqueId_SendsSrCommand_ReturnsSerialNumber()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("SN1234\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoLeak(transport, NullLogger<ArduinoLeak>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetUniqueId", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("SR"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("SN1234", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsLeakStatus()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoLeak(transport, NullLogger<ArduinoLeak>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 1) break;
        }

        Assert.Single(samples);
        Assert.Equal("arduinoleak.status", samples[0].TrendName);
        Assert.Equal(0.0, samples[0].Value);
    }
}
