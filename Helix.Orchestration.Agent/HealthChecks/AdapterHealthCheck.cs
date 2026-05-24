using Helix.Orchestration.Agent.Adapters;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Helix.Orchestration.Agent.HealthChecks;

public sealed class AdapterHealthCheck(AdapterRegistry registry) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        foreach (var (adapterId, proxy) in registry.GetAll())
        {
            try
            {
                var status = await proxy.HealthCheckAsync(cancellationToken);
                if (!status.Healthy)
                    return HealthCheckResult.Degraded($"Adapter {adapterId} reported unhealthy: {status.Message}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Adapter {adapterId} health check failed.", ex);
            }
        }

        return HealthCheckResult.Healthy("All registered adapters are healthy.");
    }
}
