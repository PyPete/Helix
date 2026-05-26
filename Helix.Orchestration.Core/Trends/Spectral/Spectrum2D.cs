namespace Helix.Orchestration.Core.Trends;

/// <summary>
/// Universal two-dimensional analytical spectrum envelope.
/// Shape (Axis1, Axis2, Intensity matrix) is stable; semantics are carried by typed metadata.
/// </summary>
public sealed record Spectrum2D(
    string TrendId,
    double[] Axis1,
    double[] Axis2,
    double[,] Intensity,
    SpectralType SpectralType,
    ISpectralMetadata Metadata,
    DateTimeOffset Timestamp
);
