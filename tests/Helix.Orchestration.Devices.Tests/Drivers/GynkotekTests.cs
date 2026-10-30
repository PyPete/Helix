using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class GynkotekTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task Start_SendsSfCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("starting flow\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Gynkotek(transport, NullLogger<Gynkotek>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("SF"), port.Writes[0]);
    }

    [Fact]
    public async Task Start_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Gynkotek(transport, NullLogger<Gynkotek>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Stop_SendsStCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("stopping flow\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Gynkotek(transport, NullLogger<Gynkotek>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("ST"), port.Writes[0]);
    }

    [Fact]
    public async Task Stop_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Gynkotek(transport, NullLogger<Gynkotek>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SetPoint_SendsFrBinaryCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("setting flow\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Gynkotek(transport, NullLogger<Gynkotek>.Instance);

        // 4.999 ml/min = 4999 ul → "4999" → "0100 1001 1001 1001"
        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"setpoint\":4.999}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("FR0100100110011001"), port.Writes[0]);
    }

    [Fact]
    public async Task SetPoint_OutsideLimits_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Gynkotek(transport, NullLogger<Gynkotek>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"setpoint\":60}"));

        Assert.False(result.Success);
        Assert.Contains("outside of bounds", result.ErrorMessage);
    }

    [Fact]
    public async Task GetSetPoint_SendsGfCommand_ReturnsFlowInMlMin()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("010010011001\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Gynkotek(transport, NullLogger<Gynkotek>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetSetPoint", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("GF"), port.Writes[0]);
    }

    [Fact]
    public async Task GetSetPoint_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Gynkotek(transport, NullLogger<Gynkotek>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetSetPoint", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetUniqueId_SendsSrCommand_ReturnsSerialNumber()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("SN1234\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Gynkotek(transport, NullLogger<Gynkotek>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetUniqueId", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("SR"), port.Writes[0]);
    }
}
