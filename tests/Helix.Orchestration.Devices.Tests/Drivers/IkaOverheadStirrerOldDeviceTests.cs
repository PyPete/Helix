using System.Text;
using System.Text.Json;
using System.IO.Ports;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class IkaOverheadStirrerOldDeviceTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task SetPoint_ValidRpm_UsesEvenParityAndSevenDataBits_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("\r\n");          // OUT_SP_4 50 \r\n response
        port.EnqueueRead("50.0 4\r\n");    // IN_SP_4\r\n response
        // IkaOverheadStirrerOldDevice must be created with Parity=Even, DataBits=7
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n",
            parity: Parity.Even, dataBits: 7);
        await transport.ConnectAsync();
        var driver = new IkaOverheadStirrerOldDevice(transport, NullLogger<IkaOverheadStirrerOldDevice>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetPoint", Json("{\"rpm\":50}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("OUT_SP_4 50 \r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task GetTorque_SendsInPv5Command_ReturnsTorque()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("0.2 5\r\n");
        var transport = FakeSerial.MakeTransport(port, baudRate: 9600, commandTerminator: "\r\n",
            parity: Parity.Even, dataBits: 7);
        await transport.ConnectAsync();
        var driver = new IkaOverheadStirrerOldDevice(transport, NullLogger<IkaOverheadStirrerOldDevice>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetTorque", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("IN_PV_5\r\n"), port.Writes[0]);
    }
}
