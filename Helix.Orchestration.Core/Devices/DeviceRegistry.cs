namespace Helix.Orchestration.Core.Devices;

/// <summary>
/// Registry of logical devices available for workflow compilation and execution.
/// Workflows reference devices by alias; the registry resolves aliases to device descriptors.
/// </summary>
public sealed class DeviceRegistry
{
    private readonly Dictionary<string, DeviceDescriptor> _devicesByAlias = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a device descriptor in the registry.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the alias is already registered.</exception>
    public void Register(DeviceDescriptor descriptor)
    {
        if (_devicesByAlias.ContainsKey(descriptor.DeviceAlias))
            throw new ArgumentException($"Device alias '{descriptor.DeviceAlias}' is already registered.", nameof(descriptor));

        _devicesByAlias[descriptor.DeviceAlias] = descriptor;
    }

    /// <summary>
    /// Attempts to retrieve a device descriptor by alias.
    /// </summary>
    public bool TryGetDevice(string deviceAlias, out DeviceDescriptor? descriptor)
        => _devicesByAlias.TryGetValue(deviceAlias, out descriptor);

    /// <summary>
    /// Retrieves all registered device descriptors.
    /// </summary>
    public IReadOnlyCollection<DeviceDescriptor> GetAllDevices()
        => _devicesByAlias.Values;

    /// <summary>
    /// Clears all registered devices.
    /// </summary>
    public void Clear()
        => _devicesByAlias.Clear();
}
