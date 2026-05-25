using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.BaseClasses;

public abstract class VacPumpDeviceBase(string? name = null) : SetPointDeviceBase(name)
{
    public override DeviceKind DeviceKind => DeviceKind.VacPump;

    public abstract DeviceResult Vent();

    protected VacPumpDeviceBase RegisterDefaults()
    {
        RegisterMeasurementName("ProcessFrequency", "ProcessFrequency");
        RegisterMeasurementName("ProcessPressure", "ProcessPressure");
        return this;
    }
}
