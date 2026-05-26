using Helix.Orchestration.Core.Capabilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Adapters.Devices;

public static class DeviceAdapterExtensions
{
    public static IServiceCollection AddMigratedDeviceAdapters(this IServiceCollection services)
    {
        services.AddSingleton<CapabilityRegistry>(sp =>
        {
            var registry = new CapabilityRegistry();
            var logger = sp.GetRequiredService<ILogger<CapabilityRegistry>>();
            logger.LogInformation(
                "CapabilityRegistry initialized — 0 adapters registered (Phase 1 placeholder). " +
                "Phase 2 will register real device adapters.");
            return registry;
        });

        services.AddSingleton<ICapabilityInvoker>(sp => sp.GetRequiredService<CapabilityRegistry>());

        return services;
    }
}
