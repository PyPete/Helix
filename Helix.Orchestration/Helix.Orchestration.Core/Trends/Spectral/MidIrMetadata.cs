namespace Helix.Orchestration.Core.Trends;

/// <summary>
/// Typed metadata for Mid-IR (ATR-FTIR) spectra.
/// </summary>
public sealed record MidIrMetadata(
    double WavenumberMin,
    double WavenumberMax,
    double ResolutionCm,
    int ScanCount,
    bool AttenuatedTotalReflection,
    string? ApodizationFunction
) : ISpectralMetadata;
