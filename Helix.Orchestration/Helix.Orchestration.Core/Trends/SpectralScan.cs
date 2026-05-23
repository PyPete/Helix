namespace Helix.Orchestration.Core.Trends;

public sealed record SpectralScan(
    string TrendId,
    double[] XData,
    double[] YData,
    SpectralType SpectralType,
    string? SampleId,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object?> Metadata
);
