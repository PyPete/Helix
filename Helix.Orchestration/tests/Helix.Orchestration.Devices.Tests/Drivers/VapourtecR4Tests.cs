using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Model;
using Helix.Orchestration.Devices.Transport;
using Moq;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class VapourtecR4Tests
{
    private static Mock<ISerialTransport> CreateMockTransport()
    {
        var mock = new Mock<ISerialTransport>();
        mock.Setup(t => t.IsOpen).Returns(true);
        return mock;
    }

    [Fact]
    public void IsDevice_ReturnsSuccess_WhenFirmwareAndFlowHistoryAreValid()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.SendCommand("GV", 300)).Returns("v1.122");
        mock.Setup(t => t.SendCommand("HF", 300)).Returns("19921,0,0");

        var device = new VapourtecR4(mock.Object);

        var result = device.IsDevice();

        Assert.Equal(DeviceResultState.Success, result.State);
    }

    [Fact]
    public void IsDevice_ReturnsFailure_WhenFlowHistoryMissing()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.SendCommand("GV", 300)).Returns("v1.122");
        mock.Setup(t => t.SendCommand("HF", 300)).Returns(string.Empty);

        var device = new VapourtecR4(mock.Object);

        var result = device.IsDevice();

        Assert.Equal(DeviceResultState.Failure, result.State);
    }

    [Fact]
    public void GetUniqueId_ReturnsWarning_WhenFirmwareMissing()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.SendCommand("GV", 300)).Returns(string.Empty);

        var device = new VapourtecR4(mock.Object);

        var (result, uniqueId) = device.GetUniqueId();

        Assert.Equal(DeviceResultState.Warning, result.State);
        Assert.Equal(string.Empty, uniqueId);
    }

    [Fact]
    public void Start_SendsMainAndForwardPowerOn()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.SendCommand("PN", 350)).Returns("OK");
        mock.Setup(t => t.SendCommand("FW PN", 350)).Returns("OK");

        var device = new VapourtecR4(mock.Object);

        var result = device.Start();

        Assert.Equal(DeviceResultState.Success, result.State);
        mock.Verify(t => t.SendCommand("PN", 350), Times.Once);
        mock.Verify(t => t.SendCommand("FW PN", 350), Times.Once);
    }

    [Fact]
    public void Stop_SendsMainAndForwardPowerOff()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.SendCommand("PF", 350)).Returns("OK");
        mock.Setup(t => t.SendCommand("FW PF", 350)).Returns("OK");

        var device = new VapourtecR4(mock.Object);

        var result = device.Stop();

        Assert.Equal(DeviceResultState.Success, result.State);
        mock.Verify(t => t.SendCommand("PF", 350), Times.Once);
        mock.Verify(t => t.SendCommand("FW PF", 350), Times.Once);
    }

    [Fact]
    public void SetFlowRate_MainPump_UsesDirectFRWithScaledUnits()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.SendCommand("FR 1 2500", 400)).Returns("OK");

        var device = new VapourtecR4(mock.Object);

        var result = device.SetFlowRate(1, 2.5);

        Assert.Equal(DeviceResultState.Success, result.State);
        mock.Verify(t => t.SendCommand("FR 1 2500", 400), Times.Once);
    }

    [Fact]
    public void SetFlowRate_ForwardPump_UsesFWPrefixAndIndexOffset()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.SendCommand("FW FR 1 2500", 400)).Returns("OK");

        var device = new VapourtecR4(mock.Object);

        var result = device.SetFlowRate(3, 2.5);

        Assert.Equal(DeviceResultState.Success, result.State);
        mock.Verify(t => t.SendCommand("FW FR 1 2500", 400), Times.Once);
    }

    [Fact]
    public void SetTemperature_UsesR4Prefix()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.SendCommand("R4 ST 2 80", 300)).Returns("OK");

        var device = new VapourtecR4(mock.Object);

        var result = device.SetTemperature(2, 80);

        Assert.Equal(DeviceResultState.Success, result.State);
        mock.Verify(t => t.SendCommand("R4 ST 2 80", 300), Times.Once);
    }

    [Fact]
    public void SelectReagent_MainChannel_UsesKPCode()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.SendCommand("KP 1", 300)).Returns("OK");

        var device = new VapourtecR4(mock.Object);

        var result = device.SelectReagent(0);

        Assert.Equal(DeviceResultState.Success, result.State);
        mock.Verify(t => t.SendCommand("KP 1", 300), Times.Once);
    }

    [Fact]
    public void SelectSolvent_ForwardChannel_UsesFWKPCode()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.SendCommand("FW KP 2", 300)).Returns("OK");

        var device = new VapourtecR4(mock.Object);

        var result = device.SelectSolvent(3);

        Assert.Equal(DeviceResultState.Success, result.State);
        mock.Verify(t => t.SendCommand("FW KP 2", 300), Times.Once);
    }

    [Fact]
    public void GetStatus_ParsesFaultFromGAResponse()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.SendCommand("GA", 300)).Returns("ER:Overpressure");

        var device = new VapourtecR4(mock.Object);

        var (result, status) = device.GetStatus();

        Assert.Equal(DeviceResultState.Success, result.State);
        Assert.NotNull(status);
        Assert.True(status.HasFault);
        Assert.False(status.IsRunning);
    }

    [Fact]
    public void GetMeasurements_ParsesTempFlowAndPressureHistories()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.SendCommand("HT", 400)).Returns("4913,U,250,U,260,U,270,U,280");
        mock.Setup(t => t.SendCommand("HF", 300)).Returns("19921,1000,2000");
        mock.Setup(t => t.SendCommand("FW HF", 300)).Returns("19921,3000,4000");
        mock.Setup(t => t.SendCommand("HP", 400)).Returns("50821,10000,12000,13000");

        var device = new VapourtecR4(mock.Object);

        var measurements = device.GetMeasurements();

        Assert.Contains(measurements, m => m.TrendName == "Temperature_Reactor_A");
        Assert.Contains(measurements, m => m.TrendName == "PumpA_FlowRate");
        Assert.Contains(measurements, m => m.TrendName == "PumpD_FlowRate");
        Assert.Contains(measurements, m => m.TrendName == "Pressure_3");
    }

    [Fact]
    public void DeviceKind_ReturnsVapourtec()
    {
        var mock = CreateMockTransport();
        var device = new VapourtecR4(mock.Object);

        Assert.Equal(DeviceKind.Vapourtec, device.DeviceKind);
    }

    [Fact]
    public void Close_ClosesTransport()
    {
        var mock = CreateMockTransport();
        mock.Setup(t => t.Close()).Verifiable();

        var device = new VapourtecR4(mock.Object);

        var result = device.Close();

        Assert.Equal(DeviceResultState.Success, result.State);
        mock.Verify(t => t.Close(), Times.Once);
    }
}
