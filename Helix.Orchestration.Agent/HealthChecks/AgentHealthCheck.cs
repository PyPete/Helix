using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Helix.Orchestration.Agent.HealthChecks;

public sealed class AgentHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(HealthCheckResult.Healthy("Agent service is running."));
}
