using System.Collections.Concurrent;
using Cleipnir.ResilientFunctions;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Helix.Orchestration.Core.WorkflowEngine;

namespace Helix.Orchestration.Core.Execution;

/// <summary>
/// Durable workflow executor backed by Cleipnir.ResilientFunctions 4.x.
/// </summary>
public sealed class WorkflowExecutor : IWorkflowExecutor
{
    private readonly FunctionsRegistry _functionsRegistry;
    private readonly WorkflowRegistry _registry;
    private readonly Func<string, WorkflowContext> _contextFactory;
    private readonly ConcurrentDictionary<string, ActionRegistration<string>> _registrations = new();
    private readonly ConcurrentDictionary<string, WorkflowStatus> _statuses = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellations = new();

    public WorkflowExecutor(
        FunctionsRegistry functionsRegistry,
        WorkflowRegistry registry,
        Func<string, WorkflowContext> contextFactory)
    {
        _functionsRegistry = functionsRegistry ?? throw new ArgumentNullException(nameof(functionsRegistry));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(string workflowName, string workflowId, object? input = null, CancellationToken cancellationToken = default)
    {
        _registry.Get(workflowName);

        var registration = _registrations.GetOrAdd(
            workflowName,
            name => _functionsRegistry.RegisterAction(
                flowType: name,
                inner: async (string instanceId, Workflow _) =>
                {
                    var workflow = _registry.Get(name);
                    var context = _contextFactory(instanceId);
                    var token = _cancellations.TryGetValue(instanceId, out var cts)
                        ? cts.Token
                        : CancellationToken.None;

                    await workflow.ExecuteAsync(context, token);
                })
        );

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _statuses[workflowId] = WorkflowStatus.Running;
        _cancellations[workflowId] = linkedCts;

        try
        {
            await registration.Invoke(flowInstance: workflowId, param: workflowId);
            _statuses[workflowId] = WorkflowStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            _statuses[workflowId] = WorkflowStatus.Suspended;
            throw;
        }
        catch
        {
            _statuses[workflowId] = WorkflowStatus.Failed;
            throw;
        }
        finally
        {
            if (_cancellations.TryRemove(workflowId, out var cts))
                cts.Dispose();
        }
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
