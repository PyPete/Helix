using Helix.Orchestration.Core.Trends;
using System.Reactive.Subjects;

namespace Helix.Orchestration.Agent.Trends;

/// <summary>
/// In-memory spectral trend provider with observable streams and bounded history.
/// </summary>
public sealed class InMemorySpectralTrendProvider : ISpectralTrend, IDisposable
{
    private const int Capacity = 100;

    private readonly object _sync = new();
    private readonly Subject<Spectrum1D> _scans = new();
    private readonly Subject<Spectrum2D> _matrices = new();
    private readonly Subject<TrendValue> _values = new();
    private readonly List<Spectrum1D> _recentScans = [];
    private readonly List<Spectrum2D> _recentMatrices = [];

    public string TrendId { get; }
    public string TrendName { get; }
    public TrendKind TrendKind => TrendKind.Spectral;
    public string? Units { get; }
    public string? ExperimentId { get; }
    public IReadOnlyList<string> Tags { get; }
    public bool IsOnline { get; private set; } = true;
    public DateTimeOffset LastUpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public Spectrum1D? LatestScan { get; private set; }
    public Spectrum2D? LatestMatrix { get; private set; }
    public IObservable<Spectrum1D> Scans => _scans;
    public IObservable<Spectrum2D> Matrices => _matrices;
    public IObservable<TrendValue> Values => _values;

    public InMemorySpectralTrendProvider(
        string trendName,
        string? trendId = null,
        string? units = null,
        string? experimentId = null,
        IReadOnlyList<string>? tags = null)
    {
        TrendName = trendName ?? throw new ArgumentNullException(nameof(trendName));
        TrendId = string.IsNullOrWhiteSpace(trendId) ? trendName : trendId;
        Units = units;
        ExperimentId = experimentId;
        Tags = tags ?? [];
    }

    /// <summary>
    /// Pushes a spectral scan into the provider.
    /// </summary>
    public void Push(Spectrum1D scan)
    {
        ArgumentNullException.ThrowIfNull(scan);

        lock (_sync)
        {
            LatestScan = scan;
            LastUpdatedAt = scan.Timestamp;
            AppendBounded(_recentScans, scan);
        }

        _scans.OnNext(scan);
        _values.OnNext(new SpectralValue(scan));
    }

    /// <summary>
    /// Pushes a spectral matrix into the provider.
    /// </summary>
    public void Push(Spectrum2D matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        lock (_sync)
        {
            LatestMatrix = matrix;
            LastUpdatedAt = matrix.Timestamp;
            AppendBounded(_recentMatrices, matrix);
        }

        _matrices.OnNext(matrix);
        _values.OnNext(new SpectralMatrixValue(matrix));
    }

    public IReadOnlyList<Spectrum1D> GetRecentScans(int count)
    {
        if (count <= 0)
            return [];

        lock (_sync)
        {
            var skip = Math.Max(0, _recentScans.Count - count);
            return _recentScans.Skip(skip).ToArray();
        }
    }

    public IReadOnlyList<Spectrum2D> GetRecentMatrices(int count)
    {
        if (count <= 0)
            return [];

        lock (_sync)
        {
            var skip = Math.Max(0, _recentMatrices.Count - count);
            return _recentMatrices.Skip(skip).ToArray();
        }
    }

    private static void AppendBounded<T>(List<T> list, T value)
    {
        list.Add(value);
        if (list.Count > Capacity)
            list.RemoveAt(0);
    }

    public void Dispose()
    {
        IsOnline = false;
        _scans.OnCompleted();
        _matrices.OnCompleted();
        _values.OnCompleted();
        _scans.Dispose();
        _matrices.Dispose();
        _values.Dispose();
    }
}
