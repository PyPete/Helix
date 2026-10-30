using Dapper;

namespace Helix.Orchestration.Infrastructure.Persistence;

public sealed class PostgresExperimentRepository(DbConnectionFactory connectionFactory) : IExperimentRepository
{
    public async Task SaveAsync(ExperimentRecord record, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO experiments (
                runspace_id, experiment_id, campaign_id, definition_id, name, status,
                created_at, started_at, ended_at, fault_message, definition_json
            )
            VALUES (
                @RunspaceId, @ExperimentId, @CampaignId, @DefinitionId, @Name, @Status,
                @CreatedAt, @StartedAt, @EndedAt, @FaultMessage, @DefinitionJson::jsonb
            )
            ON CONFLICT (runspace_id) DO UPDATE SET
                experiment_id = EXCLUDED.experiment_id,
                campaign_id = EXCLUDED.campaign_id,
                definition_id = EXCLUDED.definition_id,
                name = EXCLUDED.name,
                status = EXCLUDED.status,
                started_at = EXCLUDED.started_at,
                ended_at = EXCLUDED.ended_at,
                fault_message = EXCLUDED.fault_message,
                definition_json = EXCLUDED.definition_json;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, record, cancellationToken: ct));
    }

    public async Task<ExperimentRecord?> GetByRunspaceIdAsync(string runspaceId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                runspace_id, experiment_id, campaign_id, definition_id, name, status,
                created_at, started_at, ended_at, fault_message, definition_json
            FROM experiments
            WHERE runspace_id = @runspaceId;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<ExperimentRow>(new CommandDefinition(sql, new { runspaceId }, cancellationToken: ct));
        return row?.ToRecord();
    }

    public async Task<ExperimentRecord?> GetByExperimentIdAsync(string experimentId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                runspace_id, experiment_id, campaign_id, definition_id, name, status,
                created_at, started_at, ended_at, fault_message, definition_json
            FROM experiments
            WHERE experiment_id = @experimentId;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<ExperimentRow>(new CommandDefinition(sql, new { experimentId }, cancellationToken: ct));
        return row?.ToRecord();
    }

    public async Task<IReadOnlyList<ExperimentRecord>> GetByCampaignIdAsync(string campaignId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                runspace_id, experiment_id, campaign_id, definition_id, name, status,
                created_at, started_at, ended_at, fault_message, definition_json
            FROM experiments
            WHERE campaign_id = @campaignId
            ORDER BY created_at;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<ExperimentRow>(new CommandDefinition(sql, new { campaignId }, cancellationToken: ct));
        return rows.Select(r => r.ToRecord()).ToList();
    }

    public async Task<IReadOnlyList<ExperimentRecord>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                runspace_id, experiment_id, campaign_id, definition_id, name, status,
                created_at, started_at, ended_at, fault_message, definition_json
            FROM experiments
            ORDER BY created_at DESC;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<ExperimentRow>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.Select(r => r.ToRecord()).ToList();
    }

    private sealed class ExperimentRow
    {
        public string RunspaceId { get; set; } = "";
        public string ExperimentId { get; set; } = "";
        public string? CampaignId { get; set; }
        public string DefinitionId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? EndedAt { get; set; }
        public string? FaultMessage { get; set; }
        public string DefinitionJson { get; set; } = "";

        public ExperimentRecord ToRecord() => new(
            RunspaceId, ExperimentId, CampaignId, DefinitionId, Name, Status,
            CreatedAt, StartedAt, EndedAt, FaultMessage, DefinitionJson);
    }
}
