using Helix.Orchestration.Core.Trends;
using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace Helix.Orchestration.Agent.Trends;

/// <summary>
/// In-memory implementation of <see cref="ITrendProvider"/> for all trend kinds.
/// </summary>
public sealed class InMemoryTrendProvider : ITrendProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, InMemorySpectralTrendProvider> _spectral = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InMemoryDiodeArrayTrendProvider> _diodeArray = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, object> _scalarLatest = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, object> _scalarTrends = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InMemoryImageTrend> _image = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InMemoryVideoTrend> _video = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InMemoryDerivedTrend> _derived = new(StringComparer.OrdinalIgnoreCase);

    public IScalarTrend<T> GetScalarTrend<T>(string trendName) where T : struct, IComparable<T>
    {
        var trend = _scalarTrends.GetOrAdd(trendName, name => new InMemoryScalarTrend<T>(name, 100));
        return (InMemoryScalarTrend<T>)trend;
    }

    public TrendWindow<T> GetTrend<T>(string trendName) where T : struct, IComparable<T>
        => GetScalarTrend<T>(trendName).Window;

    public ISpectralTrend GetSpectralTrend(string trendName)
        => _spectral.GetOrAdd(trendName, name => new InMemorySpectralTrendProvider(name));

    public IDiodeArrayTrend GetDiodeArrayTrend(string trendName)
        => _diodeArray.GetOrAdd(trendName, name => new InMemoryDiodeArrayTrendProvider(name));

    public IImageTrend GetImageTrend(string trendName)
        => _image.GetOrAdd(trendName, name => new InMemoryImageTrend(name));

    public IVideoTrend GetVideoTrend(string trendName)
        => _video.GetOrAdd(trendName, name => new InMemoryVideoTrend(name));

    public IDerivedTrend GetDerivedTrend(string trendName)
        => _derived.GetOrAdd(trendName, name => new InMemoryDerivedTrend(
            new DerivedTrendDefinition(name, name, DerivationKind.MathExpression, null, [], null)));

    public Task<bool> IsTrendAvailableAsync(string trendName)
        => Task.FromResult(
            _spectral.ContainsKey(trendName) ||
            _diodeArray.ContainsKey(trendName) ||
            _scalarLatest.ContainsKey(trendName) ||
            _scalarTrends.ContainsKey(trendName) ||
            _image.ContainsKey(trendName) ||
            _video.ContainsKey(trendName) ||
            _derived.ContainsKey(trendName));

    public IEnumerable<string> GetAllTrendNames()
        => _spectral.Keys
            .Concat(_diodeArray.Keys)
            .Concat(_scalarLatest.Keys)
            .Concat(_scalarTrends.Keys)
            .Concat(_image.Keys)
            .Concat(_video.Keys)
            .Concat(_derived.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<ITrend> GetAllTrends()
        => _spectral.Values.Cast<ITrend>()
            .Concat(_diodeArray.Values)
            .Concat(_scalarTrends.Values.Cast<ITrend>())
            .Concat(_image.Values)
            .Concat(_video.Values)
            .Concat(_derived.Values);

    public IEnumerable<ITrend> GetTrendsByKind(TrendKind kind)
        => kind switch
        {
            TrendKind.Spectral => _spectral.Values,
            TrendKind.DiodeArray => _diodeArray.Values,
            TrendKind.Scalar => _scalarTrends.Values.Cast<ITrend>(),
            TrendKind.Image => _image.Values,
            TrendKind.Video => _video.Values,
            TrendKind.Derived => _derived.Values,
            _ => []
        };

    public IEnumerable<ITrend> GetTrendsByExperiment(string experimentId)
        => GetAllTrends().Where(t => string.Equals(t.ExperimentId, experimentId, StringComparison.OrdinalIgnoreCase));

    public void RegisterDerivedTrend(DerivedTrendDefinition definition)
        => _derived[definition.TrendId] = new InMemoryDerivedTrend(definition);

    /// <summary>
    /// Pushes a one-dimensional spectrum to the named trend stream.
    /// </summary>
    public void Push1D(string trendName, Spectrum1D spectrum)
    {
        var trend = _spectral.GetOrAdd(trendName, name => new InMemorySpectralTrendProvider(name));
        trend.Push(spectrum);
    }

    /// <summary>
    /// Pushes a two-dimensional spectrum to the named trend stream.
    /// </summary>
    public void Push2D(string trendName, Spectrum2D spectrum)
    {
        var trend = _spectral.GetOrAdd(trendName, name => new InMemorySpectralTrendProvider(name));
        trend.Push(spectrum);
    }

    /// <summary>
    /// Pushes a diode-array slice to the named trend stream.
    /// </summary>
    public void PushDiodeArraySlice(string trendName, DiodeArraySlice slice)
    {
        var trend = _diodeArray.GetOrAdd(trendName, name => new InMemoryDiodeArrayTrendProvider(name));
        trend.Push(slice);
    }

    /// <summary>
    /// Pushes a scalar value to both the latest-value store and the <see cref="InMemoryScalarTrend{T}"/> window.
    /// </summary>
    public void PushScalar<T>(string trendName, T value) where T : struct, IComparable<T>
    {
        _scalarLatest[trendName] = value;
        ((InMemoryScalarTrend<T>)_scalarTrends.GetOrAdd(trendName, name => new InMemoryScalarTrend<T>(name, 100))).Push(value);
    }

    /// <summary>
    /// Reads the latest scalar value for the named trend. Returns false if not present.
    /// </summary>
    public bool TryGetScalar(string trendName, out object? value)
        => _scalarLatest.TryGetValue(trendName, out value);

    public void Dispose()
    {
        foreach (var trend in _spectral.Values)
            trend.Dispose();
        foreach (var trend in _diodeArray.Values)
            trend.Dispose();
        foreach (var trend in _scalarTrends.Values.OfType<IDisposable>())
            trend.Dispose();
        foreach (var trend in _image.Values)
            trend.Dispose();
        foreach (var trend in _video.Values)
            trend.Dispose();
    }
}

