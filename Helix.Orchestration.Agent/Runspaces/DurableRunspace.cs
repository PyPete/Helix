using Helix.Orchestration.Agent.Telemetry;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Agent.Runspaces;

/// <summary>
/// Durable <see cref="IRunspace"/> implementation that delegates execution to <see cref="IWorkflowExecutor"/>.
/// </summary>
public sealed class DurableRunspace : IRunspace
{
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc/>
    public string RunspaceId { get; } = Guid.NewGuid().ToString("N");

    /// <inheritdoc/>
    public RunspaceDefinition Definition { get; }

    /// <inheritdoc/>
    public RunspaceStatus Status { get; private set; } = RunspaceStatus.Idle;

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <inheritdoc/>
    public Exception? Fault { get; private set; }

    public DurableRunspace(RunspaceDefinition definition, IWorkflowExecutor workflowExecutor)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _workflowExecutor = workflowExecutor ?? throw new ArgumentNullException(nameof(workflowExecutor));
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Status is not RunspaceStatus.Idle and not RunspaceStatus.Paused)
                throw new InvalidOperationException($"Cannot start runspace in state {Status}.");

            // Cleipnir may replay an action delegate. This legacy runspace has no
            // persisted command cursor or command-dispatch port, so running it
            // would make an external effect replayable. Do not turn a display
            // runspace into an alternative execution authority.
            throw new NotSupportedException(
                "Legacy DurableRunspace execution is disabled. Use canonical dispatch with a persisted cursor.");
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Status != RunspaceStatus.Running)
                return;

            await _workflowExecutor.CancelAsync(RunspaceId);
            Status = RunspaceStatus.Paused;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        // Resume must never mean graph-from-start. This implementation has no
        // durable cursor, so it intentionally cannot resume.
        return Task.FromException(new NotSupportedException(
            "Legacy DurableRunspace cannot resume without a persisted command cursor."));
    }

    /// <inheritdoc/>
    public async Task AbortAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Status is RunspaceStatus.Completed or RunspaceStatus.Faulted or RunspaceStatus.Disposed)
                return;

            await _workflowExecutor.CancelAsync(RunspaceId);
            Fault = new OperationCanceledException($"Runspace '{RunspaceId}' aborted.");
            Status = RunspaceStatus.Faulted;
            EndedAt = DateTimeOffset.UtcNow;
            HelixTelemetry.RunspaceFaults.Add(1);
            _completion.TrySetResult();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public Task WaitAsync(CancellationToken cancellationToken = default)
        => _completion.Task.WaitAsync(cancellationToken);

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Status is RunspaceStatus.Running or RunspaceStatus.Paused)
            await AbortAsync();

        _gate.Dispose();
        Status = RunspaceStatus.Disposed;
    }
}
