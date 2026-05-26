namespace Helix.Orchestration.Core.Trends;

/// <summary>
/// Typed metadata for mass spectrometry (MS1/MS2/MS3) spectra.
/// </summary>
public record MsMetadata(
    int MsLevel,
    double? PrecursorMz,
    int? PrecursorCharge,
    double? RetentionTimeSec,
    string? FragmentationMethod,
    double? CollisionEnergy,
    int? ScanNumber
) : ISpectralMetadata;
