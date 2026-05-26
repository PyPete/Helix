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

    private CancellationTokenSource? _executionCts;
    private Task? _executionTask;

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

            _executionCts?.Dispose();
            _executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Status = RunspaceStatus.Running;
            StartedAt ??= DateTimeOffset.UtcNow;

            _executionTask = ExecuteAsync(_executionCts.Token);
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
            _executionCts?.Cancel();
            Status = RunspaceStatus.Paused;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public Task ResumeAsync(CancellationToken cancellationToken = default)
        => StartAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AbortAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Status is RunspaceStatus.Completed or RunspaceStatus.Faulted or RunspaceStatus.Disposed)
                return;

            await _workflowExecutor.CancelAsync(RunspaceId);
            _executionCts?.Cancel();
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

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = HelixTelemetry.AgentActivitySource.StartActivity("runspace.execute");
        activity?.SetTag("helix.runspace.id", RunspaceId);
        activity?.SetTag("helix.workflow.name", Definition.Name);

        try
        {
            await _workflowExecutor.ExecuteAsync(Definition.Name, RunspaceId, cancellationToken: cancellationToken);

            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (Status == RunspaceStatus.Running)
                {
                    Status = RunspaceStatus.Completed;
                    EndedAt = DateTimeOffset.UtcNow;
                    HelixTelemetry.RunspaceCompletions.Add(1);
                    _completion.TrySetResult();
                }
            }
            finally
            {
                _gate.Release();
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await _gate.WaitAsync();
            try
            {
                if (Status == RunspaceStatus.Running)
                    Status = RunspaceStatus.Paused;
            }
            finally
            {
                _gate.Release();
            }
        }
        catch (OperationCanceledException)
        {
            await _gate.WaitAsync();
            try
            {
                if (Status == RunspaceStatus.Running)
                    Status = RunspaceStatus.Paused;
            }
            finally
            {
                _gate.Release();
            }
        }
        catch (Exception ex)
        {
            await _gate.WaitAsync();
            try
            {
                Fault = ex;
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
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Status is RunspaceStatus.Running or RunspaceStatus.Paused)
            await AbortAsync();

        _executionCts?.Dispose();
        _gate.Dispose();
        Status = RunspaceStatus.Disposed;
    }
}
