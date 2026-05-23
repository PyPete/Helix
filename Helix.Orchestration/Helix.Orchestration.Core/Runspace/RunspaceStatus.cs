namespace Helix.Orchestration.Core.Runspace;

/// <summary>
/// Lifecycle states of a Helix execution slot (Runspace).
/// </summary>
public enum RunspaceStatus
{
    /// <summary>Allocated but not yet started.</summary>
    Idle,

    /// <summary>Actively executing a workflow.</summary>
    Running,

    /// <summary>Execution paused, e.g. awaiting operator input.</summary>
    Paused,

    /// <summary>Workflow completed successfully.</summary>
    Completed,

    /// <summary>Workflow faulted or was aborted.</summary>
    Faulted,

    /// <summary>Runspace has been disposed and is no longer usable.</summary>
    Disposed
}
