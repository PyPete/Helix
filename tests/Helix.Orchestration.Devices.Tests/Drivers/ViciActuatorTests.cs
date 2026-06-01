using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class ViciActuatorTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task GoTo_SendsGoPositionCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ViciActuator(transport, NullLogger<ViciActuator>.Instance);

        var result = await driver.InvokeCapabilityAsync("GoTo", Json("{\"position\":\"A\"}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("GOA\r"), port.Writes[0]);
    }

    [Fact]
    public async Task GetCurrentPosition_SendsCpCommand_ReturnsPosition()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("CPA\x0D\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ViciActuator(transport, NullLogger<ViciActuator>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetCurrentPosition", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("CP\r"), port.Writes[0]);
        Assert.Contains("A", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetCurrentPosition_QuestionMarkResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("?????\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ViciActuator(transport, NullLogger<ViciActuator>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetCurrentPosition", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SetDelay_ValidMs_SendsDtCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ViciActuator(transport, NullLogger<ViciActuator>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetDelay", Json("{\"milliseconds\":300}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("DT300\r"), port.Writes[0]);
    }

    [Fact]
    public async Task SetDelay_OutsideLimits_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ViciActuator(transport, NullLogger<ViciActuator>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetDelay", Json("{\"milliseconds\":700000}"));

        Assert.False(result.Success);
        Assert.Contains("outside of the available range", result.ErrorMessage);
    }

    [Fact]
    public async Task SwitchThenReturn_SendsTtCommand_WaitsDelay()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r"); // DT300 response
        port.EnqueueRead("\r"); // TT response
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ViciActuator(transport, NullLogger<ViciActuator>.Instance);

        // Set a short delay for test
        await driver.InvokeCapabilityAsync("SetDelay", Json("{\"milliseconds\":100}"));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await driver.InvokeCapabilityAsync("SwitchThenReturn", Json("{}"));
        sw.Stop();

        Assert.True(result.Success);
        Assert.Equal(Ascii("TT\r"), port.Writes[1]);
        Assert.True(sw.ElapsedMilliseconds >= 80);
    }
}
