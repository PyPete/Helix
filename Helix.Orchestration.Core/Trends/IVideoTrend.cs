namespace Helix.Orchestration.Core.Trends;

public interface IVideoTrend : ITrend
{
    IObservable<VideoSegment> Segments { get; }
    VideoSegment? LatestSegment { get; }
}
