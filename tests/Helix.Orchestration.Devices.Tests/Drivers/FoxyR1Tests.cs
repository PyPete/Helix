using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class FoxyR1Tests
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
        port.EnqueueRead("STOP\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("STOP\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Stop_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Collect_SendsCollectCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("COLLECT\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var result = await driver.InvokeCapabilityAsync("Collect", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("COLLECT\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Collect_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var result = await driver.InvokeCapabilityAsync("Collect", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Drain_SendsDrainCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("DRAIN\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var result = await driver.InvokeCapabilityAsync("Drain", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("DRAIN\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Home_SendsHomeCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("HOME\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var result = await driver.InvokeCapabilityAsync("Home", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("HOME\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task NextSample_SendsNextTubeCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("NEXTTUBE\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var result = await driver.InvokeCapabilityAsync("NextSample", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("NEXTTUBE\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task NextSample_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var result = await driver.InvokeCapabilityAsync("NextSample", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SetToPosition_SendsTubePositionCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("TUBE=2\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetToPosition", Json("{\"position\":2}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("TUBE=2\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task SetToPosition_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetToPosition", Json("{\"position\":2}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Run_SendsRunAndTubeCommands_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("RUN\r\n");
        port.EnqueueRead("TUBE=40\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var result = await driver.InvokeCapabilityAsync("Run", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("RUN\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Run_TubeCommandFails_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("RUN\r\n");
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var result = await driver.InvokeCapabilityAsync("Run", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("could not execute", result.ErrorMessage);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsTubePositionAsDouble()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("TUBE=40\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new FoxyR1(transport, NullLogger<FoxyR1>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 1) break;
        }

        Assert.Single(samples);
        Assert.Equal("foxyr1.position", samples[0].TrendName);
        Assert.Equal(40.0, samples[0].Value);
    }
}
