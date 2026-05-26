namespace Helix.Orchestration.Core.Trends;

/// <summary>
/// Typed metadata for tandem mass spectrometry (MS/MS) spectra.
/// </summary>
public sealed record MsMsMetadata(
    int MsLevel,
    double? PrecursorMz,
    int? PrecursorCharge,
    double? RetentionTimeSec,
    string? FragmentationMethod,
    double? CollisionEnergy,
    int? ScanNumber,
    double? IsolationWindowMz,
    double? IsolationWindowWidth,
    int? ParentScanNumber
) : MsMetadata(
    MsLevel,
    PrecursorMz,
    PrecursorCharge,
    RetentionTimeSec,
    FragmentationMethod,
    CollisionEnergy,
    ScanNumber
);
