namespace Helix.Orchestration.Core.Trends;

public interface ITrend
{
    string TrendId { get; }
    string TrendName { get; }
    TrendKind TrendKind { get; }
    string? Units { get; }
    string? ExperimentId { get; }
    IReadOnlyList<string> Tags { get; }
    bool IsOnline { get; }
    DateTimeOffset LastUpdatedAt { get; }
    IObservable<TrendValue> Values { get; }
}
