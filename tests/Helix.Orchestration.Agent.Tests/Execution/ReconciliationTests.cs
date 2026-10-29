using Helix.Orchestration.Agent.Execution;
using Helix.Orchestration.Core.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Execution;

public sealed class ReconciliationTests
{
    [Fact]
    public async Task Matching_independent_observation_confirms_once_and_releases_only_recovery_hold()
    {
        var commandId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var ledger = new TestCommandLedger(commandId, executionId);
        var store = new TestExecutionControlStore();
        var controls = new ExecutionControlService(store);
        var independentHold = await controls.RequestControlAsync(executionId, ExecutionControlState.Held, 0, "scientist", "scientist-review");
        var observations = new TestObservation(new SimulatorObservation("simulator", "flowRate", commandId, "1.0", "mL/min", 7, DateTimeOffset.UtcNow));
        var worker = new CommandReconciliationWorker(ledger, observations, new ExecutionControlService(store), NullLogger<CommandReconciliationWorker>.Instance);

        await worker.ReconcileAsync();
        await worker.ReconcileAsync();

        Assert.Equal(CommandDispatchState.Confirmed, ledger.Dispatch.State);
        Assert.Equal(1, ledger.Confirmations);
        var snapshot = await controls.GetSnapshotAsync(executionId);
        Assert.Equal(ExecutionControlState.Held, snapshot.State);
        Assert.Contains("scientist-review", snapshot.HoldReasons);
        Assert.DoesNotContain($"reconciliation:{commandId:D}", snapshot.HoldReasons);
        Assert.True(independentHold.Accepted);
    }

    [Fact]
    public async Task Mismatch_or_missing_observation_remains_held_without_replay()
    {
        var commandId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var ledger = new TestCommandLedger(commandId, executionId);
        var store = new TestExecutionControlStore();
        var worker = new CommandReconciliationWorker(ledger,
            new TestObservation(new SimulatorObservation("simulator", "flowRate", Guid.NewGuid(), "1.0", "mL/min", 2, DateTimeOffset.UtcNow)),
            new ExecutionControlService(store), NullLogger<CommandReconciliationWorker>.Instance);

        await worker.ReconcileAsync();

        Assert.Equal(CommandDispatchState.HeldForReconciliation, ledger.Dispatch.State);
        Assert.Equal(0, ledger.Confirmations);
        var snapshot = await new ExecutionControlService(store).GetSnapshotAsync(executionId);
        Assert.Contains($"reconciliation:{commandId:D}", snapshot.HoldReasons);
    }
}

public sealed class DispatchRaceTests
{
    [Fact]
    public async Task CompetingDispatchAndTerminalTransitions_RecordOneInFlightFactAndOneLegalTerminalResult()
    {
        var commandId = Guid.NewGuid();
        var ledger = new TestCommandLedger(commandId, Guid.NewGuid(), startInFlight: false);
        using var callersReady = new CountdownEvent(8);
        var releaseCallers = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var starts = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            callersReady.Signal();
            await releaseCallers.Task;
            return await ledger.BeginDispatchAsync(ledger.Intent, new SimulatorCommandExpectation("flowRate", "1.0", "mL/min"));
        })).ToArray();

        Assert.True(callersReady.Wait(TimeSpan.FromSeconds(10)), "Competing dispatches did not reach the start barrier.");
        releaseCallers.SetResult();
        var dispatches = await Task.WhenAll(starts);

        Assert.Equal(1, ledger.DispatchStarts);
        Assert.All(dispatches, dispatch => Assert.Equal(CommandDispatchState.InFlight, dispatch.State));

        var observation = new SimulatorObservation("simulator", "flowRate", commandId, "1.0", "mL/min", 1, DateTimeOffset.UtcNow);
        await Task.WhenAll(
            Task.Run(() => ledger.ConfirmDispatchAsync(commandId, observation, recovered: false)),
            Task.Run(() => ledger.HoldForReconciliationAsync(commandId, "effect outcome requires review")));

        Assert.Equal(1, ledger.TerminalTransitions);
        Assert.True(ledger.Dispatch.State is CommandDispatchState.Confirmed or CommandDispatchState.HeldForReconciliation);
    }
}

