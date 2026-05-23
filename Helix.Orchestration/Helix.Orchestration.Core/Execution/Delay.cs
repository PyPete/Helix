namespace Helix.Orchestration.Core.Execution;

/// <summary>
/// Represents a fixed-duration delay step.
/// </summary>
public sealed class Delay : WorkflowStep
{
    /// <summary>
    /// Duration to wait before continuing.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    public override Task ExecuteAsync(WorkflowEngine.WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.Delay(Duration, cancellationToken);
    }
}
