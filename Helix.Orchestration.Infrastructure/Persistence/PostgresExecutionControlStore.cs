using System.Text.Json;
using Dapper;
using Helix.Orchestration.Core.Execution;

namespace Helix.Orchestration.Infrastructure.Persistence;

/// <summary>PostgreSQL optimistic-concurrency store for the Agent control fence.</summary>
public sealed class PostgresExecutionControlStore(DbConnectionFactory connectionFactory) : IExecutionControlStore
{
    public async Task<ExecutionControlSnapshot> GetAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();
        const string sql = """
            SELECT control_state AS State, revision AS Revision, hold_reasons::text AS HoldReasonsJson,
                   actor AS Actor, evidence AS Evidence, changed_at AS ChangedAtUtc
            FROM execution_controls WHERE execution_id = @executionId;
            """;
        var row = await connection.QuerySingleOrDefaultAsync<Row>(
            new CommandDefinition(sql, new { executionId }, cancellationToken: cancellationToken));
        return row?.ToSnapshot() ?? ExecutionControlSnapshot.Running;
    }

    public async Task<ExecutionControlWriteResult> CompareAndSetAsync(
        Guid executionId,
        long expectedRevision,
        ExecutionControlState state,
        IReadOnlySet<string> holdReasons,
        string actor,
        string evidence,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string create = """
            INSERT INTO execution_controls (execution_id, control_state, revision, hold_reasons, changed_at)
            VALUES (@executionId, 'Running', 0, '[]'::jsonb, TIMESTAMPTZ 'epoch')
            ON CONFLICT (execution_id) DO NOTHING;
            """;
        await connection.ExecuteAsync(new CommandDefinition(create, new { executionId }, transaction, cancellationToken: cancellationToken));

        var reasonsJson = JsonSerializer.Serialize(holdReasons.Order(StringComparer.Ordinal));
        const string update = """
            UPDATE execution_controls
            SET control_state = @state, revision = revision + 1, hold_reasons = CAST(@reasonsJson AS jsonb),
                actor = @actor, evidence = @evidence, changed_at = NOW()
            WHERE execution_id = @executionId AND revision = @expectedRevision
            RETURNING control_state AS State, revision AS Revision, hold_reasons::text AS HoldReasonsJson,
                      actor AS Actor, evidence AS Evidence, changed_at AS ChangedAtUtc;
            """;
        var updated = await connection.QuerySingleOrDefaultAsync<Row>(new CommandDefinition(
            update, new { executionId, expectedRevision, state = state.ToString(), reasonsJson, actor, evidence },
            transaction, cancellationToken: cancellationToken));
        if (updated is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new ExecutionControlWriteResult(false, await GetAsync(executionId, cancellationToken));
        }

        const string append = """
            INSERT INTO execution_control_events (event_id, execution_id, revision, control_state, hold_reasons, actor, evidence, occurred_at)
            VALUES (@eventId, @executionId, @revision, @state, CAST(@reasonsJson AS jsonb), @actor, @evidence, @occurredAtUtc);
            """;
        var snapshot = updated.ToSnapshot();
        await connection.ExecuteAsync(new CommandDefinition(append, new
        {
            eventId = Guid.NewGuid(), executionId, revision = snapshot.Revision, state = snapshot.State.ToString(),
            reasonsJson, actor, evidence, occurredAtUtc = snapshot.ChangedAtUtc
        }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return new ExecutionControlWriteResult(true, snapshot);
    }

    private sealed class Row
    {
        public string State { get; init; } = "Running";
        public long Revision { get; init; }
        public string HoldReasonsJson { get; init; } = "[]";
        public string? Actor { get; init; }
        public string? Evidence { get; init; }
        public DateTimeOffset ChangedAtUtc { get; init; }

        public ExecutionControlSnapshot ToSnapshot() => new(
            Enum.Parse<ExecutionControlState>(State, ignoreCase: false), Revision,
            new HashSet<string>(JsonSerializer.Deserialize<string[]>(HoldReasonsJson) ?? [], StringComparer.Ordinal),
            Actor, Evidence, ChangedAtUtc);
    }
}
