using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.BaseClasses;

public abstract class DeviceBase : IDevice
{
    private readonly Dictionary<string, string> _measurementNames = new(StringComparer.OrdinalIgnoreCase);

    public abstract DeviceKind DeviceKind { get; }

    public string Name { get; private set; }

    protected DeviceBase(string? name = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? GetType().Name.ToLowerInvariant() : name;
    }

    protected DeviceResult Success() => DeviceResult.Ok();

    protected DeviceResult Failure(string reason) => DeviceResult.Fail(reason);

    protected DeviceResult Warning(string reason) => DeviceResult.Warn(reason);

    protected void RegisterMeasurementName(string defaultName, string displayName)
        => _measurementNames[defaultName] = displayName;

    protected string GetMeasurementName(string defaultName)
        => _measurementNames.TryGetValue(defaultName, out var name) ? name : defaultName;

    public DeviceResult UpdateMeasurementName(string oldName, string newName)
    {
        var pair = _measurementNames.FirstOrDefault(kvp => string.Equals(kvp.Value, oldName, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(pair.Key))
            return Failure("Measurement name not found");

        if (_measurementNames.Values.Any(v => string.Equals(v, newName, StringComparison.OrdinalIgnoreCase)))
            return Failure("Measurement name already exists");

        _measurementNames[pair.Key] = newName;
        return Success();
    }

    public abstract DeviceResult IsDevice();

    public abstract DeviceResult Close();

    public abstract (DeviceResult Result, string UniqueIdentifier) GetUniqueId();

    public abstract IReadOnlyList<DeviceMeasurement> GetMeasurements();

    public virtual double GetTimingLowerLimit() => 0.8;

    public virtual DeviceDetails GetDeviceDetails() =>
        new(
            DeviceKind.ToString(),
            GetDeviceInfo(),
            GetConnectionDetails(),
            GetCommands());

    protected virtual IReadOnlyList<DeviceInfoComponent> GetDeviceInfo() => [];

    protected virtual IReadOnlyList<ConnectionComponent> GetConnectionDetails() => [];

    protected virtual IReadOnlyList<CommandComponent> GetCommands() => [];
}
