namespace Helix.Orchestration.Core.Trends;

/// <summary>
/// Typed metadata for two-dimensional NMR spectra.
/// </summary>
public sealed record Nmr2DMetadata(
    string F1Nucleus,
    string F2Nucleus,
    string ExperimentType,
    double FieldStrengthMhz,
    int? NumberOfPointsF1,
    int? NumberOfPointsF2
) : ISpectralMetadata;
