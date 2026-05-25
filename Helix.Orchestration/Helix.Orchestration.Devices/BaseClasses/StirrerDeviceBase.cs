using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.BaseClasses;

public abstract class StirrerDeviceBase(string? name = null) : SetPointDeviceBase(name)
{
    public override DeviceKind DeviceKind => DeviceKind.OverheadStirrer;

    protected int CurrentSpeed { get; set; }

    protected StirrerDeviceBase RegisterDefaults()
    {
        RegisterMeasurementName("ProcessTorque", "ProcessTorque");
        RegisterMeasurementName("ProcessSpeed", "ProcessSpeed");
        RegisterMeasurementName("SetPointSpeed", "SetPointSpeed");
        return this;
    }
}
