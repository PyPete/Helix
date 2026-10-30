using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class ArduinoSignalTowerTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task GetNumberOfValves_SendsNvCommand_ReturnsCount()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("2\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoSignalTower(transport, NullLogger<ArduinoSignalTower>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetNumberOfValves", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("NV"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("2", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetNumberOfValves_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoSignalTower(transport, NullLogger<ArduinoSignalTower>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetNumberOfValves", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("returned incorrectly", result.ErrorMessage);
    }

    [Fact]
    public async Task GoToOn_SendsOpValveCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoSignalTower(transport, NullLogger<ArduinoSignalTower>.Instance);

        var result = await driver.InvokeCapabilityAsync("GoToOn", Json("{\"valve\":\"A\"}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("OPA"), port.Writes[0]);
    }

    [Fact]
    public async Task GoToOff_SendsClValveCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoSignalTower(transport, NullLogger<ArduinoSignalTower>.Instance);

        var result = await driver.InvokeCapabilityAsync("GoToOff", Json("{\"valve\":\"A\"}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("CLA"), port.Writes[0]);
    }

    [Fact]
    public async Task GetCurrentPosition_OnRelay_ReturnsOn()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("1\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoSignalTower(transport, NullLogger<ArduinoSignalTower>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetCurrentPosition", Json("{\"valve\":\"A\"}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("CPA"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("On", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetCurrentPosition_OffRelay_ReturnsOff()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("0\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoSignalTower(transport, NullLogger<ArduinoSignalTower>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetCurrentPosition", Json("{\"valve\":\"B\"}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("CPB"), port.Writes[0]);
        Assert.Contains("Off", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task SetDelay_ValidDelay_SendsDtCommand()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoSignalTower(transport, NullLogger<ArduinoSignalTower>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetDelay", Json("{\"valve\":\"A\",\"milliseconds\":200}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("DTA200"), port.Writes[0]);
    }

    [Fact]
    public async Task SetDelay_NegativeDelay_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoSignalTower(transport, NullLogger<ArduinoSignalTower>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetDelay", Json("{\"valve\":\"A\",\"milliseconds\":-1}"));

        Assert.False(result.Success);
        Assert.Contains("range", result.ErrorMessage);
    }

    [Fact]
    public async Task SwitchThenReturn_SendsTtCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoSignalTower(transport, NullLogger<ArduinoSignalTower>.Instance);

        var result = await driver.InvokeCapabilityAsync("SwitchThenReturn", Json("{\"valve\":\"A\"}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("TTA"), port.Writes[0]);
    }

    [Fact]
    public void ArduinoSignalTower_DoesNotOverrideStreamTrendsAsync()
    {
        // Verify at compile time that the class does not override StreamTrendsAsync
        // (checked by inspecting the method resolution)
        var method = typeof(ArduinoSignalTower).GetMethod("StreamTrendsAsync",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
        Assert.Null(method);
    }
}
