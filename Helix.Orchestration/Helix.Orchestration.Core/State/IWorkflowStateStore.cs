namespace Helix.Orchestration.Core.State;

/// <summary>
/// Defines the contract for workflow state persistence.
/// Implementations wrap Cleipnir's IFunctionStore or provide custom storage.
/// </summary>
public interface IWorkflowStateStore
{
    /// <summary>
    /// Saves or updates workflow state.
    /// </summary>
    /// <param name="state">The workflow state to persist.</param>
    Task SaveAsync(WorkflowState state);

    /// <summary>
    /// Retrieves workflow state by instance ID.
    /// </summary>
    /// <param name="workflowId">The workflow instance identifier.</param>
    /// <returns>The workflow state if found, otherwise null.</returns>
    Task<WorkflowState?> GetAsync(string workflowId);

    /// <summary>
    /// Deletes workflow state.
    /// </summary>
    /// <param name="workflowId">The workflow instance identifier.</param>
    Task DeleteAsync(string workflowId);

    /// <summary>
    /// Lists all workflow states matching optional filters.
    /// </summary>
    /// <param name="workflowName">Optional filter by workflow name.</param>
    /// <param name="status">Optional filter by status.</param>
    /// <returns>Collection of matching workflow states.</returns>
    Task<IEnumerable<WorkflowState>> ListAsync(string? workflowName = null, string? status = null);
}
