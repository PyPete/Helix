// TODO: Add NetOceanDirect NuGet reference to use this class at runtime.
// Without the reference, all methods throw NotImplementedException.
using System.Runtime.Versioning;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// CDll wrapper implementing IOceanViewDriver. Wraps the NetOceanDirect vendor SDK.
/// This is NOT a DeviceAdapterBase — it is the native integration layer.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class OceanViewDriver : IOceanViewDriver
{
    // TODO: Add NetOceanDirect NuGet reference and uncomment vendor SDK calls below.
    // private readonly OceanDirect _controller;
    // private readonly OpticalBench _ob;

    public OceanViewDriver()
    {
        // TODO: _controller = OceanDirect.getInstance();
        // TODO: _ob = OpticalBench.getInstance();
    }

    public DeviceResult OpenDevice(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public DeviceResult CloseDevice(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public List<(int Id, string Name, DeviceKind DeviceKind)> FindActiveDevices()
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public string GetDeviceModel(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public (DeviceResult Result, double[]? Spectrum) GetSpectrum(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public (DeviceResult Result, double[] Wavelengths) GetWavelengths(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public DeviceResult ApplyElectricDarkCorrection(int deviceId, bool apply)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public DeviceResult ApplyNonLinearityCorrection(int deviceId, bool apply)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public DeviceResult SetScansToAverage(int deviceId, int numberOfScans)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public (DeviceResult Result, int Scans) GetScansToAverage(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public DeviceResult SetBoxcarWidth(int deviceId, int width)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public (DeviceResult Result, int Width) GetBoxcarWidth(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public DeviceResult SetIntegrationTime(int deviceId, int milliseconds)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public (DeviceResult Result, int IntegrationTime) GetIntegrationTime(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public (DeviceResult Result, int IntegrationTime) GetMinIntegrationTime(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public (DeviceResult Result, int IntegrationTime) GetMaxIntegrationTime(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public (DeviceResult Result, int StepSize) GetIntegrationStepSize(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public string GetApiVersion(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");

    public (ushort Diameter, string ProbeCoating, string Filter, string SerialNum, ushort SlitWidth) GetProbeDetails(int deviceId)
        => throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");
}
