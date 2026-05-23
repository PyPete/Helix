namespace Helix.Orchestration.Core.Trends;

public interface IDiodeArrayTrend : ITrend
{
    IObservable<DiodeArraySlice> Slices { get; }
    IReadOnlyList<DiodeArraySlice> GetSnapshot();
    IScalarTrend<double> ExtractChromatogram(double wavelengthNm);
    SpectralScan? GetSpectrumAt(DateTimeOffset timestamp);
}
