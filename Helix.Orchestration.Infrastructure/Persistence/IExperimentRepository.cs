namespace Helix.Orchestration.Infrastructure.Persistence;

public interface IExperimentRepository
{
    Task SaveAsync(ExperimentRecord record, CancellationToken ct = default);

    Task<ExperimentRecord?> GetByRunspaceIdAsync(string runspaceId, CancellationToken ct = default);

    Task<IReadOnlyList<ExperimentRecord>> GetByCampaignIdAsync(string campaignId, CancellationToken ct = default);
}

public sealed record ExperimentRecord(
    string RunspaceId,
    string? CampaignId,
    string DefinitionId,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    string? FaultMessage,
    string DefinitionJson
);
