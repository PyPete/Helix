using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Core.Runspace;

/// <summary>
/// Creates and tracks runspaces for a host process.
/// </summary>
public interface IRunspaceManager : IAsyncDisposable
{
    /// <summary>
    /// Creates a new runspace for the provided compiled definition.
    /// </summary>
    Task<IRunspace> CreateAsync(RunspaceDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tracked runspace by id, or null when not found.
    /// </summary>
    IRunspace? Get(string runspaceId);

    /// <summary>
    /// Returns all runspaces currently tracked by the manager.
    /// </summary>
    IReadOnlyList<IRunspace> ListActive();

    /// <summary>
    /// Tracks an already-created runspace instance.
    /// </summary>
    void Track(IRunspace runspace);

    /// <summary>
    /// Aborts and removes a runspace from management.
    /// </summary>
    Task AbortAsync(string runspaceId, CancellationToken cancellationToken = default);
}
