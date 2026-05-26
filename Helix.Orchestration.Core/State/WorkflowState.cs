namespace Helix.Orchestration.Core.State;

/// <summary>
/// Represents the durable state of a workflow instance.
/// This is serialized and persisted by Cleipnir across crashes and restarts.
/// </summary>
public sealed record WorkflowState
{
    /// <summary>
    /// Workflow instance identifier.
    /// </summary>
    public required string WorkflowId { get; init; }

    /// <summary>
    /// Name of the workflow definition being executed.
    /// </summary>
    public required string WorkflowName { get; init; }

    /// <summary>
    /// Timestamp when the workflow started.
    /// </summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp of the last state update.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Current execution status.
    /// </summary>
    public string Status { get; init; } = "Pending";

    /// <summary>
    /// User-defined variables and step state stored as key-value pairs.
    /// Serialized as JSON by Cleipnir's state store.
    /// </summary>
    public Dictionary<string, object?> Variables { get; init; } = new();

    /// <summary>
    /// Execution logs and step results.
    /// </summary>
    public List<string> Logs { get; init; } = new();

    /// <summary>
    /// Optional experiment context reference (experiment ID).
    /// </summary>
    public string? ExperimentId { get; init; }
}
