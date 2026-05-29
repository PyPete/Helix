using System.Collections.Concurrent;

namespace Helix.Orchestration.Core.Devices;

/// <summary>
/// In-memory registry of devices known to the local agent.
/// Used by <see cref="Helix.Orchestration.Core.WSL.WslCompiler"/> to validate device bindings.
/// </summary>
public sealed class DeviceRegistry
{
    private readonly ConcurrentDictionary<string, DeviceDescriptor> _byAlias = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers a device descriptor.</summary>
    public void Register(DeviceDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _byAlias[descriptor.Alias] = descriptor;
    }

    /// <summary>Attempts to look up a device by its alias.</summary>
    public bool TryGetDevice(string alias, out DeviceDescriptor? descriptor)
        => _byAlias.TryGetValue(alias, out descriptor);

    /// <summary>All registered device descriptors.</summary>
    public IReadOnlyCollection<DeviceDescriptor> All => (IReadOnlyCollection<DeviceDescriptor>)_byAlias.Values;
}
