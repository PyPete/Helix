using System.Reactive.Subjects;

namespace Helix.Orchestration.Core.Trends;

/// <summary>
/// In-memory scalar trend implementation backed by a rolling <see cref="TrendWindow{T}"/> and observable value stream.
/// </summary>
public sealed class InMemoryScalarTrend<T> : IScalarTrend<T>, IDisposable where T : struct, IComparable<T>
{
    private readonly Subject<TrendValue> _values = new();

    public string TrendId { get; }
    public string TrendName { get; }
    public TrendKind TrendKind => TrendKind.Scalar;
    public string? Units { get; }
    public string? ExperimentId { get; }
    public IReadOnlyList<string> Tags { get; }
    public bool IsOnline { get; private set; } = true;
    public DateTimeOffset LastUpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public IObservable<TrendValue> Values => _values;
    public TrendWindow<T> Window { get; }
    public T? Latest => Window.Latest();

    public InMemoryScalarTrend(
        string trendName,
        int capacity = 100,
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
        Window = new TrendWindow<T>(TrendName, capacity);
    }

    /// <summary>
    /// Pushes a new scalar value into the trend stream and rolling window.
    /// </summary>
    public void Push(T value)
    {
        Window.Add(value);
        LastUpdatedAt = Window.LastUpdatedAt;
        _values.OnNext(new ScalarValue<T>(value));
    }

    public void Dispose()
    {
        IsOnline = false;
        _values.OnCompleted();
        _values.Dispose();
    }
}
