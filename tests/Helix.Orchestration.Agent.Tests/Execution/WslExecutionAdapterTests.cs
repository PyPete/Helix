using Helix.Orchestration.Agent.Execution;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Leasing;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Execution;

public sealed class WslExecutionAdapterTests
{
    [Fact]
    public async Task ExecuteAsync_RejectsBeforeLeaseOrExecutorSideEffects()
    {
        var leasing = new InMemoryDeviceLeasingService();
        await using var manager = new RunspaceManager();
        var executor = new StubWorkflowExecutor();
        var adapter = new WslExecutionAdapter(leasing, manager, executor);

        var document = new WslDocument
        {
            Name = "wf-run",
            Nodes =
            [
                new WslNode("node-1", "Pump", "Capability", new Dictionary<string, object?>
                {
                    ["deviceId"] = "pump-1"
                })
            ],
            Edges = []
        };

        await Assert.ThrowsAsync<NotSupportedException>(() => adapter.ExecuteAsync(document, "holder-a"));

        Assert.Empty(manager.ListActive());
        Assert.False(leasing.IsHeldBy("pump-1", "holder-a"));
        Assert.Null(executor.LastWorkflowName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDocumentInvalid_StillRejectsBeforeCompilation()
    {
        var leasing = new InMemoryDeviceLeasingService();
        await using var manager = new RunspaceManager();
        var executor = new StubWorkflowExecutor();
        var adapter = new WslExecutionAdapter(leasing, manager, executor);

        var invalid = new WslDocument
        {
            Name = string.Empty,
            Nodes = [],
            Edges = []
        };

        await Assert.ThrowsAsync<NotSupportedException>(() => adapter.ExecuteAsync(invalid, "holder-a"));
        Assert.Empty(manager.ListActive());
    }

    private sealed class StubWorkflowExecutor : IWorkflowExecutor
    {
        public string? LastWorkflowName { get; private set; }

        public Task ExecuteAsync(string workflowName, string workflowId, object? input = null, CancellationToken cancellationToken = default)
        {
            LastWorkflowName = workflowName;
            return Task.CompletedTask;
        }

        public Task<WorkflowStatus> GetStatusAsync(string workflowId) => Task.FromResult(WorkflowStatus.Completed);

        public Task CancelAsync(string workflowId) => Task.CompletedTask;
    }
}
