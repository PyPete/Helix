using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.Interfaces;

public interface ISetPointDevice : IDevice
{
    DeviceResult Start();

    DeviceResult Stop();

    DeviceResult SetPoint(double value);

    (DeviceResult Result, double SetPoint) GetSetPoint();

    (DeviceResult Result, double Min, double Max) GetSetPointLimits();
}
