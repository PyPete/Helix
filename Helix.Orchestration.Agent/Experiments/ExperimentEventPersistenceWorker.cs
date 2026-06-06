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
        IsManual: evt is ManualActionEvent,
        DeviceId: evt is ManualActionEvent ma ? ma.DeviceId : null,
        Capability: evt is ManualActionEvent mac ? mac.Capability : null,
        ParametersJson: evt is ManualActionEvent map
            ? System.Text.Json.JsonSerializer.Serialize(map.Parameters) : null,
        ResultJson: evt is ManualActionEvent mar && mar.Result != null
            ? System.Text.Json.JsonSerializer.Serialize(mar.Result) : null,
        OccurredAt: evt.Timestamp
    );
}
