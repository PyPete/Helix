namespace Helix.Orchestration.Core.WorkflowEngine;

/// <summary>
/// Defines the contract for a durable, executable workflow.
/// Implementations are registered with the WorkflowRegistry and invoked by the WorkflowExecutor.
/// </summary>
public interface IWorkflow
{
    /// <summary>
    /// Executes the workflow logic.
    /// </summary>
    /// <param name="context">The workflow execution context providing access to capabilities, trends, safety, and state.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown or emergency stop.</param>
    /// <returns>A task representing the asynchronous workflow execution.</returns>
    Task ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken = default);
}
