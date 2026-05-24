namespace Helix.Orchestration.Agent.Configuration;

public sealed class DeviceRegistryDocument
{
    public List<DeviceRegistryEntry> Devices { get; set; } = [];
}

public sealed class DeviceRegistryEntry
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string AdapterId { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = [];
}
