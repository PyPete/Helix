namespace Helix.Orchestration.SiteServer.Contracts;

public sealed class CreateCampaignRequest
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public List<Dictionary<string, object?>> ParameterSets { get; set; } = new();
}

public sealed class CampaignResponse
{
    public string CampaignId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public List<CampaignIterationResponse> Iterations { get; set; } = new();
}

public sealed class CampaignIterationResponse
{
    public int Index { get; set; }

    public Dictionary<string, object?> Parameters { get; set; } = new();

    public string? RunspaceId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }
}
