using Helix.Orchestration.Agent.Runspaces;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Runspaces;

public sealed class DurableRunspaceTests
{
    [Fact]
    public async Task StartAsync_DelegatesToWorkflowExecutor_AndCompletes()
    {
        var executor = new StubWorkflowExecutor();
        var definition = CreateDefinition("wf-start");
        await using var runspace = new DurableRunspace(definition, executor);

        await runspace.StartAsync();
        await runspace.WaitAsync();

        Assert.Equal(RunspaceStatus.Completed, runspace.Status);
        Assert.Equal("wf-start", executor.LastWorkflowName);
        Assert.Equal(runspace.RunspaceId, executor.LastWorkflowId);
    }

    [Fact]
    public async Task PauseAsync_CancelsCurrentExecution_AndSetsPausedStatus()
    {
        var executor = new BlockingWorkflowExecutor();
        var definition = CreateDefinition("wf-pause");
        await using var runspace = new DurableRunspace(definition, executor);

        await runspace.StartAsync();
        await executor.ExecutionStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        await runspace.PauseAsync();

        Assert.Equal(RunspaceStatus.Paused, runspace.Status);
        Assert.True(executor.CancelCalled);
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
