using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Helix.Orchestration.Core.Execution;

namespace Helix.Orchestration.Infrastructure.Persistence;

/// <summary>PostgreSQL implementation of the immutable command-admission ledger.</summary>
public sealed class PostgresCommandLedger(DbConnectionFactory connectionFactory) : ICommandLedger
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<CommandEvidence> AppendAdmissionAsync(
        CommandIntent intent,
        IReadOnlyList<CommandCheckEvidence> checks,
        CommandAdmissionState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(checks);

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // A command ID is an idempotency key.  Take a transaction-scoped lock
        // before looking it up so concurrent callers either observe the same
        // committed fact or create it exactly once.  This is deliberately
        // before the stream lock: all callers use command identity then stream
        // ordering, avoiding an inverted lock order.
        const string lockCommandIdentity = """
            SELECT pg_advisory_xact_lock(hashtextextended(@CommandId::text, 0));
            """;
        await connection.ExecuteAsync(new CommandDefinition(
            lockCommandIdentity,
            new { intent.CommandId },
            transaction,
            cancellationToken: cancellationToken));

        const string createStream = """
            INSERT INTO command_streams (execution_id) VALUES (@ExecutionId)
            ON CONFLICT (execution_id) DO NOTHING;
            """;
        await connection.ExecuteAsync(new CommandDefinition(createStream, intent, transaction, cancellationToken: cancellationToken));

        const string existingEvidence = """
            SELECT e.evidence_id, e.sequence_num, e.admission_state, e.checks_json, e.occurred_at,
                   i.execution_id, i.correlation_id, i.device_id, i.capability,
                   i.canonical_parameters::text AS canonical_parameters, i.verified_actor_sid,
                   i.verified_actor_display_name, i.session_reference, i.policy_revision
            FROM command_evidence e
            JOIN command_intents i ON i.command_id = e.command_id
            WHERE e.command_id = @CommandId;
            """;
        var existing = await connection.QuerySingleOrDefaultAsync<ExistingEvidenceRow>(
            new CommandDefinition(existingEvidence, new { intent.CommandId }, transaction, cancellationToken: cancellationToken));
        if (existing is not null)
        {
            if (!existing.Matches(intent))
                throw new InvalidOperationException("Command identity was reused with different immutable intent content.");

            await transaction.CommitAsync(cancellationToken);
            return existing.ToEvidence(intent.CommandId);
        }

        const string insertIntent = """
            INSERT INTO command_intents (
                command_id, execution_id, correlation_id, device_id, capability, canonical_parameters,
                verified_actor_sid, verified_actor_display_name, session_reference, policy_revision, occurred_at)
            VALUES (
                @CommandId, @ExecutionId, @CorrelationId, @DeviceId, @Capability, CAST(@CanonicalParameters AS jsonb),
                @VerifiedActorSid, @VerifiedActorDisplayName, @SessionReference, @PolicyRevision, @OccurredAtUtc);
            """;
        await connection.ExecuteAsync(new CommandDefinition(insertIntent, intent, transaction, cancellationToken: cancellationToken));

        const string allocateSequence = """
            UPDATE command_streams SET next_sequence = next_sequence + 1
            WHERE execution_id = @ExecutionId
            RETURNING next_sequence;
            """;
        var sequence = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(allocateSequence, new { intent.ExecutionId }, transaction, cancellationToken: cancellationToken));

        var evidence = new CommandEvidence(Guid.NewGuid(), intent.CommandId, sequence, state, checks, DateTimeOffset.UtcNow);
        const string appendEvidence = """
            INSERT INTO command_evidence (
                evidence_id, command_id, execution_id, sequence_num, admission_state, checks_json, occurred_at)
            VALUES (@EvidenceId, @CommandId, @ExecutionId, @Sequence, @AdmissionState, CAST(@ChecksJson AS jsonb), @OccurredAtUtc);
            """;
        await connection.ExecuteAsync(new CommandDefinition(appendEvidence, new
        {
            evidence.EvidenceId,
            evidence.CommandId,
            intent.ExecutionId,
            evidence.Sequence,
            AdmissionState = evidence.State.ToString(),
            ChecksJson = JsonSerializer.Serialize(evidence.Checks, JsonOptions),
            evidence.OccurredAtUtc
        }, transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return evidence;
    }

    public async Task<CommandEvidenceRecord?> GetAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();
        const string sql = """
            SELECT i.command_id AS CommandId, i.execution_id AS ExecutionId, i.correlation_id AS CorrelationId,
                   i.device_id AS DeviceId, i.capability AS Capability, i.canonical_parameters::text AS CanonicalParameters,
                   i.verified_actor_sid AS VerifiedActorSid, i.verified_actor_display_name AS VerifiedActorDisplayName,
                   i.session_reference AS SessionReference, i.policy_revision AS PolicyRevision, i.occurred_at AS IntentOccurredAtUtc,
                   e.evidence_id AS EvidenceId, e.sequence_num AS SequenceNum, e.admission_state AS AdmissionState,
                   e.checks_json::text AS ChecksJson, e.occurred_at AS EvidenceOccurredAtUtc
            FROM command_intents i JOIN command_evidence e ON e.command_id = i.command_id
            WHERE i.command_id = @commandId;
            """;
        var row = await connection.QuerySingleOrDefaultAsync<JoinedEvidenceRow>(
            new CommandDefinition(sql, new { commandId }, cancellationToken: cancellationToken));
        return row?.ToRecord();
    }

    public async Task<CommandDispatchRecord> BeginDispatchAsync(
        CommandIntent intent,
        SimulatorCommandExpectation expectation,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();
        const string insert = """
            INSERT INTO command_dispatches (
                command_id, execution_id, dispatch_state, state_key, canonical_value, unit, started_at)
            VALUES (@CommandId, @ExecutionId, 'InFlight', @StateKey, @CanonicalValue, @Unit, NOW())
            ON CONFLICT (command_id) DO NOTHING;
            """;
        await connection.ExecuteAsync(new CommandDefinition(insert, new
        {
            intent.CommandId, intent.ExecutionId, expectation.StateKey, expectation.CanonicalValue, expectation.Unit
        }, cancellationToken: cancellationToken));
        return await GetDispatchAsync(intent.CommandId, cancellationToken)
            ?? throw new InvalidOperationException("Dispatch start was not durably recorded.");
    }

    public async Task<CommandDispatchRecord?> GetDispatchAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();
        const string sql = """
            SELECT command_id AS CommandId, execution_id AS ExecutionId, dispatch_state AS State,
                   state_key AS StateKey, canonical_value AS CanonicalValue, unit AS Unit,
                   observed_generation AS ObservedGeneration, started_at AS StartedAtUtc,
                   resolved_at AS ResolvedAtUtc, resolution AS Resolution
            FROM command_dispatches WHERE command_id = @commandId;
            """;
        var row = await connection.QuerySingleOrDefaultAsync<DispatchRow>(
            new CommandDefinition(sql, new { commandId }, cancellationToken: cancellationToken));
        return row?.ToRecord();
    }

    public async Task<CommandDispatchRecord> ConfirmDispatchAsync(
        Guid commandId,
        SimulatorObservation observation,
        bool recovered,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();
        const string update = """
            UPDATE command_dispatches
            SET dispatch_state = 'Confirmed', observed_generation = @generation, resolved_at = NOW(),
                resolution = @resolution
            WHERE command_id = @commandId AND dispatch_state <> 'Confirmed';
            """;
        await connection.ExecuteAsync(new CommandDefinition(update, new
        {
            commandId, generation = observation.Generation,
            resolution = recovered ? "Recovered from independently observed simulator effect." : "Effect confirmed by dispatcher."
        }, cancellationToken: cancellationToken));
        return await GetDispatchAsync(commandId, cancellationToken)
            ?? throw new InvalidOperationException("Dispatch confirmation was not durably recorded.");
    }

    public async Task<CommandDispatchRecord> HoldForReconciliationAsync(
        Guid commandId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();
        const string update = """
            UPDATE command_dispatches
            SET dispatch_state = 'HeldForReconciliation', resolved_at = NOW(), resolution = @reason
            WHERE command_id = @commandId AND dispatch_state = 'InFlight';
            """;
        await connection.ExecuteAsync(new CommandDefinition(update, new { commandId, reason }, cancellationToken: cancellationToken));
        return await GetDispatchAsync(commandId, cancellationToken)
            ?? throw new InvalidOperationException("Reconciliation hold was not durably recorded.");
    }

    public async Task<IReadOnlyList<CommandDispatchRecord>> GetIncompleteDispatchesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();
        const string sql = """
            SELECT command_id AS CommandId, execution_id AS ExecutionId, dispatch_state AS State,
                   state_key AS StateKey, canonical_value AS CanonicalValue, unit AS Unit,
                   observed_generation AS ObservedGeneration, started_at AS StartedAtUtc,
                   resolved_at AS ResolvedAtUtc, resolution AS Resolution
            FROM command_dispatches WHERE dispatch_state = 'InFlight' ORDER BY started_at, command_id;
            """;
        var rows = await connection.QueryAsync<DispatchRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.Select(row => row.ToRecord()).ToArray();
    }

    private class EvidenceRow
    {
        public Guid EvidenceId { get; init; }
        public long SequenceNum { get; init; }
        public string AdmissionState { get; init; } = string.Empty;
        public string ChecksJson { get; init; } = "[]";
        public DateTimeOffset OccurredAt { get; init; }

        public CommandEvidence ToEvidence(Guid commandId) => new(
            EvidenceId,
            commandId,
            SequenceNum,
            Enum.Parse<CommandAdmissionState>(AdmissionState, ignoreCase: false),
            JsonSerializer.Deserialize<List<CommandCheckEvidence>>(ChecksJson, JsonOptions) ?? [],
            OccurredAt);
    }

    private sealed class ExistingEvidenceRow : EvidenceRow
    {
        public Guid ExecutionId { get; init; }
        public Guid CorrelationId { get; init; }
        public string DeviceId { get; init; } = string.Empty;
        public string Capability { get; init; } = string.Empty;
        public string CanonicalParameters { get; init; } = "{}";
        public string VerifiedActorSid { get; init; } = string.Empty;
        public string VerifiedActorDisplayName { get; init; } = string.Empty;
        public string SessionReference { get; init; } = string.Empty;
        public string PolicyRevision { get; init; } = string.Empty;

        public bool Matches(CommandIntent intent) =>
            ExecutionId == intent.ExecutionId &&
            CorrelationId == intent.CorrelationId &&
            string.Equals(DeviceId, intent.DeviceId, StringComparison.Ordinal) &&
            string.Equals(Capability, intent.Capability, StringComparison.Ordinal) &&
            string.Equals(CanonicalParameters, intent.CanonicalParameters, StringComparison.Ordinal) &&
            string.Equals(VerifiedActorSid, intent.VerifiedActorSid, StringComparison.Ordinal) &&
            string.Equals(VerifiedActorDisplayName, intent.VerifiedActorDisplayName, StringComparison.Ordinal) &&
            string.Equals(SessionReference, intent.SessionReference, StringComparison.Ordinal) &&
            string.Equals(PolicyRevision, intent.PolicyRevision, StringComparison.Ordinal);
    }

    private sealed class DispatchRow
    {
        public Guid CommandId { get; init; }
        public Guid ExecutionId { get; init; }
        public string State { get; init; } = string.Empty;
        public string StateKey { get; init; } = string.Empty;
        public string CanonicalValue { get; init; } = string.Empty;
        public string Unit { get; init; } = string.Empty;
        public long? ObservedGeneration { get; init; }
        public DateTimeOffset StartedAtUtc { get; init; }
        public DateTimeOffset? ResolvedAtUtc { get; init; }
        public string? Resolution { get; init; }

        public CommandDispatchRecord ToRecord() => new(
            CommandId,
            ExecutionId,
            Enum.Parse<CommandDispatchState>(State, ignoreCase: false),
            new SimulatorCommandExpectation(StateKey, CanonicalValue, Unit),
            ObservedGeneration,
            StartedAtUtc,
            ResolvedAtUtc,
            Resolution);
    }

    private sealed class JoinedEvidenceRow
    {
        public Guid CommandId { get; init; }
        public Guid ExecutionId { get; init; }
        public Guid CorrelationId { get; init; }
        public string DeviceId { get; init; } = string.Empty;
        public string Capability { get; init; } = string.Empty;
        public string CanonicalParameters { get; init; } = "{}";
        public string VerifiedActorSid { get; init; } = string.Empty;
        public string VerifiedActorDisplayName { get; init; } = string.Empty;
        public string SessionReference { get; init; } = string.Empty;
        public string PolicyRevision { get; init; } = string.Empty;
        public DateTimeOffset IntentOccurredAtUtc { get; init; }
        public Guid EvidenceId { get; init; }
        public long SequenceNum { get; init; }
        public string AdmissionState { get; init; } = string.Empty;
        public string ChecksJson { get; init; } = "[]";
        public DateTimeOffset EvidenceOccurredAtUtc { get; init; }

        public CommandEvidenceRecord ToRecord() => new(
            new CommandIntent(CommandId, ExecutionId, CorrelationId, DeviceId, Capability, CanonicalParameters,
                VerifiedActorSid, VerifiedActorDisplayName, SessionReference, PolicyRevision, IntentOccurredAtUtc),
            new CommandEvidence(EvidenceId, CommandId, SequenceNum,
                Enum.Parse<CommandAdmissionState>(AdmissionState, ignoreCase: false),
                JsonSerializer.Deserialize<List<CommandCheckEvidence>>(ChecksJson, JsonOptions) ?? [], EvidenceOccurredAtUtc));
    }
}
