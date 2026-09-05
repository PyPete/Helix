using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Core.Runspace;

/// <summary>
/// In-memory, single-process implementation of <see cref="IRunspace"/>.
/// Intended for unit tests and local development without a durable runtime.
/// The workflow body is provided as a delegate at construction time.
/// </summary>
public sealed class InMemoryRunspace : IRunspace
{
    private readonly Func<CancellationToken, Task> _body;
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenSource? _cts;
    private SemaphoreSlim? _pauseGate;

    /// <inheritdoc/>
    public string RunspaceId { get; }

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

    public InMemoryRunspace(RunspaceDefinition definition, Func<CancellationToken, Task> body)
    {
        RunspaceId = Guid.NewGuid().ToString("N");
        Definition = definition;
        _body = body;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Status != RunspaceStatus.Idle)
            throw new InvalidOperationException($"Cannot start a runspace in state {Status}.");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Status = RunspaceStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;

        _ = RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            await _body(token);
            Status = RunspaceStatus.Completed;
        }
        catch (Exception ex)
        {
            Fault = ex;
            Status = RunspaceStatus.Faulted;
        }
        finally
        {
            EndedAt = DateTimeOffset.UtcNow;
            _completion.TrySetResult();
        }
    }

    /// <summary>
    /// Cooperative pause checkpoint — awaited at the start of each BFS iteration.
    /// Call this inside the body loop before each unit of work.
    /// </summary>
    public async Task CheckPauseAsync(CancellationToken token)
    {
        if (_pauseGate is SemaphoreSlim gate)
            await gate.WaitAsync(token);
    }

    /// <inheritdoc/>
    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        if (Status == RunspaceStatus.Running)
        {
            Status = RunspaceStatus.Paused;
            _pauseGate = new SemaphoreSlim(0, 1);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        if (Status == RunspaceStatus.Paused)
        {
            Status = RunspaceStatus.Running;
            _pauseGate?.Release();
            _pauseGate = null;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task AbortAsync(CancellationToken cancellationToken = default)
    {
        // Release the pause gate first so the BFS loop can observe cancellation.
        _pauseGate?.Release();
        _pauseGate = null;

        // If the runspace was never started there is no background task to await;
        // resolve the completion immediately so WaitAsync does not block forever.
        if (Status == RunspaceStatus.Idle)
        {
            Status = RunspaceStatus.Faulted;
            EndedAt = DateTimeOffset.UtcNow;
            Fault = new OperationCanceledException("Runspace aborted before start.");
            _completion.TrySetResult();
            return;
        }

        if (_cts is not null)
            await _cts.CancelAsync();
        await WaitAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task WaitAsync(CancellationToken cancellationToken = default) =>
        _completion.Task.WaitAsync(cancellationToken);

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Status == RunspaceStatus.Running || Status == RunspaceStatus.Paused)
            await AbortAsync();

        _cts?.Dispose();
        _pauseGate?.Dispose();
        Status = RunspaceStatus.Disposed;
    }
}
