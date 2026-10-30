namespace Helix.Orchestration.Core.Execution;

/// <summary>Immutable command proposal accepted only after deterministic admission.</summary>
public sealed record CommandIntent(
    Guid CommandId,
    Guid ExecutionId,
    Guid CorrelationId,
    string DeviceId,
    string Capability,
    string CanonicalParameters,
    string VerifiedActorSid,
    string VerifiedActorDisplayName,
    string SessionReference,
    string PolicyRevision,
    DateTimeOffset OccurredAtUtc);

public enum CommandAdmissionState
{
    Allowed,
    Rejected,
    Held
}

public enum CommandCheckStatus
{
    Passed,
    Failed,
    Unknown
}

/// <summary>Evidence for one deterministic admission check.</summary>
public sealed record CommandCheckEvidence(
    string Check,
    CommandCheckStatus Status,
    string Code,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    string ContextRevision);

/// <summary>Immutable admission evidence; facts are appended, never edited.</summary>
public sealed record CommandEvidence(
    Guid EvidenceId,
    Guid CommandId,
    long Sequence,
    CommandAdmissionState State,
    IReadOnlyList<CommandCheckEvidence> Checks,
    DateTimeOffset OccurredAtUtc);

/// <summary>Immutable evidence joined with the intent that created it.</summary>
public sealed record CommandEvidenceRecord(CommandIntent Intent, CommandEvidence Evidence);

/// <summary>One bounded, canonically ordered page of committed command evidence.</summary>
public sealed record CommandEvidencePage(string? Cursor, string? NextCursor, IReadOnlyList<CommandEvidenceRecord> Records);

/// <summary>Attributable context appended to, rather than changing, an immutable command fact.</summary>
public sealed record CommandReviewNote(
    Guid NoteId,
    Guid OperationId,
    Guid CommandId,
    string VerifiedActorSid,
    string VerifiedActorDisplayName,
    string Text,
    DateTimeOffset OccurredAtUtc);

/// <summary>Approved simulation-only limits required before dispatch.</summary>
public sealed record SimulationPolicy(
    string Revision,
    string ApprovedBySid,
    DateTimeOffset ApprovedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    int MutationBudget,
    TimeSpan ExecutionWallTime);

public enum ExecutionControlState
{
    Running,
    Paused,
    Held,
    Aborted
}

/// <summary>Port for the durable, append-only command ledger.</summary>
public interface ICommandLedger
{
    Task<CommandEvidence> AppendAdmissionAsync(
        CommandIntent intent,
        IReadOnlyList<CommandCheckEvidence> checks,
        CommandAdmissionState state,
        CancellationToken cancellationToken = default);

    Task<CommandEvidenceRecord?> GetAsync(Guid commandId, CancellationToken cancellationToken = default);

    /// <summary>Records that a command crossed the final dispatch-start fence before any device effect.</summary>
    Task<CommandDispatchRecord> BeginDispatchAsync(
        CommandIntent intent,
        SimulatorCommandExpectation expectation,
        CancellationToken cancellationToken = default);

    Task<CommandDispatchRecord?> GetDispatchAsync(Guid commandId, CancellationToken cancellationToken = default);

    /// <summary>Records a durable, observed command outcome. Repeated confirmation is idempotent.</summary>
    Task<CommandDispatchRecord> ConfirmDispatchAsync(
        Guid commandId,
        SimulatorObservation observation,
        bool recovered,
        CancellationToken cancellationToken = default);

    /// <summary>Records that an uncertain in-flight command is held for scientist review, never retried.</summary>
    Task<CommandDispatchRecord> HoldForReconciliationAsync(
        Guid commandId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>Returns only effects whose start was durable but whose outcome was not observed.</summary>
    Task<IReadOnlyList<CommandDispatchRecord>> GetIncompleteDispatchesAsync(CancellationToken cancellationToken = default);

    Task<CommandEvidencePage> GetHistoryAsync(
        Guid? executionId,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken = default) => throw new NotSupportedException("Evidence history is unavailable.");

    Task<CommandReviewNote> AppendReviewNoteAsync(
        Guid operationId,
        Guid commandId,
        string verifiedActorSid,
        string verifiedActorDisplayName,
        string text,
        CancellationToken cancellationToken = default) => throw new NotSupportedException("Evidence annotations are unavailable.");
}

/// <summary>Authoritative Agent-only command-dispatch boundary.</summary>
public interface ICommandDispatcher
{
    Task<CommandEvidence> ProposeAsync(CommandIntent intent, CancellationToken cancellationToken = default);
    Task<CommandEvidence> DispatchAsync(Guid commandId, CancellationToken cancellationToken = default);
}

/// <summary>Read-only durable simulator observation used for reconciliation.</summary>
public interface ISimulatorObservation
{
    Task<SimulatorObservation?> GetAsync(string deviceId, string stateKey, CancellationToken cancellationToken = default);
}

public sealed record SimulatorObservation(
    string DeviceId,
    string StateKey,
    Guid LastAppliedCommandId,
    string CanonicalValue,
    string Unit,
    long Generation,
    DateTimeOffset ObservedAtUtc);

/// <summary>Durable simulator effect sink. Setters persist effects before reporting completion.</summary>
public interface ISimulatorStateStore : ISimulatorObservation
{
    Task<SimulatorObservation> ApplyAsync(
        string deviceId,
        string stateKey,
        Guid commandId,
        string canonicalValue,
        string unit,
        CancellationToken cancellationToken = default);
}

/// <summary>Canonical postcondition expected from a mutable simulator command.</summary>
public sealed record SimulatorCommandExpectation(
    string StateKey,
    string CanonicalValue,
    string Unit);

public enum CommandDispatchState
{
    InFlight,
    Confirmed,
    HeldForReconciliation
}

/// <summary>Durable dispatch lifecycle. InFlight is deliberately uncertain after a process crash.</summary>
public sealed record CommandDispatchRecord(
    Guid CommandId,
    Guid ExecutionId,
    CommandDispatchState State,
    SimulatorCommandExpectation Expectation,
    long? ObservedGeneration,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    string? Resolution);

/// <summary>Durable control snapshot used by both the dispatch fence and recovery worker.</summary>
public sealed record ExecutionControlSnapshot(
    ExecutionControlState State,
    long Revision,
    IReadOnlySet<string> HoldReasons,
    string? Actor,
    string? Evidence,
    DateTimeOffset ChangedAtUtc)
{
    public static ExecutionControlSnapshot Running { get; } = new(
        ExecutionControlState.Running,
        0,
        new HashSet<string>(StringComparer.Ordinal),
        null,
        null,
        DateTimeOffset.UnixEpoch);
}

/// <summary>Persistence port for revision-checked execution controls and their attributable event history.</summary>
public interface IExecutionControlStore
{
    Task<ExecutionControlSnapshot> GetAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task<ExecutionControlWriteResult> CompareAndSetAsync(
        Guid executionId,
        long expectedRevision,
        ExecutionControlState state,
        IReadOnlySet<string> holdReasons,
        string actor,
        string evidence,
        CancellationToken cancellationToken = default);
}

public sealed record ExecutionControlWriteResult(bool Applied, ExecutionControlSnapshot Snapshot);
