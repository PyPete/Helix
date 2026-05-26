using Microsoft.Extensions.Options;

namespace Helix.Orchestration.SiteServer.Agent;

public static class AgentProxyServiceExtensions
{
    public static IServiceCollection AddHelixAgentProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AgentProxyOptions>(configuration.GetSection(AgentProxyOptions.SectionName));
        services.AddSingleton<IAgentControlClient, GrpcAgentControlClient>();

        services.AddSingleton<IAgentProxy>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AgentProxyOptions>>().Value;

            return options.Mode.Equals("grpc", StringComparison.OrdinalIgnoreCase)
                ? ActivatorUtilities.CreateInstance<GrpcAgentProxy>(sp)
                : ActivatorUtilities.CreateInstance<SimAgentProxy>(sp);
        });

        return services;
    }
}
