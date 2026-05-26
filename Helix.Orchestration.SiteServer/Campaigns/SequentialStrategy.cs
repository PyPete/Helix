namespace Helix.Orchestration.SiteServer.Campaigns;

public sealed class SequentialStrategy : ICampaignIterationStrategy
{
    public IReadOnlyList<CampaignIterationPlan> BuildIterations(IReadOnlyList<Dictionary<string, object?>> parameterSets)
    {
        var list = new List<CampaignIterationPlan>(parameterSets.Count);

        for (var i = 0; i < parameterSets.Count; i++)
        {
            list.Add(new CampaignIterationPlan(
                Index: i,
                Parameters: new Dictionary<string, object?>(parameterSets[i])));
        }

        return list;
    }
}
