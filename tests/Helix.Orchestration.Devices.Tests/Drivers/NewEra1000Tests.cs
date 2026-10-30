using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class NewEra1000Tests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task Start_SendsRunCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\x0200I\x03\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new NewEra1000(transport, NullLogger<NewEra1000>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("RUN \r"), port.Writes[0]);
    }

    [Fact]
    public async Task Start_NonIResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\x0200W\x03\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new NewEra1000(transport, NullLogger<NewEra1000>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Stop_SendsStpCommandTwice_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\x0200P\x03\r"); // first STP — pause
        port.EnqueueRead("\x0200S\x03\r"); // second STP — stopped
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new NewEra1000(transport, NullLogger<NewEra1000>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("STP \r"), port.Writes[0]);
        Assert.Equal(Ascii("STP \r"), port.Writes[1]);
    }

    [Fact]
    public async Task SetVolume_SendsVolCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\x0200S\x03\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new NewEra1000(transport, NullLogger<NewEra1000>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetVolume", Json("{\"volume\":12.45}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("VOL 12.45 \r"), port.Writes[0]);
    }

    [Fact]
    public async Task NewEra1000_DoesNotCreateNewEraBaseMethodsHelper()
    {
        // Verify the driver does not reference NewEraBaseMethods (compile-time check)
        var driverType = typeof(NewEra1000);
        var fields = driverType.GetFields(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.DoesNotContain(fields, f => f.FieldType.Name == "NewEraBaseMethods");
    }
}
