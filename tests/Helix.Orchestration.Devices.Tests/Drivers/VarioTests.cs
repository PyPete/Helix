using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class VarioTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task Start_SendsStartCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Vario(transport, NullLogger<Vario>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("START \r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Stop_SendsStop1Command_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Vario(transport, NullLogger<Vario>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("STOP 1\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task SetPoint_ValidMbar_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Vario(transport, NullLogger<Vario>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"mbar\":100}"));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task SetPoint_OutsideLimits_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Vario(transport, NullLogger<Vario>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"mbar\":0}"));

        Assert.False(result.Success);
        Assert.Contains("outside of device range", result.ErrorMessage);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsPressureAndFrequency()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("1017 mbar\r\n");
        port.EnqueueRead("00.0 Hz\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Vario(transport, NullLogger<Vario>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 2) break;
        }

        Assert.Equal(2, samples.Count);
        Assert.Contains(samples, s => s.TrendName == "vario.pressure");
        Assert.Contains(samples, s => s.TrendName == "vario.frequency");
    }
}
