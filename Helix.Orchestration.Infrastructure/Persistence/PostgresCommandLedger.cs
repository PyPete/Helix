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

        const string createStream = """
            INSERT INTO command_streams (execution_id) VALUES (@ExecutionId)
            ON CONFLICT (execution_id) DO NOTHING;
            """;
        await connection.ExecuteAsync(new CommandDefinition(createStream, intent, transaction, cancellationToken: cancellationToken));

        const string existingEvidence = """
            SELECT evidence_id, sequence_num, admission_state, checks_json, occurred_at
            FROM command_evidence WHERE command_id = @CommandId;
            """;
        var existing = await connection.QuerySingleOrDefaultAsync<EvidenceRow>(
            new CommandDefinition(existingEvidence, new { intent.CommandId }, transaction, cancellationToken: cancellationToken));
        if (existing is not null)
        {
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

    private sealed class EvidenceRow
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
