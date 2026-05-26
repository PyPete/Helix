namespace Helix.Orchestration.Infrastructure.Persistence;

public interface ICampaignRepository
{
    Task SaveAsync(CampaignRecord campaign, CancellationToken ct = default);

    Task<CampaignRecord?> GetByIdAsync(string campaignId, CancellationToken ct = default);

    Task<IReadOnlyList<CampaignRecord>> GetAllAsync(CancellationToken ct = default);

    Task DeleteAsync(string campaignId, CancellationToken ct = default);
}

public sealed record CampaignRecord(
    string CampaignId,
    string Name,
    string? Description,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    IReadOnlyList<CampaignIterationRecord> Iterations
);

public sealed record CampaignIterationRecord(
    string CampaignId,
    int IterationIndex,
    string ParametersJson,
    string? RunspaceId,
    string Status,
    string? Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt
);
