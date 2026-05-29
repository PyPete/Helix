namespace Helix.Orchestration.Core.Devices;

/// <summary>
/// Describes a device registered with the local agent.
/// </summary>
/// <param name="DeviceId">Stable unique identifier for the physical device.</param>
/// <param name="Alias">Short alias used in WSL node parameters to reference this device.</param>
/// <param name="DeviceType">Category/protocol type (e.g. "reactor", "pump", "hplc").</param>
/// <param name="Capabilities">Capability names exposed by this device.</param>
/// <param name="Metadata">Freeform key-value metadata (location, serial number, etc.).</param>
public sealed record DeviceDescriptor(
    string DeviceId,
    string Alias,
    string DeviceType,
    IReadOnlyList<string> Capabilities,
    IReadOnlyDictionary<string, object?> Metadata);
