using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Helix.Orchestration.Core.Trends;

namespace Helix.Orchestration.Simulation;

/// <summary>
/// In-memory ITrendProvider implementation for simulation purposes.
/// Allows sim devices to push TrendValue updates via PublishValue.
/// </summary>
public sealed class SimulatedTrendProvider : ITrendProvider
{
    private readonly ConcurrentDictionary<string, SimTrendEntry> _trends = new();

    /// <summary>
    /// Registers a new trend with the provider. Must be called before PublishValue.
    /// </summary>
    public void RegisterTrend(string trendId, string trendName, TrendKind kind, string? units = null, string? experimentId = null, IReadOnlyList<string>? tags = null)
    {
        _trends.TryAdd(trendId, new SimTrendEntry(trendId, trendName, kind, units, experimentId, tags ?? Array.Empty<string>()));
    }

    /// <summary>
    /// Publishes a TrendValue to the specified trend. Trend must be registered first.
    /// </summary>
    public void PublishValue(string trendId, TrendValue value)
    {
        if (_trends.TryGetValue(trendId, out var entry))
        {
            entry.Subject.OnNext(value);
            entry.LastUpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public TrendWindow<T> GetTrend<T>(string trendName) where T : struct, IComparable<T>
    {
        return GetScalarTrend<T>(trendName).Window;
    }

    public IScalarTrend<T> GetScalarTrend<T>(string trendName) where T : struct, IComparable<T>
    {
        var entry = _trends.Values.FirstOrDefault(e => e.TrendName == trendName && e.TrendKind == TrendKind.Scalar);
        if (entry == null)
            throw new KeyNotFoundException($"Scalar trend '{trendName}' not found.");
        return new SimScalarTrend<T>(entry);
    }

    public ISpectralTrend GetSpectralTrend(string trendName)
    {
        var entry = _trends.Values.FirstOrDefault(e => e.TrendName == trendName && e.TrendKind == TrendKind.Spectral);
        if (entry == null)
            throw new KeyNotFoundException($"Spectral trend '{trendName}' not found.");
        return new SimSpectralTrend(entry);
    }

    public IDiodeArrayTrend GetDiodeArrayTrend(string trendName)
    {
        var entry = _trends.Values.FirstOrDefault(e => e.TrendName == trendName && e.TrendKind == TrendKind.DiodeArray);
        if (entry == null)
            throw new KeyNotFoundException($"Diode array trend '{trendName}' not found.");
        return new SimDiodeArrayTrend(entry);
    }

    public IImageTrend GetImageTrend(string trendName)
    {
        var entry = _trends.Values.FirstOrDefault(e => e.TrendName == trendName && e.TrendKind == TrendKind.Image);
        if (entry == null)
            throw new KeyNotFoundException($"Image trend '{trendName}' not found.");
        return new SimImageTrend(entry);
    }

    public IVideoTrend GetVideoTrend(string trendName)
    {
        var entry = _trends.Values.FirstOrDefault(e => e.TrendName == trendName && e.TrendKind == TrendKind.Video);
        if (entry == null)
            throw new KeyNotFoundException($"Video trend '{trendName}' not found.");
        return new SimVideoTrend(entry);
    }

    public IDerivedTrend GetDerivedTrend(string trendName)
    {
        var entry = _trends.Values.FirstOrDefault(e => e.TrendName == trendName && e.TrendKind == TrendKind.Derived);
        if (entry == null)
            throw new KeyNotFoundException($"Derived trend '{trendName}' not found.");
        return new SimDerivedTrend(entry);
    }

    public Task<bool> IsTrendAvailableAsync(string trendName)
    {
        return Task.FromResult(_trends.Values.Any(e => e.TrendName == trendName));
    }

    public IEnumerable<string> GetAllTrendNames()
    {
        return _trends.Values.Select(e => e.TrendName);
    }

    public IEnumerable<ITrend> GetAllTrends()
    {
        return _trends.Values.Select(e => new SimTrendInfo(e));
    }

    public IEnumerable<ITrend> GetTrendsByKind(TrendKind kind)
    {
        return _trends.Values.Where(e => e.TrendKind == kind).Select(e => new SimTrendInfo(e));
    }

    public IEnumerable<ITrend> GetTrendsByExperiment(string experimentId)
    {
        return _trends.Values.Where(e => e.ExperimentId == experimentId).Select(e => new SimTrendInfo(e));
    }

    public void RegisterDerivedTrend(DerivedTrendDefinition definition)
    {
        // For Phase 9, derived trends are not implemented
        throw new NotImplementedException("Derived trends are not yet implemented in the simulation provider.");
    }
}

/// <summary>
/// Internal entry holding trend metadata and the reactive Subject.
/// </summary>
internal sealed class SimTrendEntry
{
    public string TrendId { get; }
    public string TrendName { get; }
    public TrendKind TrendKind { get; }
    public string? Units { get; }
    public string? ExperimentId { get; }
    public IReadOnlyList<string> Tags { get; }
    public Subject<TrendValue> Subject { get; } = new();
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public SimTrendEntry(string trendId, string trendName, TrendKind kind, string? units, string? experimentId, IReadOnlyList<string> tags)
    {
        TrendId = trendId;
        TrendName = trendName;
        TrendKind = kind;
        Units = units;
        ExperimentId = experimentId;
        Tags = tags;
    }
}

/// <summary>
/// Minimal ITrend implementation for GetAllTrends enumeration.
/// </summary>
internal sealed class SimTrendInfo : ITrend
{
    private readonly SimTrendEntry _entry;

    public SimTrendInfo(SimTrendEntry entry) => _entry = entry;

    public string TrendId => _entry.TrendId;
    public string TrendName => _entry.TrendName;
    public TrendKind TrendKind => _entry.TrendKind;
    public string? Units => _entry.Units;
    public string? ExperimentId => _entry.ExperimentId;
    public IReadOnlyList<string> Tags => _entry.Tags;
    public bool IsOnline => true; // Sim trends are always "online"
    public DateTimeOffset LastUpdatedAt => _entry.LastUpdatedAt;
    public IObservable<TrendValue> Values => _entry.Subject;
}

/// <summary>
/// IScalarTrend implementation backed by a TrendWindow.
/// </summary>
internal sealed class SimScalarTrend<T> : IScalarTrend<T> where T : struct, IComparable<T>
{
    private readonly SimTrendEntry _entry;
    private readonly TrendWindow<T> _window;

    public SimScalarTrend(SimTrendEntry entry)
    {
        _entry = entry;
        _window = new TrendWindow<T>(entry.TrendName, 1000);

        // Subscribe to the subject and push scalar values into the window
        entry.Subject.Subscribe(value =>
        {
            if (value is ScalarValue<T> scalar)
            {
                _window.Add(scalar.Value);
            }
        });
    }

    public string TrendId => _entry.TrendId;
    public string TrendName => _entry.TrendName;
    public TrendKind TrendKind => _entry.TrendKind;
    public string? Units => _entry.Units;
    public string? ExperimentId => _entry.ExperimentId;
    public IReadOnlyList<string> Tags => _entry.Tags;
    public bool IsOnline => true;
    public DateTimeOffset LastUpdatedAt => _entry.LastUpdatedAt;
    public IObservable<TrendValue> Values => _entry.Subject;
    public TrendWindow<T> Window => _window;
    public T? Latest => _window.Count > 0 ? _window.Latest() : null;
}

/// <summary>
/// ISpectralTrend implementation backed by a rolling list of Spectrum1D/Spectrum2D.
/// </summary>
internal sealed class SimSpectralTrend : ISpectralTrend
{
    private readonly SimTrendEntry _entry;
    private readonly List<Spectrum1D> _recentScans = new();
    private readonly List<Spectrum2D> _recentMatrices = new();
    private readonly object _lock = new();

    public SimSpectralTrend(SimTrendEntry entry)
    {
        _entry = entry;
        entry.Subject.Subscribe(value =>
        {
            lock (_lock)
            {
                if (value is SpectralValue spectral)
                {
                    _recentScans.Add(spectral.Scan);
                    if (_recentScans.Count > 100) _recentScans.RemoveAt(0);
                }
                else if (value is SpectralMatrixValue matrix)
                {
                    _recentMatrices.Add(matrix.Matrix);
                    if (_recentMatrices.Count > 100) _recentMatrices.RemoveAt(0);
                }
            }
        });
    }

    public string TrendId => _entry.TrendId;
    public string TrendName => _entry.TrendName;
    public TrendKind TrendKind => _entry.TrendKind;
    public string? Units => _entry.Units;
    public string? ExperimentId => _entry.ExperimentId;
    public IReadOnlyList<string> Tags => _entry.Tags;
    public bool IsOnline => true;
    public DateTimeOffset LastUpdatedAt => _entry.LastUpdatedAt;
    public IObservable<TrendValue> Values => _entry.Subject;

    public Spectrum1D? LatestScan
    {
        get { lock (_lock) return _recentScans.Count > 0 ? _recentScans[^1] : null; }
    }

    public Spectrum2D? LatestMatrix
    {
        get { lock (_lock) return _recentMatrices.Count > 0 ? _recentMatrices[^1] : null; }
    }

    public IObservable<Spectrum1D> Scans => _entry.Subject
        .Where(v => v is SpectralValue)
        .Select(v => ((SpectralValue)v).Scan);

    public IObservable<Spectrum2D> Matrices => _entry.Subject
        .Where(v => v is SpectralMatrixValue)
        .Select(v => ((SpectralMatrixValue)v).Matrix);

    public IReadOnlyList<Spectrum1D> GetRecentScans(int count)
    {
        lock (_lock)
        {
            return _recentScans.TakeLast(count).ToList();
        }
    }

    public IReadOnlyList<Spectrum2D> GetRecentMatrices(int count)
    {
        lock (_lock)
        {
            return _recentMatrices.TakeLast(count).ToList();
        }
    }
}

/// <summary>
/// Stub implementations for other trend kinds (Phase 9 scope: scalar + spectral only).
/// </summary>
internal sealed class SimDiodeArrayTrend : IDiodeArrayTrend
{
    private readonly SimTrendEntry _entry;

    public SimDiodeArrayTrend(SimTrendEntry entry)
    {
        _entry = entry;
    }

    public string TrendId => _entry.TrendId;
    public string TrendName => _entry.TrendName;
    public TrendKind TrendKind => _entry.TrendKind;
    public string? Units => _entry.Units;
    public string? ExperimentId => _entry.ExperimentId;
    public IReadOnlyList<string> Tags => _entry.Tags;
    public bool IsOnline => true;
    public DateTimeOffset LastUpdatedAt => _entry.LastUpdatedAt;
    public IObservable<TrendValue> Values => _entry.Subject;

    public IObservable<DiodeArraySlice> Slices => Observable.Empty<DiodeArraySlice>();

    public IReadOnlyList<DiodeArraySlice> GetSnapshot() => Array.Empty<DiodeArraySlice>();

    public IScalarTrend<double> ExtractChromatogram(double wavelengthNm)
        => throw new NotImplementedException("Diode-array chromatogram extraction is not implemented in simulation yet.");

    public Spectrum1D? GetSpectrumAt(DateTimeOffset timestamp) => null;
}

internal sealed class SimImageTrend : IImageTrend
{
    private readonly SimTrendEntry _entry;

    public SimImageTrend(SimTrendEntry entry)
    {
        _entry = entry;
    }

    public string TrendId => _entry.TrendId;
    public string TrendName => _entry.TrendName;
    public TrendKind TrendKind => _entry.TrendKind;
    public string? Units => _entry.Units;
    public string? ExperimentId => _entry.ExperimentId;
    public IReadOnlyList<string> Tags => _entry.Tags;
    public bool IsOnline => true;
    public DateTimeOffset LastUpdatedAt => _entry.LastUpdatedAt;
    public IObservable<TrendValue> Values => _entry.Subject;

    public IObservable<ImageFrame> Frames => Observable.Empty<ImageFrame>();

    public ImageFrame? LatestFrame => null;
}

internal sealed class SimVideoTrend : IVideoTrend
{
    private readonly SimTrendEntry _entry;

    public SimVideoTrend(SimTrendEntry entry)
    {
        _entry = entry;
    }

    public string TrendId => _entry.TrendId;
    public string TrendName => _entry.TrendName;
    public TrendKind TrendKind => _entry.TrendKind;
    public string? Units => _entry.Units;
    public string? ExperimentId => _entry.ExperimentId;
    public IReadOnlyList<string> Tags => _entry.Tags;
    public bool IsOnline => true;
    public DateTimeOffset LastUpdatedAt => _entry.LastUpdatedAt;
    public IObservable<TrendValue> Values => _entry.Subject;

    public IObservable<VideoSegment> Segments => Observable.Empty<VideoSegment>();

    public VideoSegment? LatestSegment => null;
}

internal sealed class SimDerivedTrend : IDerivedTrend
{
    private readonly SimTrendEntry _entry;

    public SimDerivedTrend(SimTrendEntry entry)
    {
        _entry = entry;
    }

    public string TrendId => _entry.TrendId;
    public string TrendName => _entry.TrendName;
    public TrendKind TrendKind => _entry.TrendKind;
    public string? Units => _entry.Units;
    public string? ExperimentId => _entry.ExperimentId;
    public IReadOnlyList<string> Tags => _entry.Tags;
    public bool IsOnline => true;
    public DateTimeOffset LastUpdatedAt => _entry.LastUpdatedAt;
    public IObservable<TrendValue> Values => _entry.Subject;

    public DerivedTrendDefinition Definition => new(
        TrendId: TrendId,
        TrendName: TrendName,
        DerivationKind: DerivationKind.MathExpression,
        Expression: null,
        SourceTrendIds: Array.Empty<string>(),
        Delegate: null);

    public IReadOnlyList<string> SourceTrendIds => Array.Empty<string>();
}
