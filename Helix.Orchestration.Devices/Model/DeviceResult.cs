namespace Helix.Orchestration.Devices.Model;

public enum DeviceResultState
{
    Success,
    Warning,
    Failure
}

public sealed record DeviceResult
{
    public required DeviceResultState State { get; init; }

    public string Reason { get; init; } = string.Empty;

    public static DeviceResult Ok() => new() { State = DeviceResultState.Success };

    public static DeviceResult Warn(string reason) => new() { State = DeviceResultState.Warning, Reason = reason };

    public static DeviceResult Fail(string reason) => new() { State = DeviceResultState.Failure, Reason = reason };
}
