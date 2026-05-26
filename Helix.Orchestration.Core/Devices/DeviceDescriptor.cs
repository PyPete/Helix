namespace Helix.Orchestration.Core.Devices;

/// <summary>
/// Describes a logical device available in the device registry.
/// </summary>
/// <param name="DeviceId">Unique device identifier.</param>
/// <param name="DeviceAlias">Human-readable alias used in WSL documents.</param>
/// <param name="DeviceType">Device type/category (e.g., "pump", "reactor", "hplc").</param>
/// <param name="CapabilityNames">Named capabilities exposed by this device.</param>
/// <param name="Metadata">Additional device metadata.</param>
public sealed record DeviceDescriptor(
    string DeviceId,
    string DeviceAlias,
    string DeviceType,
    IReadOnlyList<string> CapabilityNames,
    IReadOnlyDictionary<string, object?> Metadata);
