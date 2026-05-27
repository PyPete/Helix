// Known gaps in InMemoryTrendProvider (not in scope for Phase 4):
// - GetTrend<T>() throws NotSupportedException
// - GetScalarTrend<T>() throws NotSupportedException
// - GetImageTrend() throws NotSupportedException
// - GetVideoTrend() throws NotSupportedException
// - GetDerivedTrend() throws NotSupportedException
// These are TODO stubs; Phase 8 (Full Test Coverage) will address them.

using Helix.Orchestration.Agent.Runspaces;
using Helix.Orchestration.Agent.Trends;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Integration;

public sealed class E2eWorkflowExecutionTests
{
    [Fact]
    public async Task ExecuteCapabilityStep_CompletesRunspace_AndRecordsTrend()
    {
        var provider = new InMemoryTrendProvider();
        var registry = new CapabilityRegistry();

        registry.Register("SetTemperature", new LambdaCapabilityInvoker(async (name, parameters, ct) =>
        {
            provider.PushScalar("reactor.temperature", 75.0);
            return await Task.FromResult(CapabilityInvocationResult.Succeeded(returnValue: 75.0));
        }));

        var executor = new CapabilityInvokingWorkflowExecutor(registry);
        var definition = new RunspaceDefinition(
            DefinitionId: "e2e-test",
            Name: "e2e-test",
            Nodes: [],
            Edges: [],
            Metadata: new Dictionary<string, object?>());

        await using var runspace = new DurableRunspace(definition, executor);
        await runspace.StartAsync();
        await runspace.WaitAsync();

        Assert.Equal(RunspaceStatus.Completed, runspace.Status);
        Assert.True(provider.TryGetScalar("reactor.temperature", out var temp));
        Assert.Equal(75.0, (double)temp!);
    }

    [Fact]
    public async Task ExecuteCapabilityStep_WhenCapabilityFails_RunspaceStillCompletes()
    {
        var provider = new InMemoryTrendProvider();
        var registry = new CapabilityRegistry();

        registry.Register("SetTemperature", new LambdaCapabilityInvoker((name, parameters, ct) =>
            Task.FromResult(CapabilityInvocationResult.Failed("device offline"))));

        var executor = new CapabilityInvokingWorkflowExecutor(registry);
        var definition = new RunspaceDefinition(
            DefinitionId: "e2e-test-fail",
            Name: "e2e-test-fail",
            Nodes: [],
            Edges: [],
            Metadata: new Dictionary<string, object?>());

        await using var runspace = new DurableRunspace(definition, executor);
        await runspace.StartAsync();
        await runspace.WaitAsync();

        Assert.Equal(RunspaceStatus.Completed, runspace.Status);
        Assert.False(provider.TryGetScalar("reactor.temperature", out _));
    }

    private sealed class LambdaCapabilityInvoker : ICapabilityInvoker
    {
        private readonly Func<string, object?, CancellationToken, Task<CapabilityInvocationResult>> _handler;

        public LambdaCapabilityInvoker(Func<string, object?, CancellationToken, Task<CapabilityInvocationResult>> handler)
        {
            _handler = handler;
        }

        public Task<CapabilityInvocationResult> InvokeAsync(string capabilityName, object? parameters, CancellationToken cancellationToken)
            => _handler(capabilityName, parameters, cancellationToken);

        public Task<bool> IsAvailableAsync(string capabilityName)
            => Task.FromResult(true);
    }

    private sealed class CapabilityInvokingWorkflowExecutor : IWorkflowExecutor
    {
        private readonly CapabilityRegistry _registry;

        public CapabilityInvokingWorkflowExecutor(CapabilityRegistry registry)
        {
            _registry = registry;
        }

        public async Task ExecuteAsync(string workflowName, string workflowId, object? input = null, CancellationToken cancellationToken = default)
        {
            await _registry.InvokeAsync("SetTemperature", null, cancellationToken);
        }

        public Task<WorkflowStatus> GetStatusAsync(string workflowId)
            => Task.FromResult(WorkflowStatus.Completed);

        public Task CancelAsync(string workflowId)
            => Task.CompletedTask;
    }
}