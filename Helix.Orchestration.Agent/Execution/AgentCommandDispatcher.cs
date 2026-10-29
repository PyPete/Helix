using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Safety;

namespace Helix.Orchestration.Agent.Execution;

/// <summary>Agent-owned admission boundary which persists every proposal before any future effect.</summary>
public sealed class AgentCommandDispatcher(ICommandLedger ledger, CommandAdmissionEvaluator evaluator) : ICommandDispatcher
{
    private readonly Dictionary<Guid, CommandIntent> _intents = [];
    private readonly Lock _gate = new();

    public Task<CommandEvidence> ProposeAsync(CommandIntent intent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        var result = evaluator.Evaluate(new CommandAdmissionContext(new Dictionary<string, CommandCheckStatus>
        {
            ["capability"] = CommandCheckStatus.Passed,
            ["safety"] = CommandCheckStatus.Unknown,
            ["lease"] = CommandCheckStatus.Unknown,
            ["resource"] = CommandCheckStatus.Unknown,
            ["budget"] = CommandCheckStatus.Unknown,
            ["termination"] = CommandCheckStatus.Unknown,
            ["health"] = CommandCheckStatus.Unknown
        }, "initial"));
        lock (_gate)
            _intents[intent.CommandId] = intent;
        return ledger.AppendAdmissionAsync(intent, result.Checks, result.State, cancellationToken);
    }

    public Task<CommandEvidence> DispatchAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        CommandIntent intent;
        lock (_gate)
        {
            if (!_intents.TryGetValue(commandId, out intent!))
                throw new InvalidOperationException("Unknown command intent.");
        }

        // Dispatch is held until fresh simulator, lease, safety and policy context are wired.
        return ProposeAsync(intent, cancellationToken);
    }
}
