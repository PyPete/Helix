using Helix.Orchestration.Core.Experiment;
using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Agent.Experiments;

/// <summary>
/// Snapshot of a running or completed experiment.
/// Immutable record — use <c>with</c> expressions to transition state.
/// </summary>
/// <param name="ExperimentId">Unique identifier generated at start time.</param>
/// <param name="Name">Human-readable experiment name.</param>
/// <param name="Lifecycle">Current lifecycle state.</param>
/// <param name="ActivePhase">Current experiment phase, or null when the experiment has not yet started executing phases.</param>
/// <param name="ActiveRunspaceId">The runspace handling the current phase, or null.</param>
/// <param name="CreatedAt">UTC time the experiment was created.</param>
/// <param name="StartedAt">UTC time the experiment transitioned to Running, or null.</param>
/// <param name="EndedAt">UTC time the experiment reached a terminal state, or null.</param>
public sealed record ExperimentContext(
    string ExperimentId,
    string Name,
    ExperimentLifecycle Lifecycle,
    ExperimentPhase? ActivePhase,
    string? ActiveRunspaceId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt
);

/// <summary>
/// Input data for starting a new experiment.
/// </summary>
/// <param name="Name">Human-readable experiment name.</param>
/// <param name="MainPhaseWorkflowId">Optional workflow definition ID to attach to the main phase.</param>
/// <param name="Metadata">Optional freeform metadata.</param>
public sealed record ExperimentStartRequest(
    string Name,
    string? MainPhaseWorkflowId = null,
    Dictionary<string, object?>? Metadata = null
);

/// <summary>
/// Agent-owned result of compiling a scientist's workflow proposal. Only this
/// server-side record may be used by retained transport compatibility paths.
/// </summary>
public sealed record CanonicalExperimentDefinition(
    RunspaceDefinition Definition,
    string PolicyRevision,
    string DefinitionHash,
    string ActorSid,
    DateTimeOffset AcceptedAtUtc);

/// <summary>
/// Manages the in-process lifecycle of experiments: creation, phase transitions, and terminal states.
/// </summary>
public interface IExperimentManager
{
    /// <summary>
    /// Starts a new experiment and returns its initial context.
    /// </summary>
    Task<ExperimentContext> StartAsync(ExperimentStartRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets the experiment context by ID, or null when not found.
    /// </summary>
    ExperimentContext? Get(string experimentId);

    /// <summary>
    /// Returns all experiments in non-terminal states.
    /// </summary>
    IReadOnlyList<ExperimentContext> ListActive();

    /// <summary>
    /// Returns the currently active experiment, or null when none is running.
    /// </summary>
    ExperimentContext? GetActive();

    /// <summary>
    /// Suspends the active runspace for the experiment.
    /// </summary>
    Task PauseAsync(string experimentId, CancellationToken ct = default);

    /// <summary>
    /// Resumes a suspended experiment.
    /// </summary>
    Task ResumeAsync(string experimentId, CancellationToken ct = default);

    /// <summary>
    /// Marks the experiment as failed and clears the active experiment slot.
    /// </summary>
    Task AbortAsync(string experimentId, CancellationToken ct = default);

    /// <summary>
    /// Marks the experiment as completed and clears the active experiment slot.
    /// </summary>
    Task CompleteAsync(string experimentId, CancellationToken ct = default);

    /// <summary>
    /// Associates a runspace ID with the experiment (called after a runspace is created for a phase).
    /// </summary>
    void SetActiveRunspace(string experimentId, string runspaceId);

    /// <summary>Associates an Agent-compiled definition with an experiment.</summary>
    void SetCanonicalDefinition(string experimentId, CanonicalExperimentDefinition definition);

    /// <summary>Gets the Agent-compiled definition for a canonical experiment reference.</summary>
    bool TryGetCanonicalDefinition(string experimentId, out CanonicalExperimentDefinition? definition);
}
