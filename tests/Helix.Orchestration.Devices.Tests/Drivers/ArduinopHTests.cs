using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class ArduinopHTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task EnterCalibration_SendsEpCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("Calibration Mode\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinopH(transport, NullLogger<ArduinopH>.Instance);

        var result = await driver.InvokeCapabilityAsync("EnterCalibration", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("EP"), port.Writes[0]);
    }

    [Fact]
    public async Task EnterCalibration_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinopH(transport, NullLogger<ArduinopH>.Instance);

        var result = await driver.InvokeCapabilityAsync("EnterCalibration", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("did not enter", result.ErrorMessage);
    }

    [Fact]
    public async Task ExitCalibration_SendsXpCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("Successful\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinopH(transport, NullLogger<ArduinopH>.Instance);

        var result = await driver.InvokeCapabilityAsync("ExitCalibration", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("XP"), port.Writes[0]);
    }

    [Fact]
    public async Task ExitCalibration_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinopH(transport, NullLogger<ArduinopH>.Instance);

        var result = await driver.InvokeCapabilityAsync("ExitCalibration", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("did not exit", result.ErrorMessage);
    }

    [Fact]
    public async Task Calibrate_SendsCbCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("Buffer Solution\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinopH(transport, NullLogger<ArduinopH>.Instance);

        var result = await driver.InvokeCapabilityAsync("Calibrate", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("CB"), port.Writes[0]);
    }

    [Fact]
    public async Task Calibrate_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinopH(transport, NullLogger<ArduinopH>.Instance);

        var result = await driver.InvokeCapabilityAsync("Calibrate", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetReading_SendsMpCommand_ReturnsPh()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("7.5\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinopH(transport, NullLogger<ArduinopH>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetReading", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("MP"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("7.5", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetReading_NonNumericResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("GARBAGE_RESPONSE\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinopH(transport, NullLogger<ArduinopH>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetReading", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("did not return correctly", result.ErrorMessage);
    }

    [Fact]
    public async Task GetUniqueId_SendsSrCommand_ReturnsSerialNumber()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("SN1234\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinopH(transport, NullLogger<ArduinopH>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetUniqueId", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("SR"), port.Writes[0]);
    }
}
