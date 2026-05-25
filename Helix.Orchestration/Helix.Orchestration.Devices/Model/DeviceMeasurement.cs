namespace Helix.Orchestration.Devices.Model;

public sealed record DeviceMeasurement(
    string TrendName,
    string DefaultTrendName,
    object Result,
    string Unit,
    DateTime Timestamp,
    double? ReactionTime = null,
    string? XName = null,
    string? XUnit = null
);
