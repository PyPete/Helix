using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class OceanViewUvTests
{
    private static readonly double[] FakeWavelengths =
        Enumerable.Range(1, 1000).Select(Convert.ToDouble).ToArray();

    private static IOceanViewDriver SetupOceanViewDriver()
    {
        var mock = Substitute.For<IOceanViewDriver>();
        mock.OpenDevice(Arg.Any<int>()).Returns(DeviceResult.Ok());
        mock.ApplyElectricDarkCorrection(Arg.Any<int>(), Arg.Any<bool>()).Returns(DeviceResult.Ok());
        mock.ApplyNonLinearityCorrection(Arg.Any<int>(), Arg.Any<bool>()).Returns(DeviceResult.Ok());
        mock.SetIntegrationTime(Arg.Any<int>(), Arg.Any<int>()).Returns(DeviceResult.Ok());
        mock.SetScansToAverage(Arg.Any<int>(), Arg.Any<int>()).Returns(DeviceResult.Ok());
        mock.SetBoxcarWidth(Arg.Any<int>(), Arg.Any<int>()).Returns(DeviceResult.Ok());
        mock.GetWavelengths(Arg.Any<int>()).Returns((DeviceResult.Ok(), FakeWavelengths));
        mock.GetSpectrum(Arg.Any<int>()).Returns((DeviceResult.Ok(), (double[]?)FakeWavelengths));
        mock.CloseDevice(Arg.Any<int>()).Returns(DeviceResult.Ok());
        return mock;
    }

    private static OceanViewUv CreateDriver(IOceanViewDriver? driver = null, IDeviceTransport? transport = null)
    {
        driver ??= Substitute.For<IOceanViewDriver>();
        transport ??= Substitute.For<IDeviceTransport>();
        return new OceanViewUv(driver, transport, NullLogger<OceanViewUv>.Instance);
    }

    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    // ---- Open + IsDevice ----

    [Fact]
    public async Task IsDevice_DeviceOpens_ReturnsSuccess()
    {
        var mock = SetupOceanViewDriver();
        var driver = CreateDriver(driver: mock);
        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.True(result.Success);
        mock.Received().OpenDevice(1);
    }

    [Fact]
    public async Task IsDevice_DeviceFailsToOpen_ReturnsFailure()
    {
        var mock = Substitute.For<IOceanViewDriver>();
        mock.OpenDevice(Arg.Any<int>()).Returns(DeviceResult.Fail("Device did not open"));
        var driver = CreateDriver(driver: mock);
        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.False(result.Success);
    }

    // ---- Close ----

    [Fact]
    public async Task Close_ReturnsSuccess()
    {
        var mock = SetupOceanViewDriver();
        var driver = CreateDriver(driver: mock);
        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));
        await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        var result = await driver.InvokeCapabilityAsync("Close", Json("{}"));

        Assert.True(result.Success);
        mock.Received().CloseDevice(1);
    }

    // ---- ElectricDark ----

    [Fact]
    public async Task TurnOnElectricDark_ReturnsSuccess()
    {
        var mock = SetupOceanViewDriver();
        var driver = CreateDriver(driver: mock);
        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));

        var on = await driver.InvokeCapabilityAsync("TurnOnElectricDark", Json("{}"));
        var off = await driver.InvokeCapabilityAsync("TurnOffElectricDark", Json("{}"));

        Assert.True(on.Success);
        Assert.True(off.Success);
        mock.Received().ApplyElectricDarkCorrection(1, true);
        mock.Received().ApplyElectricDarkCorrection(1, false);
    }

    // ---- NonLinearCorrection ----

    [Fact]
    public async Task TurnOnNonLinearCorrection_ReturnsSuccess()
    {
        var mock = SetupOceanViewDriver();
        mock.ApplyNonLinearityCorrection(Arg.Any<int>(), Arg.Any<bool>()).Returns(DeviceResult.Ok());
        var driver = CreateDriver(driver: mock);
        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));

        var on = await driver.InvokeCapabilityAsync("TurnOnNonLinearCorrection", Json("{}"));
        var off = await driver.InvokeCapabilityAsync("TurnOffNonLinearCorrection", Json("{}"));

        Assert.True(on.Success);
        Assert.True(off.Success);
    }

    // ---- ScansToAverage ----

    [Fact]
    public async Task SetScansToAverage_ReturnsSuccess()
    {
        var mock = SetupOceanViewDriver();
        var driver = CreateDriver(driver: mock);
        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));

        var result = await driver.InvokeCapabilityAsync("SetScansToAverage", Json("{\"numberOfScans\":10}"));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetScansToAverage_ReturnsValue()
    {
        var mock = SetupOceanViewDriver();
        mock.GetScansToAverage(Arg.Any<int>()).Returns((DeviceResult.Ok(), 5));
        var driver = CreateDriver(driver: mock);
        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));

        var result = await driver.InvokeCapabilityAsync("GetScansToAverage", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        Assert.Equal(5, result.ReturnValue!.Value.GetInt32());
    }

    // ---- BoxcarWidth ----

    [Fact]
    public async Task SetBoxcarWidth_ReturnsSuccess()
    {
        var mock = SetupOceanViewDriver();
        var driver = CreateDriver(driver: mock);
        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));

        var result = await driver.InvokeCapabilityAsync("SetBoxcarWidth", Json("{\"width\":10}"));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetBoxcarWidth_ReturnsValue()
    {
        var mock = SetupOceanViewDriver();
        mock.GetBoxcarWidth(Arg.Any<int>()).Returns((DeviceResult.Ok(), 10));
        var driver = CreateDriver(driver: mock);
        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));

        var result = await driver.InvokeCapabilityAsync("GetBoxcarWidth", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        Assert.Equal(10, result.ReturnValue!.Value.GetInt32());
    }

    // ---- Integration times ----

    [Fact]
    public async Task GetIntegrationTimes_ReturnCorrectValues()
    {
        var mock = SetupOceanViewDriver();
        mock.GetMinIntegrationTime(Arg.Any<int>()).Returns((DeviceResult.Ok(), 1));
        mock.GetMaxIntegrationTime(Arg.Any<int>()).Returns((DeviceResult.Ok(), 1000));
        mock.GetIntegrationTime(Arg.Any<int>()).Returns((DeviceResult.Ok(), 5));
        mock.GetIntegrationStepSize(Arg.Any<int>()).Returns((DeviceResult.Ok(), 2));
        var driver = CreateDriver(driver: mock);
        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));

        var minResult = await driver.InvokeCapabilityAsync("GetMinIntegrationTime", Json("{}"));
        var maxResult = await driver.InvokeCapabilityAsync("GetMaxIntegrationTime", Json("{}"));
        var curResult = await driver.InvokeCapabilityAsync("GetIntegrationTime", Json("{}"));
        var stepResult = await driver.InvokeCapabilityAsync("GetIntegrationStepSize", Json("{}"));

        Assert.True(minResult.Success);
        Assert.True(maxResult.Success);
        Assert.True(curResult.Success);
        Assert.True(stepResult.Success);
        Assert.Equal(1, minResult.ReturnValue!.Value.GetInt32());
        Assert.Equal(1000, maxResult.ReturnValue!.Value.GetInt32());
        Assert.Equal(5, curResult.ReturnValue!.Value.GetInt32());
        Assert.Equal(2, stepResult.ReturnValue!.Value.GetInt32());
    }

    // ---- GetSpectrum ----

    [Fact]
    public async Task GetSpectrum_ReturnsSpectrumArray()
    {
        var mock = SetupOceanViewDriver();
        var driver = CreateDriver(driver: mock);
        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));

        var result = await driver.InvokeCapabilityAsync("GetSpectrum", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
    }

    [Fact]
    public async Task GetSpectrum_FailureResponse_ReturnsFailure()
    {
        var mock = Substitute.For<IOceanViewDriver>();
        mock.GetSpectrum(Arg.Any<int>()).Returns((DeviceResult.Fail("Scan failed"), null));
        var driver = CreateDriver(driver: mock);
        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));

        var result = await driver.InvokeCapabilityAsync("GetSpectrum", Json("{}"));

        Assert.False(result.Success);
    }

    // ---- FindAllDevices ----

    [Fact]
    public async Task FindAllDevices_ReturnsDeviceList()
    {
        var mock = SetupOceanViewDriver();
        mock.FindActiveDevices().Returns(new List<(int Id, string Name, DeviceKind DeviceKind)>
        {
            (1, "USB2000", DeviceKind.Uv)
        });
        var driver = CreateDriver(driver: mock);

        var result = await driver.InvokeCapabilityAsync("FindAllDevices", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
    }

    // ---- Does NOT call Transport.SendReceiveAsync ----

    [Fact]
    public async Task Driver_DoesNotCallTransport()
    {
        var transport = Substitute.For<IDeviceTransport>();
        var mock = SetupOceanViewDriver();
        var driver = new OceanViewUv(mock, transport, NullLogger<OceanViewUv>.Instance);

        await driver.InvokeCapabilityAsync("Open", Json("{\"deviceId\":1}"));
        await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));
        await driver.InvokeCapabilityAsync("Close", Json("{}"));

        await transport.DidNotReceive().SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    // ---- OceanViewUv does NOT override StreamTrendsAsync ----

    [Fact]
    public void OceanViewUv_DoesNotOverrideStreamTrendsAsync()
    {
        var type = typeof(OceanViewUv);
        var method = type.GetMethod(nameof(OceanViewUv.StreamTrendsAsync));
        // StreamTrendsAsync should be inherited from base class (DeviceAdapterBase), not overridden
        Assert.Equal(typeof(DeviceAdapterBase), method!.DeclaringType);
    }
}
