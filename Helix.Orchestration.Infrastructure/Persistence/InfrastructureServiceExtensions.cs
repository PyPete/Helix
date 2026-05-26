using Helix.Orchestration.Infrastructure.Storage;
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

    /// <summary>
    /// Registers MinIO object storage services.
    /// </summary>
    public static IServiceCollection AddMinioStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var minioOptions = configuration.GetSection("Minio").Get<Storage.MinioOptions>()
            ?? throw new InvalidOperationException("Minio configuration section is missing or invalid.");

        services.AddSingleton<Storage.IObjectStorageSink>(
            _ => new Storage.MinioObjectStorageSink(minioOptions));

        return services;
    }
}
