namespace Helix.Orchestration.Core.Trends;

public interface ISpectralTrend : ITrend
{
    Spectrum1D? LatestScan { get; }
    Spectrum2D? LatestMatrix { get; }
    IObservable<Spectrum1D> Scans { get; }
    IObservable<Spectrum2D> Matrices { get; }
    IReadOnlyList<Spectrum1D> GetRecentScans(int count);
    IReadOnlyList<Spectrum2D> GetRecentMatrices(int count);
}
