using System.Collections.Concurrent;
using System.Text.Json;
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.Infrastructure.Persistence;
using Helix.Orchestration.SiteServer.Agent;

namespace Helix.Orchestration.SiteServer.Campaigns;

public sealed class CampaignManager(
    IAgentProxy agentProxy,
    ICampaignIterationStrategy iterationStrategy,
    ICampaignRepository repository) : ICampaignManager
{
    private readonly ConcurrentDictionary<string, Campaign> _campaigns = new();

    public async Task<Campaign> CreateAsync(
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
        await repository.SaveAsync(ToCampaignRecord(campaign), ct);
        return campaign;
    }

    public async Task<Campaign?> GetByIdAsync(string campaignId, CancellationToken ct = default)
    {
        if (_campaigns.TryGetValue(campaignId, out var cached))
            return cached;

        var record = await repository.GetByIdAsync(campaignId, ct);
        if (record is null) return null;
        var campaign = FromCampaignRecord(record);
        _campaigns[campaignId] = campaign;
        return campaign;
    }

    public async Task<IReadOnlyList<Campaign>> GetAllAsync(CancellationToken ct = default)
    {
        var records = await repository.GetAllAsync(ct);
        foreach (var rec in records)
            _campaigns.TryAdd(rec.CampaignId, FromCampaignRecord(rec));
        return _campaigns.Values.OrderBy(c => c.CreatedAt).ToList();
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
                await repository.SaveAsync(ToCampaignRecord(campaign), ct);
                return campaign;
            }
        }

        campaign.Status = CampaignStatus.Completed;
        campaign.EndedAt = DateTimeOffset.UtcNow;
        await repository.SaveAsync(ToCampaignRecord(campaign), ct);
        return campaign;
    }

    public async Task<bool> DeleteAsync(string campaignId, CancellationToken ct = default)
    {
        var removed = _campaigns.TryRemove(campaignId, out _);
        await repository.DeleteAsync(campaignId, ct);
        return removed;
    }

    private static CampaignRecord ToCampaignRecord(Campaign campaign) =>
        new(
            CampaignId: campaign.CampaignId,
            Name: campaign.Name,
            Description: campaign.Description,
            Status: campaign.Status.ToString(),
            CreatedAt: campaign.CreatedAt,
            StartedAt: campaign.StartedAt,
            EndedAt: campaign.EndedAt,
            Iterations: campaign.Iterations.Select(i => new CampaignIterationRecord(
                CampaignId: campaign.CampaignId,
                IterationIndex: i.Index,
                ParametersJson: JsonSerializer.Serialize(i.Parameters),
                RunspaceId: i.RunspaceId,
                Status: i.Status.ToString(),
                Message: i.Message,
                CreatedAt: i.CreatedAt,
                StartedAt: i.StartedAt,
                EndedAt: i.EndedAt)).ToList());

    private static Campaign FromCampaignRecord(CampaignRecord record)
    {
        var campaign = new Campaign
        {
            CampaignId = record.CampaignId,
            Name = record.Name,
            Description = record.Description,
            Status = Enum.TryParse<CampaignStatus>(record.Status, out var s) ? s : CampaignStatus.Draft,
            CreatedAt = record.CreatedAt,
            StartedAt = record.StartedAt,
            EndedAt = record.EndedAt
        };
        foreach (var iter in record.Iterations)
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(iter.ParametersJson)
                ?? new Dictionary<string, object?>();
            campaign.Iterations.Add(new CampaignIteration(
                Index: iter.IterationIndex,
                Parameters: parameters,
                RunspaceId: iter.RunspaceId,
                Status: Enum.TryParse<CampaignIterationStatus>(iter.Status, out var is2) ? is2 : CampaignIterationStatus.Pending,
                Message: iter.Message,
                CreatedAt: iter.CreatedAt,
                StartedAt: iter.StartedAt,
                EndedAt: iter.EndedAt));
        }
        return campaign;
    }
}
