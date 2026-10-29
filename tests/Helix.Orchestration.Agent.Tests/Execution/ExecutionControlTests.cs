using Helix.Orchestration.Agent.Execution;
using Helix.Orchestration.Core.Execution;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Execution;

public sealed class ExecutionControlTests
{
    [Fact]
    public async Task Holds_are_reason_specific_and_survive_recreated_service()
    {
        var store = new TestExecutionControlStore();
        var executionId = Guid.NewGuid();
        var controls = new ExecutionControlService(store);

        var safety = await controls.RequestControlAsync(executionId, ExecutionControlState.Held, 0, "scientist-a", "safety");
        var lease = await controls.RequestControlAsync(executionId, ExecutionControlState.Held, safety.Snapshot.Revision, "scientist-b", "lease");

        // A new Agent-local service sees the durable snapshot, not default Running.
        var recreated = new ExecutionControlService(store);
        var released = await recreated.ReviewHoldAsync(executionId, "safety", lease.Snapshot.Revision, "scientist-a", "review-42");

        Assert.True(released.Accepted);
        Assert.Equal(ExecutionControlState.Held, released.Snapshot.State);
        Assert.DoesNotContain("safety", released.Snapshot.HoldReasons);
        Assert.Contains("lease", released.Snapshot.HoldReasons);
        var stale = await recreated.RequestControlAsync(executionId, ExecutionControlState.Running, lease.Snapshot.Revision, "scientist-a");
        Assert.False(stale.Accepted);
        Assert.Equal("control.stale_revision", stale.Code);
    }

    [Fact]
    public async Task Abort_is_terminal_and_blocks_dispatch_before_start()
    {
        var store = new TestExecutionControlStore();
        var executionId = Guid.NewGuid();
        var controls = new ExecutionControlService(store);

        var aborted = await controls.RequestControlAsync(executionId, ExecutionControlState.Aborted, 0, "scientist-a", "emergency stop");
        var dispatchStarted = false;
        var fence = await controls.StartDispatchAsync(executionId, _ =>
        {
            dispatchStarted = true;
            return Task.CompletedTask;
        });
        var resumed = await controls.RequestControlAsync(executionId, ExecutionControlState.Running, aborted.Snapshot.Revision, "scientist-a");

        Assert.True(aborted.Accepted);
        Assert.False(fence.Accepted);
        Assert.False(dispatchStarted);
        Assert.Equal("control.aborted", resumed.Code);
    }
}

internal sealed class TestExecutionControlStore : IExecutionControlStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, ExecutionControlSnapshot> _states = [];

    public Task<ExecutionControlSnapshot> GetAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
            return Task.FromResult(_states.TryGetValue(executionId, out var snapshot) ? snapshot : ExecutionControlSnapshot.Running);
    }

    public Task<ExecutionControlWriteResult> CompareAndSetAsync(
        Guid executionId,
        long expectedRevision,
        ExecutionControlState state,
        IReadOnlySet<string> holdReasons,
        string actor,
        string evidence,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var current = _states.TryGetValue(executionId, out var snapshot) ? snapshot : ExecutionControlSnapshot.Running;
            if (current.Revision != expectedRevision)
                return Task.FromResult(new ExecutionControlWriteResult(false, current));

            var next = new ExecutionControlSnapshot(state, current.Revision + 1,
                new HashSet<string>(holdReasons, StringComparer.Ordinal), actor, evidence, DateTimeOffset.UtcNow);
            _states[executionId] = next;
            return Task.FromResult(new ExecutionControlWriteResult(true, next));
        }
    }
}
