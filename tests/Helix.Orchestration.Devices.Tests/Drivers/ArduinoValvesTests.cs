using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class ArduinoValvesTests
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
        var driver = new ArduinoValves(transport, NullLogger<ArduinoValves>.Instance);

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
        var driver = new ArduinoValves(transport, NullLogger<ArduinoValves>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetNumberOfValves", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("returned incorrectly", result.ErrorMessage);
    }

    [Fact]
    public async Task GoToOpen_SendsOpIndexCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoValves(transport, NullLogger<ArduinoValves>.Instance);

        var result = await driver.InvokeCapabilityAsync("GoToOpen", Json("{\"valve\":0}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("OP0"), port.Writes[0]);
    }

    [Fact]
    public async Task GoToClosed_SendsClIndexCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoValves(transport, NullLogger<ArduinoValves>.Instance);

        var result = await driver.InvokeCapabilityAsync("GoToClosed", Json("{\"valve\":0}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("CL0"), port.Writes[0]);
    }

    [Fact]
    public async Task GetCurrentPosition_ClosedRelay_ReturnsClosed()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("1\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoValves(transport, NullLogger<ArduinoValves>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetCurrentPosition", Json("{\"valve\":0}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("CP0"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("Closed", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetCurrentPosition_OpenRelay_ReturnsOpen()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("0\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoValves(transport, NullLogger<ArduinoValves>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetCurrentPosition", Json("{\"valve\":1}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("CP1"), port.Writes[0]);
        Assert.Contains("Open", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetCurrentPosition_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoValves(transport, NullLogger<ArduinoValves>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetCurrentPosition", Json("{\"valve\":2}"));

        Assert.False(result.Success);
        Assert.Contains("returned incorrectly", result.ErrorMessage);
    }

    [Fact]
    public async Task GetUniqueId_SendsSrCommand_ReturnsSerialNumber()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("SN1234\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new ArduinoValves(transport, NullLogger<ArduinoValves>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetUniqueId", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("SR"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("SN1234", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public void ArduinoValves_DoesNotOverrideStreamTrendsAsync()
    {
        var method = typeof(ArduinoValves).GetMethod("StreamTrendsAsync",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
        Assert.Null(method);
    }
}
