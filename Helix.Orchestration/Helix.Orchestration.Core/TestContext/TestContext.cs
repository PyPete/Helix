using System.Collections.Concurrent;
using Helix.Orchestration.Core.Experiment;
using Helix.Orchestration.Core.Trends;

namespace Helix.Orchestration.Core.TestContext;

/// <summary>
/// Simulation-capable experiment context for unit tests and HIL test runs.
/// Delegates all <see cref="IExperimentContext"/> behavior to an inner
/// <see cref="ExperimentContext"/> and adds injection/fault hooks.
/// </summary>
public sealed class TestContext : ITestContext
{
    private readonly ExperimentContext _inner;
    private readonly List<TestEvent> _events = new();
    private readonly ConcurrentDictionary<string, Exception> _faults = new();
    private readonly ConcurrentDictionary<string, string> _virtualDevices = new();
    private readonly object _eventLock = new();

    // --- IExperimentContext pass-through ---

    /// <inheritdoc/>
    public string ExperimentId => _inner.ExperimentId;

    /// <inheritdoc/>
    public string Name => _inner.Name;

    /// <inheritdoc/>
    public ExperimentLifecycle Lifecycle => _inner.Lifecycle;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Metadata => _inner.Metadata;

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt => _inner.CreatedAt;

    /// <inheritdoc/>
    public DateTimeOffset? StartedAt => _inner.StartedAt;

    /// <inheritdoc/>
    public DateTimeOffset? CompletedAt => _inner.CompletedAt;

    /// <inheritdoc/>
    public Task TransitionToAsync(ExperimentLifecycle newState) => _inner.TransitionToAsync(newState);

    /// <inheritdoc/>
    public Task SetMetadataAsync(string key, object? value) => _inner.SetMetadataAsync(key, value);

    /// <inheritdoc/>
    public Task RecordDataAsync(string dataType, object data) => _inner.RecordDataAsync(dataType, data);

    // --- Construction ---

    public TestContext(string experimentId, string name)
    {
        _inner = new ExperimentContext(experimentId, name);
    }

    // --- ITestContext ---

    /// <inheritdoc/>
    public void InjectScalar<T>(string trendId, T value) where T : struct
    {
        RecordEvent(TestEventKind.ScalarInjected, trendId, $"{value}");
    }

    /// <inheritdoc/>
    public void InjectSpectralScan(string trendId, SpectralScan scan)
    {
        RecordEvent(TestEventKind.SpectralScanInjected, trendId, $"Type={scan.SpectralType}");
    }

    /// <inheritdoc/>
    public void InjectSpectralMatrix(string trendId, SpectralMatrix matrix)
    {
        RecordEvent(TestEventKind.SpectralMatrixInjected, trendId, $"Type={matrix.SpectralType}");
    }

    /// <inheritdoc/>
    public void RegisterVirtualDevice(string deviceId, string deviceType)
    {
        _virtualDevices[deviceId] = deviceType;
        RecordEvent(TestEventKind.DeviceRegistered, deviceId, $"Type={deviceType}");
    }

    /// <inheritdoc/>
    public void SimulateFault(string deviceId, Exception fault)
    {
        _faults[deviceId] = fault;
        RecordEvent(TestEventKind.FaultSimulated, deviceId, fault.Message);
    }

    /// <inheritdoc/>
    public void ClearFault(string deviceId)
    {
        _faults.TryRemove(deviceId, out _);
        RecordEvent(TestEventKind.FaultCleared, deviceId);
    }

    /// <inheritdoc/>
    public IReadOnlyList<TestEvent> GetEvents()
    {
        lock (_eventLock)
            return _events.ToArray();
    }

    /// <summary>
    /// Throws if a simulated fault is registered for <paramref name="deviceId"/>.
    /// Call from virtual device operations to honour fault simulation.
    /// </summary>
    public void ThrowIfFaulted(string deviceId)
    {
        if (_faults.TryGetValue(deviceId, out var ex))
            throw ex;
    }

    private void RecordEvent(TestEventKind kind, string id, string? detail = null)
    {
        lock (_eventLock)
            _events.Add(new TestEvent(kind, DateTimeOffset.UtcNow, id, detail));
    }
}
