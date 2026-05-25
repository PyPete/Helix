using System.Collections.Concurrent;
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.SiteServer.Agent;

namespace Helix.Orchestration.SiteServer.Campaigns;

public sealed class CampaignManager(
    IAgentProxy agentProxy,
    ICampaignIterationStrategy iterationStrategy) : ICampaignManager
{
    private readonly ConcurrentDictionary<string, Campaign> _campaigns = new();

    public Task<Campaign> CreateAsync(
        string name,
        string? description,
        IReadOnlyList<Dictionary<string, object?>> parameterSets,
        CancellationToken ct = default)
    {
        var plans = iterationStrategy.BuildIterations(parameterSets);
        var campaignId = Guid.NewGuid().ToString("N");

        var campaign = new Campaign
        {
            CampaignId = campaignId,
            Name = name,
            Description = description,
            Status = CampaignStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var plan in plans)
        {
            campaign.Iterations.Add(new CampaignIteration(
                Index: plan.Index,
                Parameters: plan.Parameters,
                RunspaceId: null,
                Status: CampaignIterationStatus.Pending,
                Message: null,
                CreatedAt: DateTimeOffset.UtcNow,
                StartedAt: null,
                EndedAt: null));
        }

        _campaigns[campaignId] = campaign;
        return Task.FromResult(campaign);
    }

    public Task<Campaign?> GetByIdAsync(string campaignId, CancellationToken ct = default)
    {
        _campaigns.TryGetValue(campaignId, out var campaign);
        return Task.FromResult(campaign);
    }

    public Task<IReadOnlyList<Campaign>> GetAllAsync(CancellationToken ct = default)
    {
        var list = _campaigns.Values.OrderBy(c => c.CreatedAt).ToList();
        return Task.FromResult<IReadOnlyList<Campaign>>(list);
    }

    public async Task<Campaign> StartAsync(string campaignId, CancellationToken ct = default)
    {
        if (!_campaigns.TryGetValue(campaignId, out var campaign))
            throw new KeyNotFoundException($"Campaign '{campaignId}' not found.");

        if (campaign.Status is CampaignStatus.Running)
            return campaign;

        if (campaign.Status is CampaignStatus.Completed)
            return campaign;

        campaign.Status = CampaignStatus.Running;
        campaign.StartedAt = DateTimeOffset.UtcNow;

        for (var i = 0; i < campaign.Iterations.Count; i++)
        {
            var current = campaign.Iterations[i];
            var startedAt = DateTimeOffset.UtcNow;

            campaign.Iterations[i] = current with
            {
                Status = CampaignIterationStatus.Running,
                StartedAt = startedAt,
                Message = null
            };

            try
            {
                var definition = new RunspaceDefinition(
                    DefinitionId: $"{campaign.CampaignId}-iter-{current.Index}",
                    Name: $"{campaign.Name} Iteration {current.Index}",
                    Nodes: Array.Empty<WslNode>(),
                    Edges: Array.Empty<WslEdge>(),
                    Metadata: new Dictionary<string, object?>(current.Parameters));

                var runspaceId = await agentProxy.SubmitExperimentAsync(definition, ct);

                campaign.Iterations[i] = campaign.Iterations[i] with
                {
                    RunspaceId = runspaceId,
                    Status = CampaignIterationStatus.Completed,
                    Message = "Submitted",
                    EndedAt = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                campaign.Iterations[i] = campaign.Iterations[i] with
                {
                    Status = CampaignIterationStatus.Failed,
                    Message = ex.Message,
                    EndedAt = DateTimeOffset.UtcNow
                };

                campaign.Status = CampaignStatus.Failed;
                campaign.EndedAt = DateTimeOffset.UtcNow;
                return campaign;
            }
        }

        campaign.Status = CampaignStatus.Completed;
        campaign.EndedAt = DateTimeOffset.UtcNow;
        return campaign;
    }

    public Task<bool> DeleteAsync(string campaignId, CancellationToken ct = default)
        => Task.FromResult(_campaigns.TryRemove(campaignId, out _));
}
