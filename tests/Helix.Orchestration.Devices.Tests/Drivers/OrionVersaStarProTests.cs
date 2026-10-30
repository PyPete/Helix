using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class OrionVersaStarProTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task Stop_SendsStopCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("ST\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 115200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new OrionVersaStarPro(transport, NullLogger<OrionVersaStarPro>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("STOP\r"), port.Writes[0]);
    }

    [Fact]
    public async Task Stop_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 115200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new OrionVersaStarPro(transport, NullLogger<OrionVersaStarPro>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetMeasurement_ParsesPhFromResponse_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("SE\r");   // SETCHANNEL response - ends with \r (terminator)
        port.EnqueueRead(">SE\r");  // SETREADTYPE response - ends with \r (terminator)
        port.EnqueueRead("Versa Star Pro,V19673,11.50,08-11-22, 6:30:17 PM,Ch-1,-----,-----,-----,VA30874,-----,-----,PH,7.000,pH,0.0,mV,25.0,C (MAN),0.0,%,100\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 115200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new OrionVersaStarPro(transport, NullLogger<OrionVersaStarPro>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetMeasurement", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("SETCHANNEL 1\r"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("7", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetMeasurement_SetChannelFails_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r"); // SETCHANNEL - no SE in response
        var transport = FakeSerial.MakeTransport(port, baudRate: 115200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new OrionVersaStarPro(transport, NullLogger<OrionVersaStarPro>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetMeasurement", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("Set channel", result.ErrorMessage);
    }
}
