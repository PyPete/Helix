namespace Helix.Orchestration.Core.Experiment;

/// <summary>
/// Identifies the execution phase of an experiment at the time an event was recorded.
/// </summary>
public enum ExperimentPhase
{
    /// <summary>
    /// Steps executing before the main measurement phase.
    /// </summary>
    Pre,

    /// <summary>
    /// Core measurement / data-collection steps.
    /// </summary>
    Main,

    /// <summary>
    /// Steps executing after the main measurement phase (teardown, cleanup).
    /// </summary>
    Post
}