public sealed class InMemoryImageTrend : IImageTrend, IDisposable
{
    private readonly Subject<ImageFrame> _frames = new();

    public string TrendId { get; }
    public string TrendName { get; }
    public TrendKind TrendKind => TrendKind.Image;
    public string? Units => null;
    public string? ExperimentId => null;
    public IReadOnlyList<string> Tags => [];
    public bool IsOnline { get; private set; } = true;
    public DateTimeOffset LastUpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public IObservable<TrendValue> Values => System.Reactive.Linq.Observable.Empty<TrendValue>();
    public IObservable<ImageFrame> Frames => _frames;
    public ImageFrame? LatestFrame { get; private set; }

    public InMemoryImageTrend(string trendName)
    {
        TrendName = trendName;
        TrendId = trendName;
    }

    public void Push(ImageFrame frame)
    {
        LatestFrame = frame;
        LastUpdatedAt = DateTimeOffset.UtcNow;
        _frames.OnNext(frame);
    }

    public void Dispose()
    {
        IsOnline = false;
        _frames.OnCompleted();
        _frames.Dispose();
    }
}

public sealed class InMemoryVideoTrend : IVideoTrend, IDisposable
{
    private readonly Subject<VideoSegment> _segments = new();

    public string TrendId { get; }
    public string TrendName { get; }
    public TrendKind TrendKind => TrendKind.Video;
    public string? Units => null;
    public string? ExperimentId => null;
    public IReadOnlyList<string> Tags => [];
    public bool IsOnline { get; private set; } = true;
    public DateTimeOffset LastUpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public IObservable<TrendValue> Values => System.Reactive.Linq.Observable.Empty<TrendValue>();
    public IObservable<VideoSegment> Segments => _segments;
    public VideoSegment? LatestSegment { get; private set; }

    public InMemoryVideoTrend(string trendName)
    {
        TrendName = trendName;
        TrendId = trendName;
    }

    public void Push(VideoSegment segment)
    {
        LatestSegment = segment;
        LastUpdatedAt = DateTimeOffset.UtcNow;
        _segments.OnNext(segment);
    }

    public void Dispose()
    {
        IsOnline = false;
        _segments.OnCompleted();
        _segments.Dispose();
    }
}

public sealed class InMemoryDerivedTrend : IDerivedTrend
{
    public string TrendId => Definition.TrendId;
    public string TrendName => Definition.TrendName;
    public TrendKind TrendKind => TrendKind.Derived;
    public string? Units => null;
    public string? ExperimentId => null;
    public IReadOnlyList<string> Tags => [];
    public bool IsOnline => true;
    public DateTimeOffset LastUpdatedAt { get; } = DateTimeOffset.UtcNow;
    public IObservable<TrendValue> Values => System.Reactive.Linq.Observable.Empty<TrendValue>();
    public DerivedTrendDefinition Definition { get; }
    public IReadOnlyList<string> SourceTrendIds => Definition.SourceTrendIds;

    public InMemoryDerivedTrend(DerivedTrendDefinition definition)
        => Definition = definition;
}
