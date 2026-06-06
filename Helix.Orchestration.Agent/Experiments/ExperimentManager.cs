using System.Collections.Concurrent;
using Helix.Orchestration.Agent.Events;
using Helix.Orchestration.Core.Experiment;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Experiments;

/// <summary>
/// In-process singleton that manages experiment lifecycle state and publishes
/// experiment lifecycle events to the main runspace event bus.
/// </summary>
public sealed class ExperimentManager(
    IRunspaceEventEmitter emitter,
    ILogger<ExperimentManager> logger) : IExperimentManager
{
    private readonly ConcurrentDictionary<string, ExperimentContext> _experiments = new();
    private volatile string? _activeExperimentId;

    /// <inheritdoc />
    public Task<ExperimentContext> StartAsync(ExperimentStartRequest request, CancellationToken ct = default)
    {
        var experimentId = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        var context = new ExperimentContext(
            ExperimentId: experimentId,
            Name: request.Name,
            Lifecycle: ExperimentLifecycle.Running,
            ActivePhase: ExperimentPhase.Main,
            ActiveRunspaceId: null,
            CreatedAt: now,
            StartedAt: now,
            EndedAt: null
        );

        _experiments[experimentId] = context;
        _activeExperimentId = experimentId;

        emitter.Publish(new ExperimentStartedEvent(experimentId, request.Name));
        emitter.Publish(new ExperimentPhaseChangedEvent(experimentId, "main"));

        logger.LogInformation("Experiment {ExperimentId} '{Name}' started.", experimentId, request.Name);
        return Task.FromResult(context);
    }

    /// <inheritdoc />
    public ExperimentContext? Get(string experimentId) =>
        _experiments.TryGetValue(experimentId, out var ctx) ? ctx : null;

    /// <inheritdoc />
    public IReadOnlyList<ExperimentContext> ListActive() =>
        _experiments.Values
            .Where(c => c.Lifecycle is ExperimentLifecycle.Running or ExperimentLifecycle.Suspended)
            .ToList();

    /// <inheritdoc />
    public ExperimentContext? GetActive() =>
        _activeExperimentId is { } id ? Get(id) : null;

    /// <inheritdoc />
    public Task PauseAsync(string experimentId, CancellationToken ct = default)
    {
        if (_experiments.TryGetValue(experimentId, out var ctx))
        {
            _experiments[experimentId] = ctx with { Lifecycle = ExperimentLifecycle.Suspended };
            logger.LogInformation("Experiment {ExperimentId} paused.", experimentId);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResumeAsync(string experimentId, CancellationToken ct = default)
    {
        if (_experiments.TryGetValue(experimentId, out var ctx))
        {
            _experiments[experimentId] = ctx with { Lifecycle = ExperimentLifecycle.Running };
            logger.LogInformation("Experiment {ExperimentId} resumed.", experimentId);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AbortAsync(string experimentId, CancellationToken ct = default)
    {
        if (_experiments.TryGetValue(experimentId, out var ctx))
        {
            _experiments[experimentId] = ctx with
            {
                Lifecycle = ExperimentLifecycle.Failed,
                EndedAt = DateTimeOffset.UtcNow
            };
        }

        if (_activeExperimentId == experimentId)
            _activeExperimentId = null;

        emitter.Publish(new ExperimentFailedEvent(experimentId, "Aborted by operator"));
        logger.LogInformation("Experiment {ExperimentId} aborted.", experimentId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CompleteAsync(string experimentId, CancellationToken ct = default)
    {
        if (_experiments.TryGetValue(experimentId, out var ctx))
        {
            _experiments[experimentId] = ctx with
            {
                Lifecycle = ExperimentLifecycle.Completed,
                EndedAt = DateTimeOffset.UtcNow
            };
        }

        if (_activeExperimentId == experimentId)
            _activeExperimentId = null;

        emitter.Publish(new ExperimentCompletedEvent(experimentId));
        logger.LogInformation("Experiment {ExperimentId} completed.", experimentId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void SetActiveRunspace(string experimentId, string runspaceId)
    {
        if (_experiments.TryGetValue(experimentId, out var ctx))
        {
            _experiments[experimentId] = ctx with { ActiveRunspaceId = runspaceId };
            logger.LogInformation(
                "Experiment {ExperimentId} associated with runspace {RunspaceId}.",
                experimentId, runspaceId);
        }
    }
}
