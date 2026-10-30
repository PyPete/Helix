namespace Helix.Orchestration.Core.Execution;

/// <summary>
/// Defines the contract for executing workflows with durable execution semantics.
/// Implementations wrap Cleipnir.ResilientFunctions to provide crash-safe, resumable workflow execution.
/// </summary>
public interface IWorkflowExecutor
{
    /// <summary>
    /// Starts or resumes a workflow execution.
    /// </summary>
    /// <param name="workflowName">The registered name of the workflow to execute.</param>
    /// <param name="workflowId">Unique identifier for this execution instance.</param>
    /// <param name="input">Optional input parameters for the workflow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous workflow execution.</returns>
    Task ExecuteAsync(string workflowName, string workflowId, object? input = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the status of a running workflow.
    /// </summary>
    /// <param name="workflowId">The workflow instance identifier.</param>
    /// <returns>The current status of the workflow (Pending, Running, Completed, Failed, Suspended).</returns>
    Task<WorkflowStatus> GetStatusAsync(string workflowId);

    /// <summary>
    /// Cancels a running workflow.
    /// </summary>
    /// <param name="workflowId">The workflow instance identifier.</param>
    Task CancelAsync(string workflowId);
}

/// <summary>
/// Workflow execution status.
/// </summary>
public enum WorkflowStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Suspended
}