internal sealed class TestObservation(SimulatorObservation? observation) : ISimulatorObservation
{
    public Task<SimulatorObservation?> GetAsync(string deviceId, string stateKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(observation);
}

internal sealed class TestCommandLedger : ICommandLedger
{
    private readonly CommandEvidenceRecord _evidence;
    private readonly Lock _gate = new();
    private readonly bool _startsInFlight;

    public TestCommandLedger(Guid commandId, Guid executionId, bool startInFlight = true)
    {
        var intent = new CommandIntent(commandId, executionId, Guid.NewGuid(), "simulator", "SetFlow", "{\"flowRate\":1.0}",
            "scientist", "Scientist", "session", "simulation-policy-v1", DateTimeOffset.UtcNow);
        _evidence = new CommandEvidenceRecord(intent, new CommandEvidence(Guid.NewGuid(), commandId, 1, CommandAdmissionState.Allowed, [], DateTimeOffset.UtcNow));
        _startsInFlight = startInFlight;
        Dispatch = new CommandDispatchRecord(commandId, executionId, startInFlight ? CommandDispatchState.InFlight : CommandDispatchState.HeldForReconciliation,
            new SimulatorCommandExpectation("flowRate", "1.0", "mL/min"), null, DateTimeOffset.UtcNow, null, null);
    }

    public CommandDispatchRecord Dispatch { get; private set; }
    public CommandIntent Intent => _evidence.Intent;
    public int Confirmations { get; private set; }
    public int DispatchStarts { get; private set; }
    public int TerminalTransitions { get; private set; }

    public Task<CommandEvidence> AppendAdmissionAsync(CommandIntent intent, IReadOnlyList<CommandCheckEvidence> checks, CommandAdmissionState state, CancellationToken cancellationToken = default) =>
        Task.FromResult(_evidence.Evidence);

    public Task<CommandEvidenceRecord?> GetAsync(Guid commandId, CancellationToken cancellationToken = default) =>
        Task.FromResult<CommandEvidenceRecord?>(commandId == _evidence.Intent.CommandId ? _evidence : null);

    public Task<CommandDispatchRecord> BeginDispatchAsync(CommandIntent intent, SimulatorCommandExpectation expectation, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_startsInFlight && DispatchStarts == 0)
            {
                DispatchStarts++;
                Dispatch = new CommandDispatchRecord(intent.CommandId, intent.ExecutionId, CommandDispatchState.InFlight,
                    expectation, null, DateTimeOffset.UtcNow, null, null);
            }
            return Task.FromResult(Dispatch);
        }
    }

    public Task<CommandDispatchRecord?> GetDispatchAsync(Guid commandId, CancellationToken cancellationToken = default) =>
        Task.FromResult<CommandDispatchRecord?>(commandId == Dispatch.CommandId ? Dispatch : null);

    public Task<CommandDispatchRecord> ConfirmDispatchAsync(Guid commandId, SimulatorObservation observation, bool recovered, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (Dispatch.State == CommandDispatchState.InFlight)
            {
                Confirmations++;
                TerminalTransitions++;
                Dispatch = Dispatch with { State = CommandDispatchState.Confirmed, ObservedGeneration = observation.Generation, ResolvedAtUtc = DateTimeOffset.UtcNow };
            }
            return Task.FromResult(Dispatch);
        }
    }

    public Task<CommandDispatchRecord> HoldForReconciliationAsync(Guid commandId, string reason, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (Dispatch.State == CommandDispatchState.InFlight)
            {
                TerminalTransitions++;
                Dispatch = Dispatch with { State = CommandDispatchState.HeldForReconciliation, Resolution = reason, ResolvedAtUtc = DateTimeOffset.UtcNow };
            }
            return Task.FromResult(Dispatch);
        }
    }

    public Task<IReadOnlyList<CommandDispatchRecord>> GetIncompleteDispatchesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommandDispatchRecord>>(Dispatch.State == CommandDispatchState.InFlight ? [Dispatch] : []);
}
