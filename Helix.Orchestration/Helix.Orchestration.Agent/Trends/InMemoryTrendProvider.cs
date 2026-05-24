using Helix.Orchestration.Core.Trends;
using System.Collections.Concurrent;

namespace Helix.Orchestration.Agent.Trends;

/// <summary>
/// In-memory implementation of <see cref="ITrendProvider"/> for spectral and diode-array trends.
/// </summary>
public sealed class InMemoryTrendProvider : ITrendProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, InMemorySpectralTrendProvider> _spectral = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InMemoryDiodeArrayTrendProvider> _diodeArray = new(StringComparer.OrdinalIgnoreCase);

    public TrendWindow<T> GetTrend<T>(string trendName) where T : struct, IComparable<T>
        => throw new NotSupportedException("TODO: Scalar trend windows are not implemented in this phase.");

    public IScalarTrend<T> GetScalarTrend<T>(string trendName) where T : struct, IComparable<T>
        => throw new NotSupportedException("TODO: Scalar trends are not implemented in this phase.");

    public ISpectralTrend GetSpectralTrend(string trendName)
        => _spectral.GetOrAdd(trendName, name => new InMemorySpectralTrendProvider(name));

    public IDiodeArrayTrend GetDiodeArrayTrend(string trendName)
        => _diodeArray.GetOrAdd(trendName, name => new InMemoryDiodeArrayTrendProvider(name));

    public IImageTrend GetImageTrend(string trendName)
        => throw new NotSupportedException("TODO: Image trends are not implemented in this phase.");

    public IVideoTrend GetVideoTrend(string trendName)
        => throw new NotSupportedException("TODO: Video trends are not implemented in this phase.");

    public IDerivedTrend GetDerivedTrend(string trendName)
        => throw new NotSupportedException("TODO: Derived trends are not implemented in this phase.");

    public Task<bool> IsTrendAvailableAsync(string trendName)
        => Task.FromResult(_spectral.ContainsKey(trendName) || _diodeArray.ContainsKey(trendName));

    public IEnumerable<string> GetAllTrendNames()
        => _spectral.Keys.Concat(_diodeArray.Keys).Distinct(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<ITrend> GetAllTrends()
        => _spectral.Values.Cast<ITrend>().Concat(_diodeArray.Values);

    public IEnumerable<ITrend> GetTrendsByKind(TrendKind kind)
        => kind switch
        {
            TrendKind.Spectral => _spectral.Values,
            TrendKind.DiodeArray => _diodeArray.Values,
            _ => []
        };

    public IEnumerable<ITrend> GetTrendsByExperiment(string experimentId)
        => GetAllTrends().Where(t => string.Equals(t.ExperimentId, experimentId, StringComparison.OrdinalIgnoreCase));

    public void RegisterDerivedTrend(DerivedTrendDefinition definition)
        => throw new NotSupportedException("TODO: Derived trend registration is not implemented in this phase.");

    /// <summary>
    /// Pushes a spectral scan to the named trend stream.
    /// </summary>
    public void PushSpectralScan(string trendName, SpectralScan scan)
    {
        var trend = _spectral.GetOrAdd(trendName, name => new InMemorySpectralTrendProvider(name));
        trend.Push(scan);
    }

    /// <summary>
    /// Pushes a spectral matrix to the named trend stream.
    /// </summary>
    public void PushSpectralMatrix(string trendName, SpectralMatrix matrix)
    {
        var trend = _spectral.GetOrAdd(trendName, name => new InMemorySpectralTrendProvider(name));
        trend.Push(matrix);
    }

    /// <summary>
    /// Pushes a diode-array slice to the named trend stream.
    /// </summary>
    public void PushDiodeArraySlice(string trendName, DiodeArraySlice slice)
    {
        var trend = _diodeArray.GetOrAdd(trendName, name => new InMemoryDiodeArrayTrendProvider(name));
        trend.Push(slice);
    }

    public void Dispose()
    {
        foreach (var trend in _spectral.Values)
            trend.Dispose();

        foreach (var trend in _diodeArray.Values)
            trend.Dispose();
    }
}
