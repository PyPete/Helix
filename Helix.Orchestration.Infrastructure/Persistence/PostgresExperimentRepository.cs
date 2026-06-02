using Dapper;

namespace Helix.Orchestration.Infrastructure.Persistence;

public sealed class PostgresExperimentRepository(DbConnectionFactory connectionFactory) : IExperimentRepository
{
    public async Task SaveAsync(ExperimentRecord record, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO experiments (
                runspace_id, campaign_id, definition_id, name, status,
                created_at, started_at, ended_at, fault_message, definition_json
            )
            VALUES (
                @RunspaceId, @CampaignId, @DefinitionId, @Name, @Status,
                @CreatedAt, @StartedAt, @EndedAt, @FaultMessage, @DefinitionJson::jsonb
            )
            ON CONFLICT (runspace_id) DO UPDATE SET
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
                runspace_id AS RunspaceId,
                campaign_id AS CampaignId,
                definition_id AS DefinitionId,
                name AS Name,
                status AS Status,
                created_at AS CreatedAt,
                started_at AS StartedAt,
                ended_at AS EndedAt,
                fault_message AS FaultMessage,
                definition_json AS DefinitionJson
            FROM experiments
            WHERE runspace_id = @runspaceId;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<ExperimentRecord>(new CommandDefinition(sql, new { runspaceId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ExperimentRecord>> GetByCampaignIdAsync(string campaignId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                runspace_id AS RunspaceId,
                campaign_id AS CampaignId,
                definition_id AS DefinitionId,
                name AS Name,
                status AS Status,
                created_at AS CreatedAt,
                started_at AS StartedAt,
                ended_at AS EndedAt,
                fault_message AS FaultMessage,
                definition_json AS DefinitionJson
            FROM experiments
            WHERE campaign_id = @campaignId
            ORDER BY created_at;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<ExperimentRecord>(new CommandDefinition(sql, new { campaignId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ExperimentRecord>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                runspace_id    AS RunspaceId,
                campaign_id    AS CampaignId,
                definition_id  AS DefinitionId,
                name           AS Name,
                status         AS Status,
                created_at     AS CreatedAt,
                started_at     AS StartedAt,
                ended_at       AS EndedAt,
                fault_message  AS FaultMessage,
                definition_json AS DefinitionJson
            FROM experiments
            ORDER BY created_at DESC;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<ExperimentRecord>(
            new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }
}
