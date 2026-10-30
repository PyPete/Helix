using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class MasterflexLsDigitalTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task Start_SendsG0Command_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\x06\r"); // ACK — ends with \r (terminator)
        var transport = FakeSerial.MakeTransport(port, baudRate: 4800, commandTerminator: "\r",
            parity: System.IO.Ports.Parity.Odd, dataBits: 7);
        await transport.ConnectAsync();
        var driver = new MasterFlexLsDigital(transport, NullLogger<MasterFlexLsDigital>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("\x02P02G0\r"), port.Writes[0]);
    }

    [Fact]
    public async Task Start_NonAckResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("P99\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 4800, commandTerminator: "\r",
            parity: System.IO.Ports.Parity.Odd, dataBits: 7);
        await transport.ConnectAsync();
        var driver = new MasterFlexLsDigital(transport, NullLogger<MasterFlexLsDigital>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Stop_SendsHCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\x06\r"); // ends with \r
        var transport = FakeSerial.MakeTransport(port, baudRate: 4800, commandTerminator: "\r",
            parity: System.IO.Ports.Parity.Odd, dataBits: 7);
        await transport.ConnectAsync();
        var driver = new MasterFlexLsDigital(transport, NullLogger<MasterFlexLsDigital>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("\x02P02H\r"), port.Writes[0]);
    }

    [Fact]
    public async Task SetPoint_ValidRpm_SendsSetSpeedCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\x06\r"); // Halt ACK — ends with \r (terminator only)
        port.EnqueueRead("\x06\r"); // SetSpeed ACK
        var transport = FakeSerial.MakeTransport(port, baudRate: 4800, commandTerminator: "\r",
            parity: System.IO.Ports.Parity.Odd, dataBits: 7);
        await transport.ConnectAsync();
        var driver = new MasterFlexLsDigital(transport, NullLogger<MasterFlexLsDigital>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"rpm\":75}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("\x02P02H\r"), port.Writes[0]); // Halt first
        Assert.Equal(Ascii("\x02P02S+75\r"), port.Writes[1]); // Then set speed
    }

    [Fact]
    public async Task SetPoint_OutsideLimits_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 4800, commandTerminator: "\r",
            parity: System.IO.Ports.Parity.Odd, dataBits: 7);
        await transport.ConnectAsync();
        var driver = new MasterFlexLsDigital(transport, NullLogger<MasterFlexLsDigital>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"rpm\":700}"));

        Assert.False(result.Success);
        Assert.Contains("outside of bounds", result.ErrorMessage);
    }
}
