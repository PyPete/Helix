using System.Runtime.Versioning;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

[SupportedOSPlatform("windows")]
public sealed class PicoTc08Tests
{
    private static PicoTc08 CreateDriver(IPicoApi? pico = null, IDeviceTransport? transport = null)
    {
        pico ??= Substitute.For<IPicoApi>();
        transport ??= Substitute.For<IDeviceTransport>();
        return new PicoTc08(pico, transport, NullLogger<PicoTc08>.Instance);
    }

    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    // ---- FindActiveDevices capability ----

    [Fact]
    public async Task FindActiveDevices_ReturnsHandleList()
    {
        var pico = Substitute.For<IPicoApi>();
        pico.FindAndOpenDevices().Returns(new List<short> { 1, 2 });
        var driver = CreateDriver(pico: pico);

        var result = await driver.InvokeCapabilityAsync("FindActiveDevices", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        pico.Received(1).FindAndOpenDevices();
    }

    // ---- IsDevice capability ----

    [Fact]
    public async Task IsDevice_DeviceInfoPresent_ReturnsSuccess()
    {
        var pico = Substitute.For<IPicoApi>();
        pico.GetDeviceInfo(Arg.Any<short>()).Returns("some useful device info string");
        pico.SetChannel(Arg.Any<short>(), Arg.Any<short>(), Arg.Any<ThermoCoupleType>())
            .Returns(DeviceResult.Ok());
        var driver = CreateDriver(pico: pico);
        await driver.InvokeCapabilityAsync("SetActiveDevice", Json("{\"handle\":1}"));

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.True(result.Success);
        pico.Received().SetChannel(1, Arg.Any<short>(), ThermoCoupleType.K);
    }

    [Fact]
    public async Task IsDevice_EmptyDeviceInfo_ReturnsFailure()
    {
        var pico = Substitute.For<IPicoApi>();
        pico.GetDeviceInfo(Arg.Any<short>()).Returns(string.Empty);
        var driver = CreateDriver(pico: pico);

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("TC-08", result.ErrorMessage);
    }

    // ---- Close capability ----

    [Fact]
    public async Task Close_CallsStopDevice_ReturnsSuccess()
    {
        var pico = Substitute.For<IPicoApi>();
        pico.StopDevice(Arg.Any<short>()).Returns(DeviceResult.Ok());
        var driver = CreateDriver(pico: pico);
        await driver.InvokeCapabilityAsync("SetActiveDevice", Json("{\"handle\":1}"));

        var result = await driver.InvokeCapabilityAsync("Close", Json("{}"));

        Assert.True(result.Success);
        pico.Received(1).StopDevice(1);
    }

    // ---- SetChannelThermocoupleType ----

    [Fact]
    public async Task SetChannelThermocoupleType_ValidChannel_ReturnsSuccess()
    {
        var pico = Substitute.For<IPicoApi>();
        pico.SetChannel(Arg.Any<short>(), Arg.Any<short>(), Arg.Any<ThermoCoupleType>())
            .Returns(DeviceResult.Ok());
        var driver = CreateDriver(pico: pico);

        var result = await driver.InvokeCapabilityAsync("SetChannelThermocoupleType",
            Json("{\"channel\":1,\"thermocoupleType\":\"J\"}"));

        Assert.True(result.Success);
        pico.Received(1).SetChannel(Arg.Any<short>(), 1, ThermoCoupleType.J);
    }

    [Fact]
    public async Task SetChannelThermocoupleType_FailedSet_ReturnsFailure()
    {
        var pico = Substitute.For<IPicoApi>();
        pico.SetChannel(Arg.Any<short>(), Arg.Any<short>(), Arg.Any<ThermoCoupleType>())
            .Returns(DeviceResult.Fail("Could not set"));
        var driver = CreateDriver(pico: pico);

        var result = await driver.InvokeCapabilityAsync("SetChannelThermocoupleType",
            Json("{\"channel\":1,\"thermocoupleType\":\"K\"}"));

        Assert.False(result.Success);
    }

    // ---- SetChannelsThermocoupleType ----

    [Fact]
    public async Task SetChannelsThermocoupleType_PartialFailure_ReturnsFailure()
    {
        var pico = Substitute.For<IPicoApi>();
        pico.SetChannels(Arg.Any<short>(), Arg.Any<Dictionary<short, ThermoCoupleType>>())
            .Returns(new List<DeviceResult> { DeviceResult.Ok(), DeviceResult.Fail("Could not map") });
        var driver = CreateDriver(pico: pico);
        await driver.InvokeCapabilityAsync("SetActiveDevice", Json("{\"handle\":1}"));

        var result = await driver.InvokeCapabilityAsync("SetChannelsThermocoupleType",
            Json("{\"channels\":{\"1\":\"J\",\"2\":\"J\"}}"));

        Assert.False(result.Success);
    }

    // ---- GetThermocoupleInformation ----

    [Fact]
    public async Task GetThermocoupleInformation_Returns8Channels()
    {
        var driver = CreateDriver();

        var result = await driver.InvokeCapabilityAsync("GetThermocoupleInformation", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        var obj = result.ReturnValue!.Value;
        Assert.Equal(8, obj.EnumerateObject().Count());
    }

    // ---- GetUniqueId ----

    [Fact]
    public async Task GetUniqueId_WithFormattedInfo_ReturnsSerialNumber()
    {
        var pico = Substitute.For<IPicoApi>();
        pico.GetDeviceInfo(Arg.Any<short>()).Returns(
            "Driver version: 2.0.57.2542\r\nPicopp.sys version: 01.00\r\nHardware version: 1\r\nUnit variant: 3\r\nBatch and serial: AO114/302\r\nCalibration date: 07Jul21");
        var driver = CreateDriver(pico: pico);

        var result = await driver.InvokeCapabilityAsync("GetUniqueId", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("AO114", result.ReturnValue!.Value.GetString());
    }

    // ---- StreamTrendsAsync ----

    [Fact]
    public async Task StreamTrendsAsync_EmitsTemperatureSamples()
    {
        var pico = Substitute.For<IPicoApi>();
        var data = new float[] { 0f, 22.5f, 23.1f, 24.0f, 25.5f, 26.0f, 27.1f, 28.0f, 29.2f };
        pico.GetData(Arg.Any<short>())
            .Returns((DeviceResult.Ok(), data, PicoTempUnit.Usbtc08UnitsCentigrade));
        var driver = CreateDriver(pico: pico);

        var samples = new List<TrendSample>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 8) break;
        }

        Assert.Equal(8, samples.Count);
        Assert.All(samples, s => Assert.StartsWith("picotc08.temperature.ch", s.TrendName));
    }

    [Fact]
    public async Task StreamTrendsAsync_GetDataFailure_EmitsNoSamples()
    {
        var pico = Substitute.For<IPicoApi>();
        pico.GetData(Arg.Any<short>())
            .Returns((DeviceResult.Fail("error"), new float[0], PicoTempUnit.Usbtc08UnitsCentigrade));
        var driver = CreateDriver(pico: pico);

        var samples = new List<TrendSample>();
        using var cts = new CancellationTokenSource(500);
        try
        {
            await foreach (var s in driver.StreamTrendsAsync(cts.Token))
            {
                samples.Add(s);
                if (samples.Count > 0) break;
            }
        }
        catch (OperationCanceledException) { /* expected */ }

        Assert.Empty(samples);
    }

    // ---- Does NOT call Transport.SendReceiveAsync ----

    [Fact]
    public async Task Driver_DoesNotCallTransport()
    {
        var transport = Substitute.For<IDeviceTransport>();
        var pico = Substitute.For<IPicoApi>();
        pico.FindAndOpenDevices().Returns(new List<short> { 1 });
        var driver = new PicoTc08(pico, transport, NullLogger<PicoTc08>.Instance);

        await driver.InvokeCapabilityAsync("FindActiveDevices", Json("{}"));
        await driver.InvokeCapabilityAsync("Close", Json("{}"));

        await transport.DidNotReceive().SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    // ---- PicoTc08LowLevelWrapper does NOT extend DeviceAdapterBase ----

    [Fact]
    public void PicoTc08LowLevelWrapper_IsNotDeviceAdapterBase()
    {
        Assert.False(typeof(PicoTc08LowLevelWrapper).IsSubclassOf(typeof(DeviceAdapterBase)));
    }

    // ---- PicoTc08 IS a DeviceAdapterBase ----

    [Fact]
    public void PicoTc08_IsDeviceAdapterBase()
    {
        Assert.True(typeof(PicoTc08).IsSubclassOf(typeof(DeviceAdapterBase)));
    }
}
