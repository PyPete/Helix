namespace Helix.Orchestration.Core.Execution;

/// <summary>
/// Represents a single executable step within a workflow.
/// </summary>
public abstract class WorkflowStep
{
    /// <summary>
    /// Unique identifier for this step within the workflow.
    /// </summary>
    public required string StepId { get; init; }

    /// <summary>
    /// Optional human-readable description of what this step does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Retry policy for this step. Null means no retries.
    /// </summary>
    public RetryPolicy? RetryPolicy { get; init; }

    /// <summary>
    /// Executes this step's logic.
    /// </summary>
    /// <param name="context">The workflow execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous step execution.</returns>
    public abstract Task ExecuteAsync(WorkflowEngine.WorkflowContext context, CancellationToken cancellationToken = default);
}
