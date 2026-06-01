using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class PilotOneTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task Start_SendsM140001Command_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("{S1310D0A\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new PilotOne(transport, NullLogger<PilotOne>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Encoding.Default.GetBytes("{M140001\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Stop_SendsM140000Command_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("{S1310D0A\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new PilotOne(transport, NullLogger<PilotOne>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Encoding.Default.GetBytes("{M140000\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task GetSetPointLimits_ReturnsCorrectLimits()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new PilotOne(transport, NullLogger<PilotOne>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetSetPointLimits", Json("{}"));

        Assert.True(result.Success);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("-151", json);
    }
}
