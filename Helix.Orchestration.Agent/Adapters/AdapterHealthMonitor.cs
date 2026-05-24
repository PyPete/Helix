using Helix.Orchestration.Agent.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Adapters;

/// <summary>
/// Periodically checks health of discovered adapters and emits observability signals.
/// </summary>
public sealed class AdapterHealthMonitor(AdapterRegistry registry, ILogger<AdapterHealthMonitor> logger) : BackgroundService
{
    private static class LogEvents
    {
        public static readonly EventId HealthCheckCycleStarted = new(2200, nameof(HealthCheckCycleStarted));
        public static readonly EventId AdapterHealthy = new(2201, nameof(AdapterHealthy));
        public static readonly EventId AdapterUnhealthy = new(2202, nameof(AdapterUnhealthy));
        public static readonly EventId AdapterHealthFailed = new(2203, nameof(AdapterHealthFailed));
    }

    /// <summary>
    /// Runs the adapter health check loop until shutdown.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(LogEvents.HealthCheckCycleStarted, "Running adapter health check cycle for {AdapterCount} adapters", registry.GetAll().Count);

            foreach (var (adapterId, proxy) in registry.GetAll())
            {
                try
                {
                    var health = await proxy.HealthCheckAsync(stoppingToken);
                    if (health.Healthy)
                    {
                        logger.LogInformation(LogEvents.AdapterHealthy, "Adapter {AdapterId} healthy", adapterId);
                    }
                    else
                    {
                        HelixTelemetry.AdapterHealthFailures.Add(1,
                            KeyValuePair.Create<string, object?>("adapter.id", adapterId));
                        logger.LogWarning(LogEvents.AdapterUnhealthy, "Adapter {AdapterId} unhealthy: {Message}", adapterId, health.Message);
                    }
                }
                catch (Exception ex)
                {
                    HelixTelemetry.AdapterHealthFailures.Add(1,
                        KeyValuePair.Create<string, object?>("adapter.id", adapterId));
                    logger.LogError(LogEvents.AdapterHealthFailed, ex, "Health check failed for adapter {AdapterId}", adapterId);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
