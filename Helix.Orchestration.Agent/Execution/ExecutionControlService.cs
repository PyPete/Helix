using System.Collections.Concurrent;
using Helix.Orchestration.Core.Execution;

namespace Helix.Orchestration.Agent.Execution;

/// <summary>
/// Serializes durable control changes with the final dispatch-start fence. The
/// store is the restart authority; the keyed gates close the local stop race.
/// </summary>
public sealed class ExecutionControlService(IExecutionControlStore store)
{
    private const string RecoveryActor = "helix-agent-recovery";
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gates = new();

    public Task<ExecutionControlSnapshot> GetSnapshotAsync(Guid executionId, CancellationToken cancellationToken = default) =>
        store.GetAsync(executionId, cancellationToken);

    /// <summary>Persists the request before callers interrupt in-flight work.</summary>
    public Task<ControlResult> RequestControlAsync(
        Guid executionId,
        ExecutionControlState requested,
        long expectedRevision,
        string actor,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        MutateAsync(executionId, expectedRevision, actor, reason ?? $"Requested {requested}.", current =>
        {
            if (current.State == ExecutionControlState.Aborted && requested != ExecutionControlState.Aborted)
                return ControlMutation.Terminal;

            var reasons = CopyReasons(current);
            return requested switch
            {
                ExecutionControlState.Held when string.IsNullOrWhiteSpace(reason) => ControlMutation.Invalid("control.hold_reason_required"),
                ExecutionControlState.Held => ControlMutation.Change(ExecutionControlState.Held, reasons.Append(reason!).ToHashSet(StringComparer.Ordinal)),
                ExecutionControlState.Aborted => ControlMutation.Change(ExecutionControlState.Aborted, reasons),
                ExecutionControlState.Paused => ControlMutation.Change(
                    reasons.Count == 0 ? ExecutionControlState.Paused : ExecutionControlState.Held, reasons),
                ExecutionControlState.Running when reasons.Count > 0 => ControlMutation.Invalid("control.hold_requires_review"),
                ExecutionControlState.Running => ControlMutation.Change(ExecutionControlState.Running, reasons),
                _ => ControlMutation.Invalid("control.invalid_state")
            };
        }, cancellationToken);

    /// <summary>Releases exactly one attributable hold with review evidence; notes cannot release authority.</summary>
    public Task<ControlResult> ReviewHoldAsync(
        Guid executionId,
        string reason,
        long expectedRevision,
        string actor,
        string reviewEvidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewEvidence);
        return MutateAsync(executionId, expectedRevision, actor, reviewEvidence, current =>
        {
            if (current.State == ExecutionControlState.Aborted)
                return ControlMutation.Terminal;
            if (!current.HoldReasons.Contains(reason, StringComparer.Ordinal))
                return ControlMutation.Invalid("control.reason_not_held");

            var reasons = CopyReasons(current);
            reasons.Remove(reason);
            var state = reasons.Count == 0
                ? (current.State == ExecutionControlState.Paused ? ExecutionControlState.Paused : ExecutionControlState.Running)
                : ExecutionControlState.Held;
            return ControlMutation.Change(state, reasons);
        }, cancellationToken);
    }

