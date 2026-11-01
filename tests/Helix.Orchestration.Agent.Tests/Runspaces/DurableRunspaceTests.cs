using Helix.Orchestration.Agent.Runspaces;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Runspaces;

public sealed class DurableRunspaceTests
{
    [Fact]
    public async Task StartAsync_RejectsLegacyExecutionWithoutDispatchingWorkflow()
    {
        var executor = new StubWorkflowExecutor();
        var definition = CreateDefinition("wf-start");
        await using var runspace = new DurableRunspace(definition, executor);

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => runspace.StartAsync());

        Assert.Equal("Legacy DurableRunspace execution is disabled. Use canonical dispatch with a persisted cursor.", exception.Message);
        Assert.Equal(RunspaceStatus.Idle, runspace.Status);
        Assert.Null(executor.LastWorkflowName);
        Assert.Null(executor.LastWorkflowId);
    }

    [Fact]
    public async Task StartAsync_RejectsLegacyExecutionWithoutRequestingCancellation()
    {
        var executor = new BlockingWorkflowExecutor();
        var definition = CreateDefinition("wf-pause");
        await using var runspace = new DurableRunspace(definition, executor);

        await Assert.ThrowsAsync<NotSupportedException>(() => runspace.StartAsync());

        Assert.Equal(RunspaceStatus.Idle, runspace.Status);
        Assert.False(executor.CancelCalled);
    }

    private static RunspaceDefinition CreateDefinition(string name) => new(
        DefinitionId: name,
        Name: name,
        Nodes: [],
        Edges: [],
        Metadata: new Dictionary<string, object?>());

    private sealed class StubWorkflowExecutor : IWorkflowExecutor
    {
        public string? LastWorkflowName { get; private set; }
        public string? LastWorkflowId { get; private set; }

        public Task ExecuteAsync(string workflowName, string workflowId, object? input = null, CancellationToken cancellationToken = default)
        {
            LastWorkflowName = workflowName;
            LastWorkflowId = workflowId;
            return Task.CompletedTask;
        }

        public Task<WorkflowStatus> GetStatusAsync(string workflowId) => Task.FromResult(WorkflowStatus.Completed);

        public Task CancelAsync(string workflowId) => Task.CompletedTask;
    }

    private sealed class BlockingWorkflowExecutor : IWorkflowExecutor
    {
        public TaskCompletionSource ExecutionStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool CancelCalled { get; private set; }

        public async Task ExecuteAsync(string workflowName, string workflowId, object? input = null, CancellationToken cancellationToken = default)
        {
            ExecutionStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        public Task<WorkflowStatus> GetStatusAsync(string workflowId) => Task.FromResult(WorkflowStatus.Running);

        public Task CancelAsync(string workflowId)
        {
            CancelCalled = true;
            return Task.CompletedTask;
        }
    }
}
