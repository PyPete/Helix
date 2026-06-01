using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class IkaDigitalHotPlateTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task StirringSetPoint_ValidRpm_SendsOutSp4Command_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("RCT Digital\r\n"); // OUT_SP_4 50\r\n response
        port.EnqueueRead("50.0 4\r\n");       // IN_SP_4\r\n response
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaDigitalHotPlate(transport, NullLogger<IkaDigitalHotPlate>.Instance);

        var result = await driver.InvokeCapabilityAsync("StirringSetPoint", Json("{\"rpm\":50}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("OUT_SP_4 50\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task StirringSetPoint_OutsideRange_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaDigitalHotPlate(transport, NullLogger<IkaDigitalHotPlate>.Instance);

        var result = await driver.InvokeCapabilityAsync("StirringSetPoint", Json("{\"rpm\":2000}"));

        Assert.False(result.Success);
        Assert.Contains("outside the feasible range", result.ErrorMessage);
    }

    [Fact]
    public async Task StirringSetPoint_GetSetPointDifferent_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("RCT Digital\r\n"); // OUT_SP_4 50\r\n
        port.EnqueueRead("60.0 4\r\n");       // IN_SP_4\r\n - different value
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaDigitalHotPlate(transport, NullLogger<IkaDigitalHotPlate>.Instance);

        var result = await driver.InvokeCapabilityAsync("StirringSetPoint", Json("{\"rpm\":50}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task StartStirrer_SendsStart4Command_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("RCT Digital\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaDigitalHotPlate(transport, NullLogger<IkaDigitalHotPlate>.Instance);

        var result = await driver.InvokeCapabilityAsync("StartStirrer", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("START_4\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task StopStirring_SendsStop4Command_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("RCT Digital\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaDigitalHotPlate(transport, NullLogger<IkaDigitalHotPlate>.Instance);

        var result = await driver.InvokeCapabilityAsync("StopStirring", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("STOP_4\r\n"), port.Writes[0]);
    }
}
