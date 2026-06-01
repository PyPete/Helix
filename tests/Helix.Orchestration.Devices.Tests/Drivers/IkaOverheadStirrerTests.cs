using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class IkaOverheadStirrerTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task SetPoint_ValidRpm_SendsOutSp4Command_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");          // OUT_SP_4 50 \r\n response
        port.EnqueueRead("50.0 4\r\n");    // IN_SP_4\r\n response
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaOverheadStirrer(transport, NullLogger<IkaOverheadStirrer>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"rpm\":50}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("OUT_SP_4 50 \r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task SetPoint_NegativeRpm_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaOverheadStirrer(transport, NullLogger<IkaOverheadStirrer>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"rpm\":-5}"));

        Assert.False(result.Success);
        Assert.Contains("outside of valid range", result.ErrorMessage);
    }

    [Fact]
    public async Task Stop_SendsStop4Command_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");        // STOP_4 \r\n response
        port.EnqueueRead("0.0 4\r\n");  // IN_PV_4\r\n response
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaOverheadStirrer(transport, NullLogger<IkaOverheadStirrer>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("STOP_4 \r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Stop_EmptyActualSpeed_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n"); // STOP_4 \r\n response
        port.EnqueueRead("\r\n"); // IN_PV_4\r\n - empty
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaOverheadStirrer(transport, NullLogger<IkaOverheadStirrer>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetSetPoint_SendsInSp4Command_ReturnsSetpoint()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("50.0 4\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaOverheadStirrer(transport, NullLogger<IkaOverheadStirrer>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetSetPoint", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("IN_SP_4\r\n"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
    }

    [Fact]
    public async Task GetTorque_SendsInPv5Command_ReturnsTorque()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("0.2 5\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaOverheadStirrer(transport, NullLogger<IkaOverheadStirrer>.Instance);

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
        var driver = new IkaOverheadStirrer(transport, NullLogger<IkaOverheadStirrer>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetTorque", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsTrendSamples()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("0.2 5\r\n");   // IN_PV_5 (torque)
        port.EnqueueRead("190.0 4\r\n"); // IN_PV_4 (speed)
        port.EnqueueRead("192.0 4\r\n"); // IN_SP_4 (setpoint)
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new IkaOverheadStirrer(transport, NullLogger<IkaOverheadStirrer>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 3) break;
        }

        Assert.Equal(3, samples.Count);
        Assert.Contains(samples, s => s.TrendName == "ikaoverheadstirrer.torque");
        Assert.Contains(samples, s => s.TrendName == "ikaoverheadstirrer.speed");
        Assert.Contains(samples, s => s.TrendName == "ikaoverheadstirrer.setpointspeed");
    }
}
