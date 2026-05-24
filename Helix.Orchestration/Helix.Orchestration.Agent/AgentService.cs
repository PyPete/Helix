using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent;

public sealed class AgentService(ILogger<AgentService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Helix Agent service.");
        logger.LogInformation("Helix Agent service started.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Helix Agent service.");
        return Task.CompletedTask;
    }
}
