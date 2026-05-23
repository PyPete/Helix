namespace Helix.Orchestration.Core.Trends;

public sealed record SpectralMatrix(
    string TrendId,
    double[] Axis1,
    double[] Axis2,
    double[,] Intensity,
    SpectralType SpectralType,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object?> Metadata
);
