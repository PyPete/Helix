using Helix.Orchestration.Core.Experiment;

namespace Helix.Orchestration.Agent.Runspaces;

/// <summary>
/// Captures the execution scope for a runspace instance.
/// </summary>
/// <param name="RunspaceId">Runtime runspace identifier.</param>
/// <param name="ExperimentContext">Experiment context bound to this runspace.</param>
public sealed record RunspaceExecutionContext(
    string RunspaceId,
    IExperimentContext ExperimentContext);