    /// <summary>
    /// Runs the durable dispatch-start write inside the same per-execution gate
    /// as a stop request. Once this succeeds, interruption leaves an uncertain
    /// effect for reconciliation instead of fabricating a no-effect result.
    /// </summary>
    public async Task<ControlResult> StartDispatchAsync(
        Guid executionId,
        Func<CancellationToken, Task> persistDispatchStart,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(persistDispatchStart);
        var gate = _gates.GetOrAdd(executionId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var current = await store.GetAsync(executionId, cancellationToken);
            if (current.State != ExecutionControlState.Running || current.HoldReasons.Count != 0)
                return ControlResult.Blocked(current);

            await persistDispatchStart(cancellationToken);
            return ControlResult.Success(current);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Adds the recovery-specific reason without releasing other scientist or safety holds.</summary>
    public Task<ControlResult> AddReconciliationHoldAsync(
        Guid executionId,
        Guid commandId,
        CancellationToken cancellationToken = default) =>
        UpdateRecoveryReasonAsync(executionId, commandId, add: true, cancellationToken);

    /// <summary>Clears only a matching recovery reason after recovery evidence was durably appended.</summary>
    public Task<ControlResult> ReleaseReconciliationHoldAsync(
        Guid executionId,
        Guid commandId,
        CancellationToken cancellationToken = default) =>
        UpdateRecoveryReasonAsync(executionId, commandId, add: false, cancellationToken);

    private async Task<ControlResult> UpdateRecoveryReasonAsync(
        Guid executionId,
        Guid commandId,
        bool add,
        CancellationToken cancellationToken)
    {
        var reason = RecoveryReason(commandId);
        while (true)
        {
            var current = await GetSnapshotAsync(executionId, cancellationToken);
            if (current.State == ExecutionControlState.Aborted)
                return ControlResult.Terminal(current);

            var reasons = CopyReasons(current);
            var changed = add ? reasons.Add(reason) : reasons.Remove(reason);
            if (!changed)
                return ControlResult.Success(current);

            var state = add
                ? ExecutionControlState.Held
                : reasons.Count == 0
                    ? (current.State == ExecutionControlState.Paused ? ExecutionControlState.Paused : ExecutionControlState.Running)
                    : ExecutionControlState.Held;
            var write = await WriteAsync(executionId, current.Revision, state, reasons, RecoveryActor,
                add ? $"Recovery hold installed for uncertain command {commandId}." : $"Recovery evidence confirmed command {commandId}.",
                cancellationToken);
            if (write.Applied)
                return ControlResult.Success(write.Snapshot);
        }
    }

    private async Task<ControlResult> MutateAsync(
        Guid executionId,
        long expectedRevision,
        string actor,
        string evidence,
        Func<ExecutionControlSnapshot, ControlMutation> mutation,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        var gate = _gates.GetOrAdd(executionId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var current = await store.GetAsync(executionId, cancellationToken);
            if (current.Revision != expectedRevision)
                return ControlResult.Conflict(current);

            var next = mutation(current);
            if (next.Code is not null)
                return next.Code == "control.aborted" ? ControlResult.Terminal(current) : ControlResult.Invalid(next.Code, current);

            var write = await WriteAsync(executionId, current.Revision, next.State!.Value, next.HoldReasons!, actor, evidence, cancellationToken);
            return write.Applied ? ControlResult.Success(write.Snapshot) : ControlResult.Conflict(write.Snapshot);
        }
        finally
        {
            gate.Release();
        }
    }

    private Task<ExecutionControlWriteResult> WriteAsync(
        Guid executionId,
        long expectedRevision,
        ExecutionControlState state,
        IReadOnlySet<string> holdReasons,
        string actor,
        string evidence,
        CancellationToken cancellationToken) =>
        store.CompareAndSetAsync(executionId, expectedRevision, state, holdReasons, actor, evidence, cancellationToken);

    private static HashSet<string> CopyReasons(ExecutionControlSnapshot current) =>
        new(current.HoldReasons, StringComparer.Ordinal);

    private static string RecoveryReason(Guid commandId) => $"reconciliation:{commandId:D}";

    private sealed record ControlMutation(ExecutionControlState? State, IReadOnlySet<string>? HoldReasons, string? Code)
    {
        public static ControlMutation Terminal { get; } = new(null, null, "control.aborted");
        public static ControlMutation Change(ExecutionControlState state, IReadOnlySet<string> reasons) => new(state, reasons, null);
        public static ControlMutation Invalid(string code) => new(null, null, code);
    }
}

public sealed record ControlResult(bool Accepted, string Code, ExecutionControlSnapshot Snapshot)
{
    public static ControlResult Success(ExecutionControlSnapshot snapshot) => new(true, "control.accepted", snapshot);
    public static ControlResult Conflict(ExecutionControlSnapshot snapshot) => new(false, "control.stale_revision", snapshot);
    public static ControlResult Terminal(ExecutionControlSnapshot snapshot) => new(false, "control.aborted", snapshot);
    public static ControlResult Blocked(ExecutionControlSnapshot snapshot) => new(false, "control.dispatch_blocked", snapshot);
    public static ControlResult Invalid(string code, ExecutionControlSnapshot snapshot) => new(false, code, snapshot);
}
