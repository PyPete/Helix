namespace Helix.Orchestration.Core.Trends;

public interface ISpectralTrend : ITrend
{
    SpectralScan? LatestScan { get; }
    SpectralMatrix? LatestMatrix { get; }
    IObservable<SpectralScan> Scans { get; }
    IObservable<SpectralMatrix> Matrices { get; }
    IReadOnlyList<SpectralScan> GetRecentScans(int count);
    IReadOnlyList<SpectralMatrix> GetRecentMatrices(int count);
}
