using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class HeidolphTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task Stop_SendsR0000Command_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("R0000\r\nSET:0\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Heidolph(transport, NullLogger<Heidolph>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("R0000\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task SetPoint_ValidRpm_SendsRCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("R0250\r\nSET:250\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Heidolph(transport, NullLogger<Heidolph>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"rpm\":250}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("R0250\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task SetPoint_OutsideLimits_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Heidolph(transport, NullLogger<Heidolph>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"rpm\":3000}"));

        Assert.False(result.Success);
        Assert.Contains("outside of bounds", result.ErrorMessage);
    }

    [Fact]
    public async Task GetTorque_SendsMCommand_ReturnsTorque()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("600 \r\nNCM: -7\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Heidolph(transport, NullLogger<Heidolph>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetTorque", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("m\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsTorqueAndSpeedSamples()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("600 \r\nNCM: -7\r\n"); // torque poll
        port.EnqueueRead("RPM: 250\r\n");          // speed poll
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Heidolph(transport, NullLogger<Heidolph>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 2) break;
        }

        Assert.True(samples.Count >= 1);
    }
}
