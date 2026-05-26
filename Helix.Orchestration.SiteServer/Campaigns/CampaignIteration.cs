namespace Helix.Orchestration.SiteServer.Campaigns;

public enum CampaignIterationStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Aborted
}

public sealed record CampaignIteration(
    int Index,
    IReadOnlyDictionary<string, object?> Parameters,
    string? RunspaceId,
    CampaignIterationStatus Status,
    string? Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt
);
