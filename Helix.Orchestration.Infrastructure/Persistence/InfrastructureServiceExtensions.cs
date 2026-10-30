using Helix.Orchestration.Infrastructure.Storage;
using Helix.Orchestration.Core.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Helix.Orchestration.Infrastructure.Persistence;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddHelixInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DbConnectionFactory>(sp =>
            new DbConnectionFactory(
                configuration.GetConnectionString("HelixOrchestration")
                ?? throw new InvalidOperationException("Connection string 'HelixOrchestration' is not configured.")));
        services.AddScoped<IExperimentRepository, PostgresExperimentRepository>();
        services.AddScoped<ICampaignRepository, PostgresCampaignRepository>();
        services.AddScoped<IExperimentEventRepository, PostgresExperimentEventRepository>();
        services.AddScoped<ICommandLedger, PostgresCommandLedger>();
        services.AddSingleton<IExecutionControlStore, PostgresExecutionControlStore>();
        services.AddSingleton<ISimulatorStateStore, PostgresSimulatorStateStore>();
        services.AddSingleton<ISimulatorObservation>(sp => sp.GetRequiredService<ISimulatorStateStore>());

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

