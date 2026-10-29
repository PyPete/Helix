using System.Globalization;
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
    ExecutionControlService controls,
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
        CommandIntent? intent;
        lock (_gate)
            _intents.TryGetValue(commandId, out intent);

        var evidence = await ledger.GetAsync(commandId, cancellationToken)
            ?? throw new InvalidOperationException("Command evidence was not committed.");
        intent ??= evidence.Intent;
        if (evidence.Evidence.State != CommandAdmissionState.Allowed)
            return evidence.Evidence;

        // Proposal feedback is evidence, not an authorization token. Rebuild the
        // complete context immediately before the durable dispatch-start fence.
        var freshResult = evaluator.Evaluate(await admissionContexts.CreateAsync(intent, cancellationToken));
        if (freshResult.State != CommandAdmissionState.Allowed)
            throw new InvalidOperationException("Fresh admission rejected dispatch; a new proposal is required.");

        if (!IsMutable(intent.Capability))
        {
            await ReadAsync(intent, cancellationToken);
            return evidence.Evidence;
        }

        var existing = await ledger.GetDispatchAsync(commandId, cancellationToken);
        if (existing is { State: CommandDispatchState.Confirmed })
            return evidence.Evidence;
        if (existing is not null)
            throw new InvalidOperationException("An uncertain dispatch requires reconciliation and cannot be replayed.");

        var expectation = CreateExpectation(intent);
        CommandDispatchRecord? dispatch = null;
        var fence = await controls.StartDispatchAsync(intent.ExecutionId, async ct =>
        {
            dispatch = await ledger.BeginDispatchAsync(intent, expectation, ct);
        }, cancellationToken);
        if (!fence.Accepted)
            throw new InvalidOperationException($"Dispatch is blocked by durable control state {fence.Snapshot.State}.");
        if (dispatch is not { State: CommandDispatchState.InFlight })
            return evidence.Evidence;

        // Cancellation after BeginDispatchAsync is intentionally not translated
        // into a rejected/no-effect result. Recovery observes the device state.
        var observation = await ApplyAsync(intent, cancellationToken)
            ?? throw new InvalidOperationException("Mutable simulator command returned no independent observation.");
        await ledger.ConfirmDispatchAsync(commandId, observation, recovered: false, cancellationToken);
        return evidence.Evidence;
    }

    private async Task<SimulatorObservation?> ApplyAsync(CommandIntent intent, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(intent.CanonicalParameters);
        var parameters = document.RootElement;
        return intent.Capability switch
        {
            "SetFlow" => await simulator.ApplySetFlowAsync(intent.CommandId, parameters.GetProperty("flowRate").GetDouble(), cancellationToken),
            "SetTemperature" => await simulator.ApplySetTemperatureAsync(intent.CommandId, parameters.GetProperty("temperature").GetDouble(), cancellationToken),
            "SetMode" => await simulator.ApplySetModeAsync(intent.CommandId, parameters.GetProperty("mode").GetString() ?? string.Empty, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported simulator capability '{intent.Capability}'.")
        };
    }

    private async Task ReadAsync(CommandIntent intent, CancellationToken cancellationToken)
    {
        var stateKey = intent.Capability switch
        {
            "GetFlow" => "flowRate",
            "GetTemperature" => "temperature",
            "GetMode" => "mode",
            _ => throw new InvalidOperationException($"Unsupported simulator capability '{intent.Capability}'.")
        };
        await simulator.GetObservationAsync(stateKey, cancellationToken);
    }

    private static bool IsMutable(string capability) => capability is "SetFlow" or "SetTemperature" or "SetMode";

    private static SimulatorCommandExpectation CreateExpectation(CommandIntent intent)
    {
        using var document = JsonDocument.Parse(intent.CanonicalParameters);
        var parameters = document.RootElement;
        return intent.Capability switch
        {
            "SetFlow" => new("flowRate", parameters.GetProperty("flowRate").GetDouble().ToString("0.0", CultureInfo.InvariantCulture), "mL/min"),
            "SetTemperature" => new("temperature", parameters.GetProperty("temperature").GetDouble().ToString("0", CultureInfo.InvariantCulture), "°C"),
            "SetMode" => new("mode", parameters.GetProperty("mode").GetString() ?? string.Empty, string.Empty),
            _ => throw new InvalidOperationException($"Unsupported mutable simulator capability '{intent.Capability}'.")
        };
    }
}
