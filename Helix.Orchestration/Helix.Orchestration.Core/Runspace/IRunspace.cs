using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Core.Runspace;

/// <summary>
/// An isolated execution slot that runs a compiled <see cref="RunspaceDefinition"/>.
/// A Runspace is the Helix-level unit of execution; it may be backed by Cleipnir or
/// any other durable runtime without exposing that detail to callers.
/// </summary>
public interface IRunspace : IAsyncDisposable
{
    /// <summary>Stable identifier for this runspace instance.</summary>
    string RunspaceId { get; }

    /// <summary>The compiled workflow definition assigned to this runspace.</summary>
    RunspaceDefinition Definition { get; }

    /// <summary>Current lifecycle status.</summary>
    RunspaceStatus Status { get; }

    /// <summary>When the runspace was created.</summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>When execution started; null if not yet started.</summary>
    DateTimeOffset? StartedAt { get; }

    /// <summary>When execution ended; null if still running or not started.</summary>
    DateTimeOffset? EndedAt { get; }

    /// <summary>
    /// Last error recorded if the runspace faulted; null otherwise.
    /// </summary>
    Exception? Fault { get; }

    /// <summary>
    /// Starts executing the assigned workflow definition.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses execution at the next safe checkpoint.
    /// </summary>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused runspace.
    /// </summary>
    Task ResumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests graceful abort; transitions to <see cref="RunspaceStatus.Faulted"/> if not cleanly done.
    /// </summary>
    Task AbortAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for the runspace to reach a terminal state (Completed or Faulted).
    /// </summary>
    Task WaitAsync(CancellationToken cancellationToken = default);
}
