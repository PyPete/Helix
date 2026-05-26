using Helix.Orchestration.Agent.Transport;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Devices.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace Helix.Orchestration.Agent.Adapters.Devices;

public static class DeviceAdapterExtensions
{
    public static IServiceCollection AddMigratedDeviceAdapters(this IServiceCollection services)
    {
        services.AddTransient<ISerialTransport, SerialTransport>();
        services.AddTransient<IModbusTransport, ModbusTransport>();
        services.AddTransient<ITcpTransport, TcpTransport>();

        services.AddSingleton<CapabilityRegistry>(sp =>
        {
            var registry = new CapabilityRegistry();

            return registry;
        });

        services.AddSingleton<ICapabilityInvoker>(sp => sp.GetRequiredService<CapabilityRegistry>());

        return services;
    }
}
