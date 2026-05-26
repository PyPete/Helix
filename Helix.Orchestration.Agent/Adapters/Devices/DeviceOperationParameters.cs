namespace Helix.Orchestration.Agent.Adapters.Devices;

public sealed record DeviceOperationParameters
{
    public string Operation { get; init; } = string.Empty;

    public Dictionary<string, object?> Arguments { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
