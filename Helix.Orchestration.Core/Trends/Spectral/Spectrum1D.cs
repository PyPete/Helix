namespace Helix.Orchestration.Core.Trends;

/// <summary>
/// Universal one-dimensional analytical spectrum envelope.
/// Shape (X,Y) is stable; semantics are carried by typed <see cref="ISpectralMetadata"/>.
/// </summary>
public sealed record Spectrum1D(
    string TrendId,
    double[] X,
    double[] Y,
    SpectralType SpectralType,
    ISpectralMetadata Metadata,
    string? SampleId,
    DateTimeOffset Timestamp
);
