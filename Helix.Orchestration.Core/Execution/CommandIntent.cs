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
    Task<SimulatorObservation?> GetAsync(string deviceId, CancellationToken cancellationToken = default);
}

public sealed record SimulatorObservation(
    string DeviceId,
    Guid LastAppliedCommandId,
    string CanonicalValue,
    string Unit,
    long Generation,
    DateTimeOffset ObservedAtUtc);
