namespace Helix.Orchestration.Core.Experiment;

/// <summary>
/// Defines the contract for experiment context in experiment-centric workflows.
/// Tracks experiment metadata, lifecycle state, and data alignment rules.
/// </summary>
public interface IExperimentContext
{
    /// <summary>
    /// Unique identifier for the experiment.
    /// </summary>
    string ExperimentId { get; }

    /// <summary>
    /// Human-readable experiment name or title.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Current lifecycle state of the experiment.
    /// </summary>
    ExperimentLifecycle Lifecycle { get; }

    /// <summary>
    /// Metadata key-value pairs associated with the experiment (e.g., project, researcher, conditions).
    /// </summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Timestamp when the experiment was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Timestamp when the experiment was started (transitioned to Running state).
    /// </summary>
    DateTimeOffset? StartedAt { get; }

    /// <summary>
    /// Timestamp when the experiment was completed or failed.
    /// </summary>
    DateTimeOffset? CompletedAt { get; }

    /// <summary>
    /// Transitions the experiment to a new lifecycle state.
    /// </summary>
    Task TransitionToAsync(ExperimentLifecycle newState);

    /// <summary>
    /// Adds or updates metadata for the experiment.
    /// </summary>
    Task SetMetadataAsync(string key, object? value);

    /// <summary>
    /// Records a data point or measurement associated with this experiment.
    /// Used for data alignment and traceability.
    /// </summary>
    Task RecordDataAsync(string dataType, object data);
}
