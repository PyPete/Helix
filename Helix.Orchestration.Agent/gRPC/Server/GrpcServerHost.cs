using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.gRPC.Server;

public sealed class GrpcServerHost(ILogger<GrpcServerHost> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("gRPC server host registration complete. ASP.NET Core endpoint mapping is configured in Program.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("gRPC server host stopping.");
        return Task.CompletedTask;
    }
}
