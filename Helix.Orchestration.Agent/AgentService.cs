using Helix.Orchestration.Agent.Adapters;
using Helix.Orchestration.Agent.Streaming;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent;

/// <summary>
/// Orchestrates startup and shutdown of the Helix Agent runtime.
/// </summary>
public sealed class AgentService(
    IAdapterDiscovery adapterDiscovery,
    TrendAggregator trendAggregator,
    ILogger<AgentService> logger) : IHostedService
{
    private static class LogEvents
    {
        public static readonly EventId ServiceStarting = new(1000, nameof(ServiceStarting));
        public static readonly EventId ServiceStarted = new(1001, nameof(ServiceStarted));
        public static readonly EventId ServiceStopping = new(1002, nameof(ServiceStopping));
    }

    /// <summary>
    /// Starts adapter discovery and trend aggregation pipelines.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(LogEvents.ServiceStarting, "Starting Helix Agent service.");
        await adapterDiscovery.DiscoverAsync(cancellationToken);
        await trendAggregator.StartAsync(cancellationToken);
        logger.LogInformation(LogEvents.ServiceStarted, "Helix Agent service started.");
    }

    /// <summary>
    /// Stops the agent service.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(LogEvents.ServiceStopping, "Stopping Helix Agent service.");
        return Task.CompletedTask;
    }
}
