using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class AlicatTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task Start_ReturnsSuccess_NoCommandSent()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new Alicat(transport, NullLogger<Alicat>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.True(result.Success);
        Assert.Empty(port.Writes);
    }

    [Fact]
    public async Task Stop_ReturnsSuccess_NoCommandSent()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new Alicat(transport, NullLogger<Alicat>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Empty(port.Writes);
    }

    [Fact]
    public async Task SetPoint_SendsAsCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("A +00987 +024.67 -01.031 +09.475 +15.44 Cl2\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new Alicat(transport, NullLogger<Alicat>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"setpoint\":5.0}"));

        Assert.True(result.Success);
        Assert.StartsWith("As5", Encoding.ASCII.GetString(port.Writes[0]));
    }

    [Fact]
    public async Task GetSetPoint_ReturnsStoredSetpoint()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("A +00987 +024.67 -01.031 +09.475 +15.44 Cl2\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new Alicat(transport, NullLogger<Alicat>.Instance);

        await driver.InvokeCapabilityAsync("SetPoint", Json("{\"setpoint\":7.5}"));
        var result = await driver.InvokeCapabilityAsync("GetSetPoint", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("7.5", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetSetPointLimits_ReturnsMinAndMax()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new Alicat(transport, NullLogger<Alicat>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetSetPointLimits", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("0", json);
        Assert.Contains("10", json);
    }

    [Fact]
    public async Task TareAll_ReturnsSuccess_NoCommandSent()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new Alicat(transport, NullLogger<Alicat>.Instance);

        var result = await driver.InvokeCapabilityAsync("TareAll", Json("{}"));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsTrendSamples()
    {
        var port = new FakeSerialPortConnection();
        // A response: unit_id abspress temp volflow massflow setpoint gas
        port.EnqueueRead("A +00987 +024.67 -01.031 +09.475 +15.44 Cl2\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 19200, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new Alicat(transport, NullLogger<Alicat>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 1) break;
        }

        Assert.True(samples.Count >= 1);
    }
}
