using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

/// <summary>
/// Note: The POP test file for ColeParmerTouch has all tests commented out.
/// These tests verify compilation and basic driver structure only.
/// </summary>
public sealed class ColeParmerTouchTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task StartInfusion_SendsIrunCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\n00>\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 115200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ColeParmerTouch(transport, NullLogger<ColeParmerTouch>.Instance);

        var result = await driver.InvokeCapabilityAsync("StartInfusion", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("0irun\r"), port.Writes[0]);
    }

    [Fact]
    public async Task StartInfusion_AlreadyCompleted_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\n00>\r\n00:T*\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 115200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ColeParmerTouch(transport, NullLogger<ColeParmerTouch>.Instance);

        var result = await driver.InvokeCapabilityAsync("StartInfusion", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Stop_SendsStopCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 115200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new ColeParmerTouch(transport, NullLogger<ColeParmerTouch>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("0stop\r"), port.Writes[0]);
    }
}
