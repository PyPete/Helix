using System.Collections.Concurrent;
using Helix.Orchestration.Contracts.Adapters;

namespace Helix.Orchestration.Agent.Streaming;

/// <summary>
/// In-memory concurrent buffer for transient trend value staging.
/// </summary>
public sealed class TrendBuffer
{
    private readonly ConcurrentQueue<TrendValue> _queue = new();

    /// <summary>
    /// Enqueues a trend value into the buffer.
    /// </summary>
    public void Enqueue(TrendValue value) => _queue.Enqueue(value);

    /// <summary>
    /// Attempts to dequeue the next trend value from the buffer.
    /// </summary>
    public bool TryDequeue(out TrendValue? value)
    {
        var success = _queue.TryDequeue(out var item);
        value = item;
        return success;
    }
}
