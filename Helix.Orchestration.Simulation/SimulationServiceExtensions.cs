using Microsoft.Extensions.DependencyInjection;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Trends;
using Helix.Orchestration.Simulation.Devices;

namespace Helix.Orchestration.Simulation;

/// <summary>
/// DI registration extensions for the Helix simulation layer.
/// </summary>
public static class SimulationServiceExtensions
{
    /// <summary>
    /// Registers all simulation services: SimulatedTrendProvider, simulated devices (BackgroundService),
    /// and SimCapabilityInvoker.
    /// </summary>
    public static IServiceCollection AddHelixSimulation(this IServiceCollection services)
    {
        // Register SimulatedTrendProvider as both ITrendProvider and concrete type (singleton)
        services.AddSingleton<SimulatedTrendProvider>();
        services.AddSingleton<ITrendProvider>(sp => sp.GetRequiredService<SimulatedTrendProvider>());

        // Register simulated devices as hosted services (BackgroundService)
        services.AddSingleton<SimReactor>();
        services.AddHostedService(sp => sp.GetRequiredService<SimReactor>());

        services.AddSingleton<SimPump>();
        services.AddHostedService(sp => sp.GetRequiredService<SimPump>());

        services.AddSingleton<SimSpectrometer>();
        services.AddHostedService(sp => sp.GetRequiredService<SimSpectrometer>());

        // Register SimCapabilityInvoker as ICapabilityInvoker
        services.AddSingleton<ICapabilityInvoker, SimCapabilityInvoker>();

        return services;
    }
}
