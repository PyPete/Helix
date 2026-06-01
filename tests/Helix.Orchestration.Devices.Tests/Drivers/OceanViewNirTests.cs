using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class OceanViewNirTests
{
    [Fact]
    public void OceanViewNir_IsNotDeviceAdapterBase_SubtypeOf_DeviceAdapterBase()
    {
        // OceanViewNir IS a DeviceAdapterBase (facade), unlike OceanViewDriver (plain class)
        Assert.True(typeof(OceanViewNir).IsSubclassOf(typeof(DeviceAdapterBase)));
    }

    [Fact]
    public void OceanViewNir_DoesNotOverrideStreamTrendsAsync()
    {
        var type = typeof(OceanViewNir);
        var method = type.GetMethod(nameof(OceanViewNir.StreamTrendsAsync));
        Assert.Equal(typeof(DeviceAdapterBase), method!.DeclaringType);
    }

    [Fact]
    public async Task IsDevice_OpensDevice_ReturnsSuccess()
    {
        var mock = Substitute.For<IOceanViewDriver>();
        mock.OpenDevice(Arg.Any<int>()).Returns(DeviceResult.Ok());
        mock.ApplyElectricDarkCorrection(Arg.Any<int>(), Arg.Any<bool>()).Returns(DeviceResult.Ok());
        mock.SetIntegrationTime(Arg.Any<int>(), Arg.Any<int>()).Returns(DeviceResult.Ok());
        mock.SetScansToAverage(Arg.Any<int>(), Arg.Any<int>()).Returns(DeviceResult.Ok());
        var transport = Substitute.For<IDeviceTransport>();
        var driver = new OceanViewNir(mock, transport, NullLogger<OceanViewNir>.Instance);

        using var doc = System.Text.Json.JsonDocument.Parse("{\"deviceId\":2}");
        await driver.InvokeCapabilityAsync("Open", doc.RootElement.Clone());

        using var emptyDoc = System.Text.Json.JsonDocument.Parse("{}");
        var result = await driver.InvokeCapabilityAsync("IsDevice", emptyDoc.RootElement.Clone());

        Assert.True(result.Success);
        mock.Received().OpenDevice(2);
    }

    [Fact]
    public async Task GetSpectrum_ReturnsSpectrumArray()
    {
        var mock = Substitute.For<IOceanViewDriver>();
        var spectrum = Enumerable.Range(1, 512).Select(Convert.ToDouble).ToArray();
        mock.GetSpectrum(Arg.Any<int>()).Returns((DeviceResult.Ok(), (double[]?)spectrum));
        var transport = Substitute.For<IDeviceTransport>();
        var driver = new OceanViewNir(mock, transport, NullLogger<OceanViewNir>.Instance);

        using var doc = System.Text.Json.JsonDocument.Parse("{}");
        var result = await driver.InvokeCapabilityAsync("GetSpectrum", doc.RootElement.Clone());

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
    }
}
