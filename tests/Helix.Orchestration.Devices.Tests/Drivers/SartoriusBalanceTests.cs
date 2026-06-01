using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class SartoriusBalanceTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task GetWeight_SendsEscPCommand_ReturnsWeight()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("N     -     0.04   g\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 1200, commandTerminator: "\r\n",
            parity: System.IO.Ports.Parity.Odd, dataBits: 7);
        await transport.ConnectAsync();
        var driver = new SartoriusBalance(transport, NullLogger<SartoriusBalance>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetWeight", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("\x1BP\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task GetWeight_EmptyResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 1200, commandTerminator: "\r\n",
            parity: System.IO.Ports.Parity.Odd, dataBits: 7);
        await transport.ConnectAsync();
        var driver = new SartoriusBalance(transport, NullLogger<SartoriusBalance>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetWeight", Json("{}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Tare_SendsEscTCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 1200, commandTerminator: "\r\n",
            parity: System.IO.Ports.Parity.Odd, dataBits: 7);
        await transport.ConnectAsync();
        var driver = new SartoriusBalance(transport, NullLogger<SartoriusBalance>.Instance);

        var result = await driver.InvokeCapabilityAsync("Tare", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("\x1BT\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task GetUniqueId_SendsEscx1Command_ReturnsSerialNumber()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("CP3202S\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 1200, commandTerminator: "\r\n",
            parity: System.IO.Ports.Parity.Odd, dataBits: 7);
        await transport.ConnectAsync();
        var driver = new SartoriusBalance(transport, NullLogger<SartoriusBalance>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetUniqueId", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("\x1Bx1_\r\n"), port.Writes[0]);
        Assert.Contains("CP3202S", result.ReturnValue!.Value.ToString());
    }
}
