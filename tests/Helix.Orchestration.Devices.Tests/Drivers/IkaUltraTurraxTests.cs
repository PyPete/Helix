using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class IkaUltraTurraxTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task SetPoint_ValidRpm_ParsesIndex1_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");                   // OUT_SP_4 5000 \r\n response
        port.EnqueueRead("IN_SP_4 5000.0\r\n");    // IN_SP_4\r\n response - uses split[1]
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaUltraTurrax(transport, NullLogger<IkaUltraTurrax>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"rpm\":5000}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("OUT_SP_4 5000 \r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task SetPoint_OutsideRange_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaUltraTurrax(transport, NullLogger<IkaUltraTurrax>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"rpm\":50}"));

        Assert.False(result.Success);
        Assert.Contains("outside of valid range", result.ErrorMessage);
    }

    [Fact]
    public async Task GetTorque_ParsesIndex1_ReturnsTorque()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("IN_PV_5 0.2\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaUltraTurrax(transport, NullLogger<IkaUltraTurrax>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetTorque", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("IN_PV_5\r\n"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
    }

    [Fact]
    public async Task GetTorque_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaUltraTurrax(transport, NullLogger<IkaUltraTurrax>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetTorque", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetSetPointLimits_ReturnsCorrectLimits()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaUltraTurrax(transport, NullLogger<IkaUltraTurrax>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetSetPointLimits", Json("{}"));

        Assert.True(result.Success);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("3000", json);
        Assert.Contains("25000", json);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsTrendSamples()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("IN_PV_5 0.2\r\n");    // torque
        port.EnqueueRead("IN_PV_4 190.0\r\n");  // speed
        port.EnqueueRead("IN_SP_4 192.0 4\r\n"); // setpoint
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaUltraTurrax(transport, NullLogger<IkaUltraTurrax>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 3) break;
        }

        Assert.Equal(3, samples.Count);
        Assert.Contains(samples, s => s.TrendName == "ikaultraturrax.torque");
        Assert.Contains(samples, s => s.TrendName == "ikaultraturrax.speed");
        Assert.Contains(samples, s => s.TrendName == "ikaultraturrax.setpointspeed");
    }
}
