namespace Helix.Orchestration.Core.Experiment;

/// <summary>
/// Lifecycle states for experiment-centric workflows.
/// </summary>
public enum ExperimentLifecycle
{
    /// <summary>
    /// Experiment has been created but not yet started.
    /// </summary>
    Created,

    /// <summary>
    /// Experiment is actively running.
    /// </summary>
    Running,

    /// <summary>
    /// Experiment has been temporarily suspended and can be resumed.
    /// </summary>
    Suspended,

    /// <summary>
    /// Experiment has completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Experiment has failed and cannot continue.
    /// </summary>
    Failed
}
