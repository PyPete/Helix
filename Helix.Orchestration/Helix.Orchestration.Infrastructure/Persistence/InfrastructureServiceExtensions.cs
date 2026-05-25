using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Helix.Orchestration.Infrastructure.Persistence;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddHelixInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HelixOrchestration")
            ?? throw new InvalidOperationException("Connection string 'HelixOrchestration' is not configured.");

        services.AddSingleton(new DbConnectionFactory(connectionString));
        services.AddScoped<IExperimentRepository, PostgresExperimentRepository>();
        services.AddScoped<ICampaignRepository, PostgresCampaignRepository>();

        return services;
    }
}
