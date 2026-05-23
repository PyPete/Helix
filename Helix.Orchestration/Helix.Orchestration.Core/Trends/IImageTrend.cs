namespace Helix.Orchestration.Core.Trends;

public interface IImageTrend : ITrend
{
    IObservable<ImageFrame> Frames { get; }
    ImageFrame? LatestFrame { get; }
}
