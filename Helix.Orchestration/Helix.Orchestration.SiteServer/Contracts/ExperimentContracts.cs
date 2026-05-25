namespace Helix.Orchestration.SiteServer.Contracts;

/// <summary>
/// Request to submit a new experiment for execution.
/// </summary>
public sealed record SubmitExperimentRequest
{
    /// <summary>
    /// WSL source code for the experiment workflow.
    /// </summary>
    public required string WslSource { get; init; }

    /// <summary>
    /// Optional experiment identifier (generated if not provided).
    /// </summary>
    public string? ExperimentId { get; init; }

    /// <summary>
    /// Optional experiment name or description.
    /// </summary>
    public string? ExperimentName { get; init; }
}

/// <summary>
/// Response containing experiment/runspace status.
/// </summary>
public sealed record ExperimentStatusResponse
{
    public required string RunspaceId { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public string? FaultMessage { get; init; }
}

/// <summary>
/// Device descriptor DTO (Phase 9: copied from IAgentProxy for consistency).
/// </summary>
public sealed record DeviceDescriptorDto
{
    public required string DeviceId { get; init; }
    public required string DeviceName { get; init; }
    public required string DeviceKind { get; init; }
    public required IReadOnlyList<string> Capabilities { get; init; }
}

/// <summary>
/// Trend descriptor DTO for listing available trends.
/// </summary>
public sealed record TrendDescriptorDto
{
    public required string TrendId { get; init; }
    public required string TrendName { get; init; }
    public required string TrendKind { get; init; }
    public string? Units { get; init; }
    public string? ExperimentId { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public required bool IsOnline { get; init; }
    public required DateTimeOffset LastUpdatedAt { get; init; }
}

/// <summary>
/// Response containing historical trend data for a scalar trend.
/// </summary>
public sealed record TrendHistoryResponse<T> where T : struct, IComparable<T>
{
    public required string TrendId { get; init; }
    public required string TrendName { get; init; }
    public required IReadOnlyList<T> Values { get; init; }
    public required int Count { get; init; }
}
