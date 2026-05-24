using Helix.Orchestration.Agent.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Helix.Orchestration.Agent.Adapters;

/// <summary>
/// Discovers and registers adapter endpoints from agent configuration.
/// </summary>
public sealed class AdapterDiscoveryService(
    IOptions<AgentConfiguration> config,
    AdapterRegistry registry,
    ILogger<AdapterDiscoveryService> logger,
    ILogger<AdapterProxy> adapterProxyLogger) : IAdapterDiscovery
{
    private static class LogEvents
    {
        public static readonly EventId DiscoveryStarted = new(2100, nameof(DiscoveryStarted));
        public static readonly EventId AdapterRegistered = new(2101, nameof(AdapterRegistered));
        public static readonly EventId AdapterRegistrationFailed = new(2102, nameof(AdapterRegistrationFailed));
        public static readonly EventId DiscoveryCompleted = new(2103, nameof(DiscoveryCompleted));
    }

    /// <summary>
    /// Executes configured adapter discovery by probing each configured endpoint.
    /// </summary>
    public async Task DiscoverAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation(LogEvents.DiscoveryStarted, "Starting adapter discovery for {AdapterCount} configured adapters", config.Value.Adapters.Count);

        foreach (var adapter in config.Value.Adapters)
        {
            try
            {
                var proxy = new AdapterProxy(adapter.AdapterId, adapter.Endpoint, adapterProxyLogger);
                _ = await proxy.GetCapabilitiesAsync(cancellationToken);
                registry.Upsert(adapter.AdapterId, proxy);
                logger.LogInformation(LogEvents.AdapterRegistered, "Registered adapter {AdapterId} at {Endpoint}", adapter.AdapterId, adapter.Endpoint);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to discover adapter {AdapterId} at {Endpoint}", adapter.AdapterId, adapter.Endpoint);
            }
        }

        logger.LogInformation(LogEvents.DiscoveryCompleted, "Adapter discovery pass completed");
    }
}
