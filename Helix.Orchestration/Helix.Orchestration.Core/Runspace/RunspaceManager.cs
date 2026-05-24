using System.Collections.Concurrent;
using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Core.Runspace;

/// <summary>
/// In-process runspace manager for creating and tracking runspaces.
/// </summary>
public sealed class RunspaceManager : IRunspaceManager
{
    private readonly ConcurrentDictionary<string, IRunspace> _runspaces = new();
    private readonly Func<RunspaceDefinition, CancellationToken, Task<IRunspace>> _factory;

    public RunspaceManager()
        : this((definition, cancellationToken) =>
        {
            IRunspace runspace = new InMemoryRunspace(definition, _ => Task.CompletedTask);
            return Task.FromResult(runspace);
        })
    {
    }

    public RunspaceManager(Func<RunspaceDefinition, CancellationToken, Task<IRunspace>> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <inheritdoc/>
    public async Task<IRunspace> CreateAsync(RunspaceDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var runspace = await _factory(definition, cancellationToken).ConfigureAwait(false);
        if (!_runspaces.TryAdd(runspace.RunspaceId, runspace))
        {
            await runspace.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"Runspace id '{runspace.RunspaceId}' is already tracked.");
        }

        return runspace;
    }

    /// <inheritdoc/>
    public IRunspace? Get(string runspaceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runspaceId);
        return _runspaces.TryGetValue(runspaceId, out var runspace) ? runspace : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<IRunspace> ListActive() => _runspaces.Values.ToList();

    /// <inheritdoc/>
    public void Track(IRunspace runspace)
    {
        ArgumentNullException.ThrowIfNull(runspace);

        if (!_runspaces.TryAdd(runspace.RunspaceId, runspace))
            throw new InvalidOperationException($"Runspace id '{runspace.RunspaceId}' is already tracked.");
    }

    /// <inheritdoc/>
    public async Task AbortAsync(string runspaceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runspaceId);

        if (!_runspaces.TryRemove(runspaceId, out var runspace))
            return;

        try
        {
            await runspace.AbortAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await runspace.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        var runspaces = _runspaces.Keys.ToArray();

        foreach (var runspaceId in runspaces)
            await AbortAsync(runspaceId).ConfigureAwait(false);
    }
}
