namespace Helix.Orchestration.Core.Trends;

/// <summary>
/// Generic spectral metadata wrapper for unknown or future analytical techniques.
/// </summary>
public sealed record GenericSpectralMetadata(
    IReadOnlyDictionary<string, object?> Fields
) : ISpectralMetadata;
