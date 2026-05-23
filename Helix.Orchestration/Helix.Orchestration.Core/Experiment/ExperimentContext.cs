using System.Collections.Concurrent;

namespace Helix.Orchestration.Core.Experiment;

/// <summary>
/// Default implementation of IExperimentContext.
/// Manages experiment lifecycle, metadata, and data recording.
/// </summary>
public sealed class ExperimentContext : IExperimentContext
{
    private readonly ConcurrentDictionary<string, object?> _metadata = new();
    private readonly List<(string DataType, object Data, DateTimeOffset Timestamp)> _recordedData = new();
    private readonly object _lifecycleLock = new();

    /// <inheritdoc/>
    public string ExperimentId { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public ExperimentLifecycle Lifecycle { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Metadata => _metadata;

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; }

    /// <inheritdoc/>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Creates a new experiment context.
    /// </summary>
    public ExperimentContext(string experimentId, string name, IDictionary<string, object?>? initialMetadata = null)
    {
        ExperimentId = experimentId ?? throw new ArgumentNullException(nameof(experimentId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        CreatedAt = DateTimeOffset.UtcNow;
        Lifecycle = ExperimentLifecycle.Created;

        if (initialMetadata != null)
        {
            foreach (var kvp in initialMetadata)
            {
                _metadata[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <inheritdoc/>
    public Task TransitionToAsync(ExperimentLifecycle newState)
    {
        lock (_lifecycleLock)
        {
            // Validate state transition
            if (!IsValidTransition(Lifecycle, newState))
            {
                throw new InvalidOperationException($"Invalid lifecycle transition from {Lifecycle} to {newState}.");
            }

            Lifecycle = newState;

            // Update timestamps
            if (newState == ExperimentLifecycle.Running && !StartedAt.HasValue)
            {
                StartedAt = DateTimeOffset.UtcNow;
            }
            else if ((newState == ExperimentLifecycle.Completed || newState == ExperimentLifecycle.Failed) && !CompletedAt.HasValue)
            {
                CompletedAt = DateTimeOffset.UtcNow;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetMetadataAsync(string key, object? value)
    {
        _metadata[key] = value;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RecordDataAsync(string dataType, object data)
    {
        lock (_recordedData)
        {
            _recordedData.Add((dataType, data, DateTimeOffset.UtcNow));
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets all recorded data for this experiment.
    /// </summary>
    public IReadOnlyList<(string DataType, object Data, DateTimeOffset Timestamp)> GetRecordedData()
    {
        lock (_recordedData)
        {
            return _recordedData.ToArray();
        }
    }

    private static bool IsValidTransition(ExperimentLifecycle current, ExperimentLifecycle next)
    {
        return (current, next) switch
        {
            (ExperimentLifecycle.Created, ExperimentLifecycle.Running) => true,
            (ExperimentLifecycle.Running, ExperimentLifecycle.Suspended) => true,
            (ExperimentLifecycle.Running, ExperimentLifecycle.Completed) => true,
            (ExperimentLifecycle.Running, ExperimentLifecycle.Failed) => true,
            (ExperimentLifecycle.Suspended, ExperimentLifecycle.Running) => true,
            (ExperimentLifecycle.Suspended, ExperimentLifecycle.Failed) => true,
            _ => false
        };
    }
}
