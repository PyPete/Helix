using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.Interfaces;

public interface IDevice
{
    DeviceKind DeviceKind { get; }

    string Name { get; }

    DeviceDetails GetDeviceDetails();

    DeviceResult IsDevice();

    DeviceResult Close();

    (DeviceResult Result, string UniqueIdentifier) GetUniqueId();

    IReadOnlyList<DeviceMeasurement> GetMeasurements();

    DeviceResult UpdateMeasurementName(string oldName, string newName);

    double GetTimingLowerLimit();
}
