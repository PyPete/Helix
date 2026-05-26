using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace Helix.Orchestration.Infrastructure.Persistence;

public sealed class PostgresCampaignRepository(DbConnectionFactory connectionFactory) : ICampaignRepository
{
    public async Task SaveAsync(CampaignRecord campaign, CancellationToken ct = default)
    {
        const string saveCampaignSql = """
            INSERT INTO campaigns (campaign_id, name, description, status, created_at, started_at, ended_at)
            VALUES (@CampaignId, @Name, @Description, @Status, @CreatedAt, @StartedAt, @EndedAt)
            ON CONFLICT (campaign_id) DO UPDATE SET
                name = EXCLUDED.name,
                description = EXCLUDED.description,
                status = EXCLUDED.status,
                started_at = EXCLUDED.started_at,
                ended_at = EXCLUDED.ended_at;
            """;

        const string deleteIterationsSql = "DELETE FROM campaign_iterations WHERE campaign_id = @campaignId;";

        const string insertIterationSql = """
            INSERT INTO campaign_iterations (
                campaign_id, iteration_index, parameters_json, runspace_id,
                status, message, created_at, started_at, ended_at
            )
            VALUES (
                @CampaignId, @IterationIndex, @ParametersJson::jsonb, @RunspaceId,
                @Status, @Message, @CreatedAt, @StartedAt, @EndedAt
            );
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        await connection.ExecuteAsync(new CommandDefinition(saveCampaignSql, campaign, transaction, cancellationToken: ct));
        await connection.ExecuteAsync(new CommandDefinition(deleteIterationsSql, new { campaignId = campaign.CampaignId }, transaction, cancellationToken: ct));

        foreach (var iteration in campaign.Iterations)
            await connection.ExecuteAsync(new CommandDefinition(insertIterationSql, iteration, transaction, cancellationToken: ct));

        await transaction.CommitAsync(ct);
    }

    public async Task<CampaignRecord?> GetByIdAsync(string campaignId, CancellationToken ct = default)
    {
        const string campaignSql = """
            SELECT
                campaign_id AS CampaignId,
                name AS Name,
                description AS Description,
                status AS Status,
                created_at AS CreatedAt,
                started_at AS StartedAt,
                ended_at AS EndedAt
            FROM campaigns
            WHERE campaign_id = @campaignId;
            """;

        const string iterationsSql = """
            SELECT
                campaign_id AS CampaignId,
                iteration_index AS IterationIndex,
                parameters_json AS ParametersJson,
                runspace_id AS RunspaceId,
                status AS Status,
                message AS Message,
                created_at AS CreatedAt,
                started_at AS StartedAt,
                ended_at AS EndedAt
            FROM campaign_iterations
            WHERE campaign_id = @campaignId
            ORDER BY iteration_index;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);

        var campaignRow = await connection.QuerySingleOrDefaultAsync<CampaignRow>(
            new CommandDefinition(campaignSql, new { campaignId }, cancellationToken: ct));

        if (campaignRow is null)
            return null;

        var iterations = (await connection.QueryAsync<CampaignIterationRecord>(
            new CommandDefinition(iterationsSql, new { campaignId }, cancellationToken: ct))).ToList();

        return new CampaignRecord(
            campaignRow.CampaignId,
            campaignRow.Name,
            campaignRow.Description,
            campaignRow.Status,
            campaignRow.CreatedAt,
            campaignRow.StartedAt,
            campaignRow.EndedAt,
            iterations);
    }

    public async Task<IReadOnlyList<CampaignRecord>> GetAllAsync(CancellationToken ct = default)
    {
        const string campaignSql = """
            SELECT
                campaign_id AS CampaignId,
                name AS Name,
                description AS Description,
                status AS Status,
                created_at AS CreatedAt,
                started_at AS StartedAt,
                ended_at AS EndedAt
            FROM campaigns
            ORDER BY created_at;
            """;

        const string iterationsSql = """
            SELECT
                campaign_id AS CampaignId,
                iteration_index AS IterationIndex,
                parameters_json AS ParametersJson,
                runspace_id AS RunspaceId,
                status AS Status,
                message AS Message,
                created_at AS CreatedAt,
                started_at AS StartedAt,
                ended_at AS EndedAt
            FROM campaign_iterations
            ORDER BY campaign_id, iteration_index;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);

        var campaigns = (await connection.QueryAsync<CampaignRow>(new CommandDefinition(campaignSql, cancellationToken: ct))).ToList();
        var iterations = (await connection.QueryAsync<CampaignIterationRecord>(new CommandDefinition(iterationsSql, cancellationToken: ct))).ToList();

        var byCampaign = iterations.GroupBy(i => i.CampaignId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CampaignIterationRecord>)g.ToList());

        return campaigns
            .Select(c => new CampaignRecord(
                c.CampaignId,
                c.Name,
                c.Description,
                c.Status,
                c.CreatedAt,
                c.StartedAt,
                c.EndedAt,
                byCampaign.TryGetValue(c.CampaignId, out var list) ? list : Array.Empty<CampaignIterationRecord>()))
            .ToList();
    }

    public async Task DeleteAsync(string campaignId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM campaigns WHERE campaign_id = @campaignId;";

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { campaignId }, cancellationToken: ct));
    }

    private sealed record CampaignRow(
        string CampaignId,
        string Name,
        string? Description,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? EndedAt);
}
