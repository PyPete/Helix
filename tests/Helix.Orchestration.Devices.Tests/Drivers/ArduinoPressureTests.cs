using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class ArduinoPressureTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task GetNumberOfSensors_SendsNsCommand_ReturnsCount()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("2\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoPressure(transport, NullLogger<ArduinoPressure>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetNumberOfSensors", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("NS"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("2", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetPressure_SendsPrNCommand_ReturnsPressure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("1.5\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoPressure(transport, NullLogger<ArduinoPressure>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetPressure", Json("{\"sensor\":1}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("PR1"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("1.5", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetPressure_QuestionMarkResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("?\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoPressure(transport, NullLogger<ArduinoPressure>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetPressure", Json("{\"sensor\":1}"));

        Assert.False(result.Success);
        Assert.Contains("returned incorrectly", result.ErrorMessage);
    }

    [Fact]
    public async Task GetUniqueId_SendsSrCommand_ReturnsSerialNumber()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("SN1212\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoPressure(transport, NullLogger<ArduinoPressure>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetUniqueId", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("SR"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("SN1212", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task StreamTrendsAsync_TwoSensors_EmitsTwoSamples()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("2\r\n");   // NS
        port.EnqueueRead("1\r\n");   // PR1
        port.EnqueueRead("1.1\r\n"); // PR2
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoPressure(transport, NullLogger<ArduinoPressure>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 2) break;
        }

        Assert.Equal(2, samples.Count);
        Assert.Contains(samples, s => s.TrendName == "arduinopressure.pressure.1");
        Assert.Contains(samples, s => s.TrendName == "arduinopressure.pressure.2");
    }
}
