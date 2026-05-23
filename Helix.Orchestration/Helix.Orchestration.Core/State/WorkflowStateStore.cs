using System.Collections.Concurrent;

namespace Helix.Orchestration.Core.State;

/// <summary>
/// In-memory implementation of IWorkflowStateStore.
/// For production, use Cleipnir's IFunctionStore with PostgreSQL, SQL Server, or other persistent backends.
/// This implementation is suitable for development and testing.
/// </summary>
public sealed class WorkflowStateStore : IWorkflowStateStore
{
    private readonly ConcurrentDictionary<string, WorkflowState> _states = new();

    /// <inheritdoc/>
    public Task SaveAsync(WorkflowState state)
    {
        var updatedState = state with { LastUpdatedAt = DateTimeOffset.UtcNow };
        _states[state.WorkflowId] = updatedState;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<WorkflowState?> GetAsync(string workflowId)
    {
        _states.TryGetValue(workflowId, out var state);
        return Task.FromResult(state);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string workflowId)
    {
        _states.TryRemove(workflowId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<WorkflowState>> ListAsync(string? workflowName = null, string? status = null)
    {
        var query = _states.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(workflowName))
        {
            query = query.Where(s => s.WorkflowName.Equals(workflowName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(query);
    }
}
