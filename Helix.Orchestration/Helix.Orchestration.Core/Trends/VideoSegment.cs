namespace Helix.Orchestration.Core.Trends;

public sealed record VideoSegment(
    string SegmentId,
    string StorageUri,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime
);
