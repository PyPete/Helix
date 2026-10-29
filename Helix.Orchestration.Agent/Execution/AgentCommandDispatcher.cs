using System.Text.Json;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Safety;
using Helix.Orchestration.Devices.Drivers;

namespace Helix.Orchestration.Agent.Execution;

/// <summary>Agent-owned admission boundary which persists every proposal before any future effect.</summary>
public sealed class AgentCommandDispatcher(
    ICommandLedger ledger,
    CommandAdmissionEvaluator evaluator,
    SimulatorDevice simulator,
    AdmissionContextProvider admissionContexts) : ICommandDispatcher
{
    private readonly Dictionary<Guid, CommandIntent> _intents = [];
    private readonly Lock _gate = new();

    public async Task<CommandEvidence> ProposeAsync(CommandIntent intent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        var context = await admissionContexts.CreateAsync(intent, cancellationToken);
        var result = evaluator.Evaluate(context);
        lock (_gate)
            _intents[intent.CommandId] = intent;
        return await ledger.AppendAdmissionAsync(intent, result.Checks, result.State, cancellationToken);
    }

    public async Task<CommandEvidence> DispatchAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        CommandIntent intent;
        lock (_gate)
        {
            if (!_intents.TryGetValue(commandId, out intent!))
                throw new InvalidOperationException("Unknown command intent.");
        }

        var evidence = await ledger.GetAsync(commandId, cancellationToken)
            ?? throw new InvalidOperationException("Command evidence was not committed.");
        if (evidence.Evidence.State != CommandAdmissionState.Allowed)
            return evidence.Evidence;

        // Proposal feedback is evidence, not an authorization token. Rebuild the
        // complete context immediately before effect so a changed safety, health,
        // lease, resource, budget, or termination condition cannot be raced by a
        // previous passing proposal.
        var freshResult = evaluator.Evaluate(await admissionContexts.CreateAsync(intent, cancellationToken));
        if (freshResult.State != CommandAdmissionState.Allowed)
            return await ledger.AppendAdmissionAsync(intent, freshResult.Checks, freshResult.State, cancellationToken);

        var flowRate = JsonDocument.Parse(intent.CanonicalParameters).RootElement.GetProperty("flowRate").GetDouble();
        await simulator.ApplySetFlowAsync(commandId, flowRate, cancellationToken);
        return evidence.Evidence;
    }

    private static bool IsSupportedSetFlow(CommandIntent intent)
    {
        if (!string.Equals(intent.DeviceId, "simulator", StringComparison.Ordinal) ||
            !string.Equals(intent.Capability, "SetFlow", StringComparison.Ordinal) ||
            !string.Equals(intent.PolicyRevision, "simulation-policy-v1", StringComparison.Ordinal))
            return false;
        try
        {
            var flow = JsonDocument.Parse(intent.CanonicalParameters).RootElement.GetProperty("flowRate").GetDouble();
            return double.IsFinite(flow) && flow >= 0 && flow <= 10 && Math.Abs(flow * 10 - Math.Round(flow * 10)) < 0.000001;
        }
        catch (JsonException) { return false; }
        catch (InvalidOperationException) { return false; }
    }
}
