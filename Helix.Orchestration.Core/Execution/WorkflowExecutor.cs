using System.Collections.Concurrent;
using Cleipnir.ResilientFunctions;
using Helix.Orchestration.Core.WorkflowEngine;

namespace Helix.Orchestration.Core.Execution;

/// <summary>
/// Durable workflow executor backed by Cleipnir.ResilientFunctions 4.x.
/// </summary>
public sealed class WorkflowExecutor : IWorkflowExecutor
{
    private readonly WorkflowRegistry _registry;
    private readonly ConcurrentDictionary<string, WorkflowStatus> _statuses = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellations = new();

    public WorkflowExecutor(
        FunctionsRegistry functionsRegistry,
        WorkflowRegistry registry,
        Func<string, WorkflowContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(functionsRegistry);
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        ArgumentNullException.ThrowIfNull(contextFactory);
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(string workflowName, string workflowId, object? input = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowName);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);
        _registry.Get(workflowName);

        // A generic Cleipnir action may replay its delegate after recovery. The
        // historical WorkflowContext exposes ICapabilityInvoker, which is not a
        // command dispatcher and therefore cannot prove a stable effect id or a
        // durable cursor. Reject this retained entry point rather than replaying
        // an external effect. Canonical Agent execution owns the dispatcher and
        // cursor; Core remains free of Agent dependencies.
        _statuses[workflowId] = WorkflowStatus.Failed;
        return Task.FromException(new NotSupportedException(
            "Generic workflow execution is disabled. Execute stable command steps through ICommandDispatcher."));
    }

    /// <inheritdoc/>
    public Task<WorkflowStatus> GetStatusAsync(string workflowId)
        => Task.FromResult(_statuses.GetValueOrDefault(workflowId, WorkflowStatus.Pending));

    /// <inheritdoc/>
    public Task CancelAsync(string workflowId)
    {
        if (_cancellations.TryGetValue(workflowId, out var cts))
            cts.Cancel();

        _statuses[workflowId] = WorkflowStatus.Suspended;
        return Task.CompletedTask;
    }
}
