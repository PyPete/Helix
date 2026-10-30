using Helix.Orchestration.Agent.Events;
using Helix.Orchestration.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Experiments;

/// <summary>
/// Background service that drains the <see cref="IExperimentEventChannel"/> and
/// persists each event to the <see cref="IExperimentEventRepository"/>.
/// </summary>
public sealed class ExperimentEventPersistenceWorker(
    IExperimentEventChannel channel,
    IServiceScopeFactory scopeFactory,
    ILogger<ExperimentEventPersistenceWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in channel.Events.ReadAllAsync(stoppingToken))
        {
            // The command ledger is the canonical command/effect history. A
            // manual-action event is a post-commit notification projection and
            // must never create a second, mutable command fact here.
            if (evt is ManualActionEvent)
            {
                logger.LogDebug("Skipping manual-action projection; command evidence owns canonical history.");
                continue;
            }

            // Legacy lifecycle projections have no authority and are only useful
            // when scoped to a concrete experiment. Never manufacture an empty
            // experiment identity for an unscoped event.
            if (string.IsNullOrWhiteSpace(evt.ExperimentId))
            {
                logger.LogDebug("Skipping unscoped legacy event projection {Type}", evt.Type);
                continue;
            }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IExperimentEventRepository>();
                await repo.AppendAsync(MapToRecord(evt), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to persist experiment event {Type}", evt.Type);
            }
        }
    }

    private static ExperimentEventRecord MapToRecord(RunspaceEvent evt) => new(
        SequenceNum: 0,
        ExperimentId: evt.ExperimentId!,
        Phase: evt.ExperimentPhase ?? "none",
        EventType: evt.Type,
        RunspaceId: evt.RunspaceId ?? "",
        StepId: evt is StepStartedEvent ss ? ss.StepId
              : evt is StepCompletedEvent sc ? sc.StepId
              : null,
        StepLabel: evt is StepStartedEvent ssl ? ssl.StepLabel : null,
        IsManual: false,
        DeviceId: null,
        Capability: null,
        ParametersJson: null,
        ResultJson: null,
        OccurredAt: evt.Timestamp
    );
}
