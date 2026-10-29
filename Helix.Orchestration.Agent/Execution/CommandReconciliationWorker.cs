using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Execution;

/// <summary>Recovery worker placeholder that deliberately never replays uncertain commands.</summary>
public sealed class CommandReconciliationWorker(ILogger<CommandReconciliationWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Command reconciliation started; uncertain effects are held pending observation.");
        return Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
