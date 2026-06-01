using System.Text;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class AalborgDfmTests
{
    private static AalborgDfm CreateDriver(params string[] responses)
    {
        var port = new FakeSerialPortConnection();
        foreach (var r in responses)
            port.EnqueueRead(r + "\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        var driver = new AalborgDfm(transport, NullLogger<AalborgDfm>.Instance);
        transport.ConnectAsync().GetAwaiter().GetResult();
        return driver;
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task GetTemperature_SendsTrCommand_ReturnsTemperature()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("25.0 C\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new AalborgDfm(transport, NullLogger<AalborgDfm>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetTemperature", System.Text.Json.JsonDocument.Parse("{}").RootElement.Clone());

        Assert.True(result.Success);
        Assert.Equal(Ascii("TR\r"), port.Writes[0]);
    }

    [Fact]
    public async Task GetPressure_SendsPrCommand_ReturnsPressure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("14.5 PSI\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new AalborgDfm(transport, NullLogger<AalborgDfm>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetPressure", System.Text.Json.JsonDocument.Parse("{}").RootElement.Clone());

        Assert.True(result.Success);
        Assert.Equal(Ascii("PR\r"), port.Writes[0]);
    }

    [Fact]
    public async Task GetFlow_SendsFCommand_ReturnsFlow()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("50.0 ml/min\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new AalborgDfm(transport, NullLogger<AalborgDfm>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetFlow", System.Text.Json.JsonDocument.Parse("{}").RootElement.Clone());

        Assert.True(result.Success);
        Assert.Equal(Ascii("F\r"), port.Writes[0]);
    }

    [Fact]
    public async Task GetUniqueId_SendsMr1Command_ReturnsSerialNumber()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("SN12345\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new AalborgDfm(transport, NullLogger<AalborgDfm>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetUniqueId", System.Text.Json.JsonDocument.Parse("{}").RootElement.Clone());

        Assert.True(result.Success);
        Assert.Equal(Ascii("MR1\r"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("SN12345", result.ReturnValue!.Value.ToString());
    }

    [Fact]
    public async Task GetTemperature_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new AalborgDfm(transport, NullLogger<AalborgDfm>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetTemperature", System.Text.Json.JsonDocument.Parse("{}").RootElement.Clone());

        Assert.False(result.Success);
        Assert.Contains("No response", result.ErrorMessage);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsFlowAndPressureAndTempSamples()
    {
        var port = new FakeSerialPortConnection();
        // PR, F, TR cycle
        port.EnqueueRead("14.5 PSI\r");
        port.EnqueueRead("50.0 ml/min\r");
        port.EnqueueRead("25.0 C\r");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r");
        await transport.ConnectAsync();
        var driver = new AalborgDfm(transport, NullLogger<AalborgDfm>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 3) break;
        }

        Assert.True(samples.Count >= 1);
        Assert.Contains(samples, s => s.TrendName == "aalborgdfm.pressure");
    }
}
