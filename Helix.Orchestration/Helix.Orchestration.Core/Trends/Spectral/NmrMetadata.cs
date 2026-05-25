namespace Helix.Orchestration.Core.Trends;

/// <summary>
/// Typed metadata for one-dimensional NMR spectra.
/// </summary>
public sealed record NmrMetadata(
    string Nucleus,
    double FieldStrengthMhz,
    string? Solvent,
    int? NumberOfScans,
    double? SpectralWidthHz,
    double? TransmitterFrequencyHz
) : ISpectralMetadata;
