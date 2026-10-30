namespace Helix.Orchestration.Core.Execution;

/// <summary>
/// Represents a wait step that blocks until a condition becomes true.
/// Useful for waiting on trend stability, capability completion, or external events.
/// </summary>
public sealed class WaitFor : WorkflowStep
{
    /// <summary>
    /// The condition to wait for.
    /// </summary>
    public required Condition Condition { get; init; }

    /// <summary>
    /// Polling interval for checking the condition.
    /// </summary>
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Optional timeout. If the condition is not met within this duration, an exception is thrown.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    public override async Task ExecuteAsync(WorkflowEngine.WorkflowContext context, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = Timeout.HasValue
            ? new CancellationTokenSource(Timeout.Value)
            : new CancellationTokenSource();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            while (!await Condition.EvaluateAsync(context, linkedCts.Token))
            {
                await Task.Delay(PollingInterval, linkedCts.Token);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"WaitFor step '{StepId}' timed out after {Timeout}.");
        }
    }
}
