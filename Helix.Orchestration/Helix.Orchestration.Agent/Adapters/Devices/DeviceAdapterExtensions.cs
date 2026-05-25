using Helix.Orchestration.Agent.Transport;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Devices.Drivers;
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
        services.AddTransient<IOpcTransport, OpcTransport>();

        services.AddTransient<VapourtecR4>();
        services.AddTransient<KnauerAzura>();
        services.AddTransient<SystagOpcUaClient>();

        services.AddSingleton<CapabilityRegistry>(sp =>
        {
            var registry = new CapabilityRegistry();

            registry.Register("device.vapourtec.r4", new DeviceCapabilityAdapter<VapourtecR4>(
                "device.vapourtec.r4",
                sp.GetRequiredService<VapourtecR4>()));

            registry.Register("device.hplc.knauer.azura", new DeviceCapabilityAdapter<KnauerAzura>(
                "device.hplc.knauer.azura",
                sp.GetRequiredService<KnauerAzura>()));

            registry.Register("device.reaction.systag", new DeviceCapabilityAdapter<SystagOpcUaClient>(
                "device.reaction.systag",
                sp.GetRequiredService<SystagOpcUaClient>()));

            return registry;
        });

        services.AddSingleton<ICapabilityInvoker>(sp => sp.GetRequiredService<CapabilityRegistry>());

        return services;
    }
}
