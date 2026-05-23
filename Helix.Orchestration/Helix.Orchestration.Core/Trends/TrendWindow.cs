namespace Helix.Orchestration.Core.Trends;

/// <summary>
/// A circular buffer maintaining a rolling window of trend values.
/// Used for real-time analytics: moving average, slope, stability detection.
/// </summary>
/// <typeparam name="T">The data type of trend values (must be comparable).</typeparam>
public sealed class TrendWindow<T> where T : struct, IComparable<T>
{
    private readonly object _lock = new();
    private readonly T[] _buffer;
    private int _head;
    private int _count;

    /// <summary>
    /// Gets the name of the trend.
    /// </summary>
    public string TrendName { get; }

    /// <summary>
    /// Gets the maximum number of values stored in the window.
    /// </summary>
    public int Capacity => _buffer.Length;

    /// <summary>
    /// Gets the current number of values in the window.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }

    /// <summary>
    /// Gets the timestamp of the most recent value.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public TrendWindow(string trendName, int capacity = 100)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        TrendName = trendName ?? throw new ArgumentNullException(nameof(trendName));
        _buffer = new T[capacity];
    }

    /// <summary>
    /// Adds a new value to the window (overwrites oldest if full).
    /// </summary>
    public void Add(T value)
    {
        lock (_lock)
        {
            _buffer[_head] = value;
            _head = (_head + 1) % _buffer.Length;

            if (_count < _buffer.Length)
            {
                _count++;
            }

            LastUpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Gets all values in the window (oldest to newest).
    /// </summary>
    public T[] GetValues()
    {
        lock (_lock)
        {
            if (_count == 0)
                return Array.Empty<T>();

            var result = new T[_count];
            var start = (_head - _count + _buffer.Length) % _buffer.Length;

            for (int i = 0; i < _count; i++)
            {
                result[i] = _buffer[(start + i) % _buffer.Length];
            }

            return result;
        }
    }

    /// <summary>
    /// Gets the most recent value in the window.
    /// </summary>
    public T? Latest()
    {
        lock (_lock)
        {
            if (_count == 0)
                return null;

            var index = (_head - 1 + _buffer.Length) % _buffer.Length;
            return _buffer[index];
        }
    }

    /// <summary>
    /// Clears all values from the window.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _head = 0;
            _count = 0;
        }
    }
}
