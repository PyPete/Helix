using System.Collections.Concurrent;
using Helix.Orchestration.Core.Execution;

namespace Helix.Orchestration.Agent.Execution;

/// <summary>Durable-dispatch control seam; terminal and hold states block future dispatch.</summary>
public sealed class ExecutionControlService
{
    private readonly ConcurrentDictionary<Guid, ExecutionControlState> _states = new();

    public ExecutionControlState Get(Guid executionId) => _states.GetOrAdd(executionId, ExecutionControlState.Running);

    public ExecutionControlState Set(Guid executionId, ExecutionControlState requested) =>
        _states.AddOrUpdate(executionId, requested, (_, current) =>
            current == ExecutionControlState.Aborted ? ExecutionControlState.Aborted : requested);
}
