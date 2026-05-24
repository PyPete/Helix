using Helix.Orchestration.Core.Trends;
using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace Helix.Orchestration.Agent.Trends;

/// <summary>
/// In-memory diode-array trend provider with observable slice stream and chromatogram extraction.
/// </summary>
public sealed class InMemoryDiodeArrayTrendProvider : IDiodeArrayTrend, IDisposable
{
    private const int Capacity = 100;

    private readonly object _sync = new();
    private readonly Subject<DiodeArraySlice> _slices = new();
    private readonly Subject<TrendValue> _values = new();
    private readonly List<DiodeArraySlice> _snapshot = [];
    private readonly ConcurrentDictionary<double, InMemoryScalarTrend<double>> _chromatograms = new();

    public string TrendId { get; }
    public string TrendName { get; }
    public TrendKind TrendKind => TrendKind.DiodeArray;
    public string? Units => null;
    public string? ExperimentId { get; }
    public IReadOnlyList<string> Tags { get; }
    public bool IsOnline { get; private set; } = true;
    public DateTimeOffset LastUpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public IObservable<DiodeArraySlice> Slices => _slices;
    public IObservable<TrendValue> Values => _values;

    public InMemoryDiodeArrayTrendProvider(
        string trendName,
        string? trendId = null,
        string? experimentId = null,
        IReadOnlyList<string>? tags = null)
    {
        TrendName = trendName ?? throw new ArgumentNullException(nameof(trendName));
        TrendId = string.IsNullOrWhiteSpace(trendId) ? trendName : trendId;
        ExperimentId = experimentId;
        Tags = tags ?? [];
    }

    /// <summary>
    /// Pushes a new diode-array slice into the provider.
    /// </summary>
    public void Push(DiodeArraySlice slice)
    {
        ArgumentNullException.ThrowIfNull(slice);

        lock (_sync)
        {
            LastUpdatedAt = slice.Timestamp;
            _snapshot.Add(slice);
            if (_snapshot.Count > Capacity)
                _snapshot.RemoveAt(0);
        }

        _slices.OnNext(slice);
        _values.OnNext(new StructuredResultValue("diode-array-slice", slice));

        foreach (var kvp in _chromatograms)
        {
            kvp.Value.Push(ExtractAbsorbance(slice, kvp.Key));
        }
    }

    public IReadOnlyList<DiodeArraySlice> GetSnapshot()
    {
        lock (_sync)
        {
            return _snapshot.ToArray();
        }
    }

    public IScalarTrend<double> ExtractChromatogram(double wavelengthNm)
        => _chromatograms.GetOrAdd(wavelengthNm, key => new InMemoryScalarTrend<double>($"{TrendName}:{key:F2}nm", capacity: Capacity));

    public SpectralScan? GetSpectrumAt(DateTimeOffset timestamp)
    {
        DiodeArraySlice? match;

        lock (_sync)
        {
            match = _snapshot.OrderBy(s => Math.Abs((s.Timestamp - timestamp).Ticks)).FirstOrDefault();
        }

        if (match is null)
            return null;

        return new SpectralScan(
            TrendId: TrendId,
            XData: match.Wavelengths,
            YData: match.Absorbances,
            SpectralType: SpectralType.UV,
            SampleId: null,
            Timestamp: match.Timestamp,
            Metadata: new Dictionary<string, object?>
            {
                ["source"] = "diode-array"
            });
    }

    private static double ExtractAbsorbance(DiodeArraySlice slice, double wavelengthNm)
    {
        if (slice.Wavelengths.Length == 0 || slice.Absorbances.Length == 0)
            return 0d;

        var nearestIndex = 0;
        var nearestDelta = Math.Abs(slice.Wavelengths[0] - wavelengthNm);

        for (var i = 1; i < slice.Wavelengths.Length && i < slice.Absorbances.Length; i++)
        {
            var delta = Math.Abs(slice.Wavelengths[i] - wavelengthNm);
            if (delta < nearestDelta)
            {
                nearestDelta = delta;
                nearestIndex = i;
            }
        }

        return slice.Absorbances[nearestIndex];
    }

    public void Dispose()
    {
        IsOnline = false;
        _slices.OnCompleted();
        _values.OnCompleted();
        _slices.Dispose();
        _values.Dispose();

        foreach (var trend in _chromatograms.Values)
            trend.Dispose();
    }
}
