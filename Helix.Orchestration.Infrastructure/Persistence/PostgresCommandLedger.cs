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
}
