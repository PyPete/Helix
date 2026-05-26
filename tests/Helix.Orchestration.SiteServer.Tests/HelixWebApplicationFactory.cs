using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Helix.Orchestration.SiteServer.Mqtt;

namespace Helix.Orchestration.SiteServer.Tests;

/// <summary>
/// Shared <see cref="WebApplicationFactory{TEntryPoint}"/> for Site Server integration tests.
/// Replaces <see cref="MqttTrendBridge"/> with a no-op hosted service so tests never require
/// a live MQTT broker.
/// </summary>
public sealed class HelixWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real MqttTrendBridge hosted service
            services.RemoveAll<IHostedService>();

            // Re-register a no-op stand-in so DI is satisfied but no broker connection is attempted
            services.AddHostedService<NoOpHostedService>();
        });
    }

    private sealed class NoOpHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
