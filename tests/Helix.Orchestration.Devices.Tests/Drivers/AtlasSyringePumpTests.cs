using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class AtlasSyringePumpTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task Remote_SendsA1Command_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("#A\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 57600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new AtlasSyringePump(transport, NullLogger<AtlasSyringePump>.Instance);

        var result = await driver.InvokeCapabilityAsync("Remote", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("A1\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Remote_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 57600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new AtlasSyringePump(transport, NullLogger<AtlasSyringePump>.Instance);

        var result = await driver.InvokeCapabilityAsync("Remote", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Stop_SendsXAxisCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("#X s\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 57600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new AtlasSyringePump(transport, NullLogger<AtlasSyringePump>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{\"axis\":0}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("X 0\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Stop_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 57600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new AtlasSyringePump(transport, NullLogger<AtlasSyringePump>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{\"axis\":0}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Pause_SendsWAxisCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("#W s\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 57600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new AtlasSyringePump(transport, NullLogger<AtlasSyringePump>.Instance);

        var result = await driver.InvokeCapabilityAsync("Pause", Json("{\"axis\":0}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("W 0\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Resume_SendsUAxisCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("#U s\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 57600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new AtlasSyringePump(transport, NullLogger<AtlasSyringePump>.Instance);

        var result = await driver.InvokeCapabilityAsync("Resume", Json("{\"axis\":0}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("U 0\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Pump_SendsPAxisCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("#P s\r\n");
        // S0 status check (called inside Pump in POP but we don't replicate that)
        var transport = FakeSerial.MakeTransport(port, baudRate: 57600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new AtlasSyringePump(transport, NullLogger<AtlasSyringePump>.Instance);

        var result = await driver.InvokeCapabilityAsync("Pump", Json("{\"axis\":0,\"flowRateMlMin\":1,\"volumeMl\":1,\"fillPort\":1,\"emptyPort\":2}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("P0 1000 1000 1 2\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Fill_SendsFAxisCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("#F s\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 57600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new AtlasSyringePump(transport, NullLogger<AtlasSyringePump>.Instance);

        var result = await driver.InvokeCapabilityAsync("Fill", Json("{\"axis\":0,\"flowRateMlMin\":1,\"port\":2}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("F0 1000 2\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Empty_SendsEAxisCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("#E s\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 57600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new AtlasSyringePump(transport, NullLogger<AtlasSyringePump>.Instance);

        var result = await driver.InvokeCapabilityAsync("Empty", Json("{\"axis\":0,\"flowRateMlMin\":1,\"port\":2}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("E0 1000 2\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task CheckIfPumpIdle_IdleState_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("#S0 0 0 623.75 0.00 376.25 4000.00 21.70 4.95 961.88\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 57600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new AtlasSyringePump(transport, NullLogger<AtlasSyringePump>.Instance);

        var result = await driver.InvokeCapabilityAsync("CheckIfPumpIdle", Json("{\"axis\":0}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("S0\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task CheckIfPumpIdle_BusyState_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("#S0 0 1 623.75 0.00 376.25 4000.00 21.70 4.95 961.88\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 57600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new AtlasSyringePump(transport, NullLogger<AtlasSyringePump>.Instance);

        var result = await driver.InvokeCapabilityAsync("CheckIfPumpIdle", Json("{\"axis\":0}"));

        Assert.False(result.Success);
        Assert.Contains("busy", result.ErrorMessage);
    }

    [Fact]
    public async Task CheckIfPumpIdle_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 57600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new AtlasSyringePump(transport, NullLogger<AtlasSyringePump>.Instance);

        var result = await driver.InvokeCapabilityAsync("CheckIfPumpIdle", Json("{\"axis\":0}"));

        Assert.False(result.Success);
        Assert.Contains("returned incorrectly", result.ErrorMessage);
    }
}
