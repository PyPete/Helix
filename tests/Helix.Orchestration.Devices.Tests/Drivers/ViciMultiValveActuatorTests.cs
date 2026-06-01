using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class ViciMultiValveActuatorTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task GoTo_SendsValveGoPositionCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ViciMultiValveActuator(transport, NullLogger<ViciMultiValveActuator>.Instance);

        var result = await driver.InvokeCapabilityAsync("GoTo", Json("{\"valve\":\"A\",\"position\":\"A\"}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("AGOA\r"), port.Writes[0]);
    }

    [Fact]
    public async Task GoTo_InvalidPosition_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ViciMultiValveActuator(transport, NullLogger<ViciMultiValveActuator>.Instance);

        var result = await driver.InvokeCapabilityAsync("GoTo", Json("{\"valve\":\"A\",\"position\":\"C\"}"));

        Assert.False(result.Success);
        Assert.Contains("not correct", result.ErrorMessage);
    }

    [Fact]
    public async Task GetCurrentPosition_SendsValveCpCommand_ReturnsPosition()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("CPA\x0D\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ViciMultiValveActuator(transport, NullLogger<ViciMultiValveActuator>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetCurrentPosition", Json("{\"valve\":\"A\"}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("ACP\r"), port.Writes[0]);
        Assert.Contains("A", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task SetDelay_ValidMs_SendsValveDtCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ViciMultiValveActuator(transport, NullLogger<ViciMultiValveActuator>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetDelay", Json("{\"valve\":\"A\",\"milliseconds\":300}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("ADT300\r"), port.Writes[0]);
    }

    [Fact]
    public void ViciMultiValveActuator_DoesNotOverrideStreamTrendsAsync()
    {
        var method = typeof(ViciMultiValveActuator).GetMethod("StreamTrendsAsync",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
        Assert.Null(method);
    }
}
