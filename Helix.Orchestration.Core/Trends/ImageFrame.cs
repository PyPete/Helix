namespace Helix.Orchestration.Core.Trends;

public sealed record ImageFrame(
    string FrameId,
    string StorageUri,
    byte[]? ThumbnailBytes,
    int Width,
    int Height,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object?> Metadata
);
