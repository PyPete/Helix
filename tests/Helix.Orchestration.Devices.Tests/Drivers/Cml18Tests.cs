using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class Cml18Tests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    private const string SampleResponse =
        "M;EH_CML18_RA025905MH0;RA025905MH0;Conductivity;5.739;??S/cm;Temperature;18.7;??C;RawValueCond;4.976;??S/cm;2000-01-04 08:01:00;W1038605LC0";

    [Fact]
    public async Task GetMeasurement_ParsesValidResponse_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead(SampleResponse + "\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Cml18(transport, NullLogger<Cml18>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetMeasurement", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("Conductivity", json);
        Assert.Contains("5.739", json);
        Assert.Contains("18.7", json);
        Assert.Contains("W1038605LC0", json);
    }

    [Fact]
    public async Task GetMeasurement_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Cml18(transport, NullLogger<Cml18>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetMeasurement", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("No response", result.ErrorMessage);
    }

    [Fact]
    public async Task GetMeasurement_MalformedResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("M;EH_CML18_RA025905MH0;RA025905MH0;Conductiv\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Cml18(transport, NullLogger<Cml18>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetMeasurement", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("parse", result.ErrorMessage);
    }

    [Fact]
    public async Task StreamTrendsAsync_EmitsConductivityAndTemperatureSamples()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead(SampleResponse + "\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n");
        await transport.ConnectAsync();
        var driver = new Cml18(transport, NullLogger<Cml18>.Instance);

        var samples = new List<Helix.Orchestration.Devices.Abstractions.TrendSample>();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 2) break;
        }

        Assert.Equal(2, samples.Count);
        Assert.Contains(samples, s => s.TrendName.Contains("conductivity"));
        Assert.Contains(samples, s => s.TrendName == "cml18.temperature");
    }
}
