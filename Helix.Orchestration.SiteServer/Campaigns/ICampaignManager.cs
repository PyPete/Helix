namespace Helix.Orchestration.SiteServer.Campaigns;

public interface ICampaignManager
{
    Task<Campaign> CreateAsync(
        string name,
        string? description,
        IReadOnlyList<Dictionary<string, object?>> parameterSets,
        CancellationToken ct = default);

    Task<Campaign?> GetByIdAsync(string campaignId, CancellationToken ct = default);

    Task<IReadOnlyList<Campaign>> GetAllAsync(CancellationToken ct = default);

    Task<Campaign> StartAsync(string campaignId, CancellationToken ct = default);

    Task<bool> DeleteAsync(string campaignId, CancellationToken ct = default);
}
