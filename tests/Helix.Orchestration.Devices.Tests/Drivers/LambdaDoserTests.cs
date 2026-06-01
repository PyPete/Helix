using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class LambdaDoserTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task SetPoint_WhenNotOn_StoresSetpointWithoutSendingCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 2400, commandTerminator: "\r",
            parity: System.IO.Ports.Parity.Odd);
        await transport.ConnectAsync();
        var driver = new LambdaDoser(transport, NullLogger<LambdaDoser>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"setpoint\":123}"));

        Assert.True(result.Success);
        Assert.Empty(port.Writes); // No commands sent when not on
    }

    [Fact]
    public async Task SetPoint_OutsideLimits_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 2400, commandTerminator: "\r",
            parity: System.IO.Ports.Parity.Odd);
        await transport.ConnectAsync();
        var driver = new LambdaDoser(transport, NullLogger<LambdaDoser>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"setpoint\":1000}"));

        Assert.False(result.Success);
        Assert.Contains("outside of limits", result.ErrorMessage);
    }

    [Fact]
    public async Task Stop_WhenNotOn_ReturnsSuccessWithoutCommand()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 2400, commandTerminator: "\r",
            parity: System.IO.Ports.Parity.Odd);
        await transport.ConnectAsync();
        var driver = new LambdaDoser(transport, NullLogger<LambdaDoser>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Empty(port.Writes);
    }

    [Fact]
    public async Task GetSetPoint_ReturnsStoredSpeed()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 2400, commandTerminator: "\r",
            parity: System.IO.Ports.Parity.Odd);
        await transport.ConnectAsync();
        var driver = new LambdaDoser(transport, NullLogger<LambdaDoser>.Instance);

        await driver.InvokeCapabilityAsync("SetPoint", Json("{\"setpoint\":250}"));
        var result = await driver.InvokeCapabilityAsync("GetSetPoint", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("250", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetSetPointLimits_ReturnsCorrectLimits()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 2400, commandTerminator: "\r",
            parity: System.IO.Ports.Parity.Odd);
        await transport.ConnectAsync();
        var driver = new LambdaDoser(transport, NullLogger<LambdaDoser>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetSetPointLimits", Json("{}"));

        Assert.True(result.Success);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("0", json);
        Assert.Contains("999", json);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsSetpointSpeedSample()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 2400, commandTerminator: "\r",
            parity: System.IO.Ports.Parity.Odd);
        await transport.ConnectAsync();
        var driver = new LambdaDoser(transport, NullLogger<LambdaDoser>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 1) break;
        }

        Assert.Single(samples);
        Assert.Equal("lambdadoser.setpointspeed", samples[0].TrendName);
    }
}
