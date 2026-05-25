namespace Helix.Orchestration.SiteServer.Campaigns;

public interface ICampaignIterationStrategy
{
    IReadOnlyList<CampaignIterationPlan> BuildIterations(IReadOnlyList<Dictionary<string, object?>> parameterSets);
}

public sealed record CampaignIterationPlan(
    int Index,
    IReadOnlyDictionary<string, object?> Parameters
);
