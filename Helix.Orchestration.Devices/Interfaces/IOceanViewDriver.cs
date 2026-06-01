using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.Interfaces;

/// <summary>
/// Local copy of the POP IOceanViewDriver interface. CDll vendor integration layer for OceanView spectrometers.
/// </summary>
public interface IOceanViewDriver
{
    DeviceResult OpenDevice(int deviceId);
    DeviceResult CloseDevice(int deviceId);
    List<(int Id, string Name, DeviceKind DeviceKind)> FindActiveDevices();
    string GetDeviceModel(int deviceId);
    (DeviceResult Result, double[]? Spectrum) GetSpectrum(int deviceId);
    (DeviceResult Result, double[] Wavelengths) GetWavelengths(int deviceId);
    DeviceResult ApplyElectricDarkCorrection(int deviceId, bool apply);
    DeviceResult ApplyNonLinearityCorrection(int deviceId, bool apply);
    DeviceResult SetScansToAverage(int deviceId, int numberOfScans);
    (DeviceResult Result, int Scans) GetScansToAverage(int deviceId);
    DeviceResult SetBoxcarWidth(int deviceId, int width);
    (DeviceResult Result, int Width) GetBoxcarWidth(int deviceId);
    DeviceResult SetIntegrationTime(int deviceId, int milliseconds);
    (DeviceResult Result, int IntegrationTime) GetIntegrationTime(int deviceId);
    (DeviceResult Result, int IntegrationTime) GetMinIntegrationTime(int deviceId);
    (DeviceResult Result, int IntegrationTime) GetMaxIntegrationTime(int deviceId);
    (DeviceResult Result, int StepSize) GetIntegrationStepSize(int deviceId);
    string GetApiVersion(int deviceId);
    (ushort Diameter, string ProbeCoating, string Filter, string SerialNum, ushort SlitWidth) GetProbeDetails(int deviceId);
}
