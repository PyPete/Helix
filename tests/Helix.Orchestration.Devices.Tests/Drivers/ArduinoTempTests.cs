using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class ArduinoTempTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task GetTemp_SendsTpCommand_ReturnsTemperature()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("27.5\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoTemp(transport, NullLogger<ArduinoTemp>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetTemp", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("TP"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("27.5", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetTemp_QuestionMarkResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("?\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoTemp(transport, NullLogger<ArduinoTemp>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetTemp", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("returned incorrectly", result.ErrorMessage);
    }

    [Fact]
    public async Task GetUniqueId_SendsSrCommand_ReturnsSerialNumber()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("SN1234\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoTemp(transport, NullLogger<ArduinoTemp>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetUniqueId", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("SR"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("SN1234", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsTemperatureSample()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("27.5\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoTemp(transport, NullLogger<ArduinoTemp>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 1) break;
        }

        Assert.Single(samples);
        Assert.Equal("arduinotemp.temperature", samples[0].TrendName);
        Assert.Equal(27.5, samples[0].Value);
    }
}
