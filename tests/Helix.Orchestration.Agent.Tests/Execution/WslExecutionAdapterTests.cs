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
    public async Task ExecuteAsync_CompilesAndStartsRunspace()
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

        var runspace = await adapter.ExecuteAsync(document, "holder-a");
        await runspace.WaitAsync();

        Assert.NotNull(manager.Get(runspace.RunspaceId));
        Assert.True(leasing.IsHeldBy("pump-1", "holder-a"));
        Assert.Equal("wf-run", executor.LastWorkflowName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDocumentInvalid_ThrowsWslValidationException()
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

        await Assert.ThrowsAsync<WslValidationException>(() => adapter.ExecuteAsync(invalid, "holder-a"));
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
