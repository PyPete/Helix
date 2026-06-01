using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class Carino9Tests
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
        port.EnqueueRead("START:OK\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Carino9(transport, NullLogger<Carino9>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("ON\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Start_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Carino9(transport, NullLogger<Carino9>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Stop_SendsOffCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("STOP:OK\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Carino9(transport, NullLogger<Carino9>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("OFF\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Stop_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Carino9(transport, NullLogger<Carino9>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SetPoint_SendsFlowCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("FLOW:OK\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Carino9(transport, NullLogger<Carino9>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"setpoint\":1}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("FLOW:1000\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task SetPoint_OutsideLimits_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Carino9(transport, NullLogger<Carino9>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"setpoint\":11}"));

        Assert.False(result.Success);
        Assert.Contains("outside of bounds", result.ErrorMessage);
    }

    [Fact]
    public async Task GetSetPoint_SendsFlowQueryCommand_ReturnsFlowInMlMin()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("FLOW:1000\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Carino9(transport, NullLogger<Carino9>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetSetPoint", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("FLOW?\r\n"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("1", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetSetPoint_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Carino9(transport, NullLogger<Carino9>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetSetPoint", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetPressure_SendsPQueryCommand_ReturnsPressure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("P:1\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Carino9(transport, NullLogger<Carino9>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetPressure", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("P?\r\n"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("1", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetPressure_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Carino9(transport, NullLogger<Carino9>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetPressure", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetSetPointLimits_ReturnsMinAndMax()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Carino9(transport, NullLogger<Carino9>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetSetPointLimits", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("0", json);
        Assert.Contains("10", json);
    }
}
