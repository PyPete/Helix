using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Adapters.Devices;

public static class DeviceAdapterExtensions
{
    public static IServiceCollection AddMigratedDeviceAdapters(this IServiceCollection services)
    {
        // Register VapourtecR4 serial transport options
        // PortName is a placeholder; override via appsettings or environment variable in production.
        services.AddSingleton(new SerialTransportOptions
        {
            PortName = "COM3",
            BaudRate = 19200,
            CommandTerminator = "\r\n"
        });
        services.AddSingleton<SerialDeviceTransport>();
        services.AddSingleton<VapourtecR4>();

        // Populate CapabilityRegistry with VapourtecR4 capabilities via the bridge
        services.AddSingleton<CapabilityRegistry>(sp =>
        {
            var registry = new CapabilityRegistry();
            var adapter = sp.GetRequiredService<VapourtecR4>();
            var logger = sp.GetRequiredService<ILogger<CapabilityRegistry>>();

            foreach (var capability in adapter.GetCapabilities())
            {
                var bridge = new HelixAdapterCapabilityBridge(adapter);
                registry.Register(capability.Name, bridge);
                logger.LogInformation("Registered capability: {CapabilityName}", capability.Name);
            }

            logger.LogInformation(
                "CapabilityRegistry initialized with {Count} VapourtecR4 capabilities.",
                adapter.GetCapabilities().Count);
            return registry;
        });

        services.AddSingleton<ICapabilityInvoker>(sp => sp.GetRequiredService<CapabilityRegistry>());

        return services;
    }
}
