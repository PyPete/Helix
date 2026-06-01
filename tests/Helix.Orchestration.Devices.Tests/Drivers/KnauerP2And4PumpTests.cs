using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class KnauerP2And4PumpTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task Start_SendsOnCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new KnauerP2And4Pump(transport, NullLogger<KnauerP2And4Pump>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("ON\r"), port.Writes[0]);
    }

    [Fact]
    public async Task Stop_SendsOffCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new KnauerP2And4Pump(transport, NullLogger<KnauerP2And4Pump>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("OFF\r"), port.Writes[0]);
    }

    [Fact]
    public async Task SetPoint_SendsFlowCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new KnauerP2And4Pump(transport, NullLogger<KnauerP2And4Pump>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"setpoint\":1.1}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("FLOW:1100\r"), port.Writes[0]);
    }

    [Fact]
    public async Task GetSetPoint_SendsFlowQueryCommand_ReturnsFlowInMlMin()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("FLOW:5310\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new KnauerP2And4Pump(transport, NullLogger<KnauerP2And4Pump>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetSetPoint", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("FLOW?\r"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
    }

    [Fact]
    public async Task GetSetPoint_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new KnauerP2And4Pump(transport, NullLogger<KnauerP2And4Pump>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetSetPoint", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetPressure_SendsPressureQueryCommand_ReturnsPressure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("PRESSURE:5310\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new KnauerP2And4Pump(transport, NullLogger<KnauerP2And4Pump>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetPressure", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("PRESSURE?\r"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
    }

    [Fact]
    public async Task GetPressure_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new KnauerP2And4Pump(transport, NullLogger<KnauerP2And4Pump>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetPressure", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsFlowAndPressureSamples()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("FLOW:5310\r");
        port.EnqueueRead("PRESSURE:5310\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new KnauerP2And4Pump(transport, NullLogger<KnauerP2And4Pump>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 2) break;
        }

        Assert.Equal(2, samples.Count);
        Assert.Contains(samples, s => s.TrendName == "knauerp2and4pump.flow");
        Assert.Contains(samples, s => s.TrendName == "knauerp2and4pump.pressure");
    }
}
