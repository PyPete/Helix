using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class ArduinoConductivityTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    // Note: ArduinoConductivity has no command terminator. The transport's CommandTerminator
    // is set to "" but the FakeSerialStream reads until it gets 0 bytes back.
    // We enqueue responses directly (no terminator appended).
    private static (ArduinoConductivity driver, FakeSerialPortConnection port) CreateDriver(params string[] responses)
    {
        var port = new FakeSerialPortConnection();
        foreach (var r in responses)
            port.EnqueueRead(r + "\r\n"); // FakeSerial reads until empty; use \r\n as flush signal
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        transport.ConnectAsync().GetAwaiter().GetResult();
        var driver = new ArduinoConductivity(transport, NullLogger<ArduinoConductivity>.Instance);
        return (driver, port);
    }

    [Fact]
    public async Task EnterCalibration_SendsEcCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("Calibration Mode\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoConductivity(transport, NullLogger<ArduinoConductivity>.Instance);

        var result = await driver.InvokeCapabilityAsync("EnterCalibration", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("EC"), port.Writes[0]);
    }

    [Fact]
    public async Task EnterCalibration_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoConductivity(transport, NullLogger<ArduinoConductivity>.Instance);

        var result = await driver.InvokeCapabilityAsync("EnterCalibration", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("Calibration did not enter", result.ErrorMessage);
    }

    [Fact]
    public async Task ExitCalibration_SendsXcCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("Successful\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoConductivity(transport, NullLogger<ArduinoConductivity>.Instance);

        var result = await driver.InvokeCapabilityAsync("ExitCalibration", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("XC"), port.Writes[0]);
    }

    [Fact]
    public async Task ExitCalibration_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoConductivity(transport, NullLogger<ArduinoConductivity>.Instance);

        var result = await driver.InvokeCapabilityAsync("ExitCalibration", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("did not exit", result.ErrorMessage);
    }

    [Fact]
    public async Task Calibrate_SendsCcCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("Successful\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoConductivity(transport, NullLogger<ArduinoConductivity>.Instance);

        var result = await driver.InvokeCapabilityAsync("Calibrate", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("CC"), port.Writes[0]);
    }

    [Fact]
    public async Task Calibrate_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoConductivity(transport, NullLogger<ArduinoConductivity>.Instance);

        var result = await driver.InvokeCapabilityAsync("Calibrate", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetReading_SendsMcCommand_ReturnsValue()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("7.5\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoConductivity(transport, NullLogger<ArduinoConductivity>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetReading", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("MC"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("7.5", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetUniqueId_SendsSrCommand_ReturnsSerialNumber()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("SN1234\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoConductivity(transport, NullLogger<ArduinoConductivity>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetUniqueId", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("SR"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("SN1234", result.ReturnValue!.Value.ToString());
    }
}
