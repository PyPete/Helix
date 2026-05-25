using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.BaseClasses;

public abstract class SetPointDeviceBase(string? name = null) : DeviceBase(name), ISetPointDevice
{
    protected abstract double[] AvailableLimits { get; }

    public abstract DeviceResult Start();

    public abstract DeviceResult Stop();

    public abstract DeviceResult SetPoint(double value);

    public abstract (DeviceResult Result, double SetPoint) GetSetPoint();

    public virtual (DeviceResult Result, double Min, double Max) GetSetPointLimits()
    {
        if (AvailableLimits.Length < 2)
            return (Failure("Setpoint limits are not configured."), 0, 0);

        return (Success(), AvailableLimits[0], AvailableLimits[1]);
    }
}
