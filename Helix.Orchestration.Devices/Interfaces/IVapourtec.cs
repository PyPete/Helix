using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.Interfaces;

public interface IVapourtec : IDevice
{
    DeviceResult Start();

    DeviceResult Stop();

    DeviceResult SetFlowRate(int pump, double flowRate);

    DeviceResult SetTemperature(int reactor, int target);

    DeviceResult SelectReagent(int channel);

    DeviceResult SelectSolvent(int channel);

    /// <summary>
    /// Queries the device for its current operational status.
    /// </summary>
    (DeviceResult Result, VapourtecStatus? Status) GetStatus();
}
