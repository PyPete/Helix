namespace Helix.Orchestration.Infrastructure.Persistence;

public interface IExperimentEventRepository
{
    Task AppendAsync(ExperimentEventRecord record, CancellationToken ct = default);

    Task<IReadOnlyList<ExperimentEventRecord>> GetByExperimentIdAsync(string experimentId, CancellationToken ct = default);

    Task<IReadOnlyList<ExperimentEventRecord>> GetReplayLogAsync(string experimentId, CancellationToken ct = default);
}

public sealed record ExperimentEventRecord(
    long SequenceNum,
    string ExperimentId,
    string Phase,
    string EventType,
    string RunspaceId,
    string? StepId,
    string? StepLabel,
    bool? IsManual,
    string? DeviceId,
    string? Capability,
    string? ParametersJson,
    string? ResultJson,
    DateTimeOffset OccurredAt
);
