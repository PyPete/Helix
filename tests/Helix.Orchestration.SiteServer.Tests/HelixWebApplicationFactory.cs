using System.Collections.Concurrent;
using Helix.Orchestration.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Helix.Orchestration.SiteServer.Mqtt;

namespace Helix.Orchestration.SiteServer.Tests;

/// <summary>
/// Shared <see cref="WebApplicationFactory{TEntryPoint}"/> for Site Server integration tests.
/// Replaces infrastructure services that require external dependencies (MQTT broker,
/// PostgreSQL) with in-memory stubs so tests run without any live services.
/// </summary>
public sealed class HelixWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real MqttTrendBridge — no live broker in tests
            services.RemoveAll<IHostedService>();
            services.AddHostedService<NoOpHostedService>();

            // Replace PostgreSQL repositories with in-memory stubs — no connection string needed
            services.RemoveAll<IExperimentRepository>();
            services.RemoveAll<ICampaignRepository>();
            services.RemoveAll<DbConnectionFactory>();
            services.AddScoped<IExperimentRepository, InMemoryExperimentRepository>();
            services.AddScoped<ICampaignRepository, InMemoryCampaignRepository>();
        });
    }

    private sealed class NoOpHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryExperimentRepository : IExperimentRepository
    {
        private readonly ConcurrentDictionary<string, ExperimentRecord> _store = new();

        public Task SaveAsync(ExperimentRecord record, CancellationToken ct = default)
        {
            _store[record.RunspaceId] = record;
            return Task.CompletedTask;
        }

        public Task<ExperimentRecord?> GetByRunspaceIdAsync(string runspaceId, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(runspaceId, out var r) ? r : null);

        public Task<IReadOnlyList<ExperimentRecord>> GetByCampaignIdAsync(string campaignId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ExperimentRecord>>(
                _store.Values.Where(r => r.CampaignId == campaignId).ToList());
    }

    private sealed class InMemoryCampaignRepository : ICampaignRepository
    {
        private readonly ConcurrentDictionary<string, CampaignRecord> _store = new();

        public Task SaveAsync(CampaignRecord campaign, CancellationToken ct = default)
        {
            _store[campaign.CampaignId] = campaign;
            return Task.CompletedTask;
        }

        public Task<CampaignRecord?> GetByIdAsync(string campaignId, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(campaignId, out var r) ? r : null);

        public Task<IReadOnlyList<CampaignRecord>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CampaignRecord>>(_store.Values.ToList());

        public Task DeleteAsync(string campaignId, CancellationToken ct = default)
        {
            _store.TryRemove(campaignId, out _);
            return Task.CompletedTask;
        }
    }
}
