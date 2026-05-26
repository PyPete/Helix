namespace Helix.Orchestration.SiteServer.Campaigns;

public enum CampaignStatus
{
    Draft,
    Running,
    Completed,
    Failed,
    Aborted
}

public sealed class Campaign
{
    public string CampaignId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public List<CampaignIteration> Iterations { get; } = new();
}
