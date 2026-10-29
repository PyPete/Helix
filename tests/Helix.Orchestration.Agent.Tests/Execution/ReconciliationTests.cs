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

internal sealed class TestObservation(SimulatorObservation? observation) : ISimulatorObservation
{
    public Task<SimulatorObservation?> GetAsync(string deviceId, string stateKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(observation);
}

internal sealed class TestCommandLedger : ICommandLedger
{
    private readonly CommandEvidenceRecord _evidence;

    public TestCommandLedger(Guid commandId, Guid executionId)
    {
        var intent = new CommandIntent(commandId, executionId, Guid.NewGuid(), "simulator", "SetFlow", "{\"flowRate\":1.0}",
            "scientist", "Scientist", "session", "simulation-policy-v1", DateTimeOffset.UtcNow);
        _evidence = new CommandEvidenceRecord(intent, new CommandEvidence(Guid.NewGuid(), commandId, 1, CommandAdmissionState.Allowed, [], DateTimeOffset.UtcNow));
        Dispatch = new CommandDispatchRecord(commandId, executionId, CommandDispatchState.InFlight,
            new SimulatorCommandExpectation("flowRate", "1.0", "mL/min"), null, DateTimeOffset.UtcNow, null, null);
    }

    public CommandDispatchRecord Dispatch { get; private set; }
    public int Confirmations { get; private set; }

    public Task<CommandEvidence> AppendAdmissionAsync(CommandIntent intent, IReadOnlyList<CommandCheckEvidence> checks, CommandAdmissionState state, CancellationToken cancellationToken = default) =>
        Task.FromResult(_evidence.Evidence);

    public Task<CommandEvidenceRecord?> GetAsync(Guid commandId, CancellationToken cancellationToken = default) =>
        Task.FromResult<CommandEvidenceRecord?>(commandId == _evidence.Intent.CommandId ? _evidence : null);

    public Task<CommandDispatchRecord> BeginDispatchAsync(CommandIntent intent, SimulatorCommandExpectation expectation, CancellationToken cancellationToken = default) =>
        Task.FromResult(Dispatch);

    public Task<CommandDispatchRecord?> GetDispatchAsync(Guid commandId, CancellationToken cancellationToken = default) =>
        Task.FromResult<CommandDispatchRecord?>(commandId == Dispatch.CommandId ? Dispatch : null);

    public Task<CommandDispatchRecord> ConfirmDispatchAsync(Guid commandId, SimulatorObservation observation, bool recovered, CancellationToken cancellationToken = default)
    {
        if (Dispatch.State != CommandDispatchState.Confirmed)
        {
            Confirmations++;
            Dispatch = Dispatch with { State = CommandDispatchState.Confirmed, ObservedGeneration = observation.Generation, ResolvedAtUtc = DateTimeOffset.UtcNow };
        }
        return Task.FromResult(Dispatch);
    }

    public Task<CommandDispatchRecord> HoldForReconciliationAsync(Guid commandId, string reason, CancellationToken cancellationToken = default)
    {
        if (Dispatch.State == CommandDispatchState.InFlight)
            Dispatch = Dispatch with { State = CommandDispatchState.HeldForReconciliation, Resolution = reason, ResolvedAtUtc = DateTimeOffset.UtcNow };
        return Task.FromResult(Dispatch);
    }

    public Task<IReadOnlyList<CommandDispatchRecord>> GetIncompleteDispatchesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommandDispatchRecord>>(Dispatch.State == CommandDispatchState.InFlight ? [Dispatch] : []);
}
