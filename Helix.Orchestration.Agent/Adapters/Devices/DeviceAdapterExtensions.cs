using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Transports.Opc;
using Helix.Orchestration.Devices.Transports.Serial;
using Helix.Orchestration.Devices.Transports.Tcp;
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

        // KnauerAzura — Pump TCP connection (192.168.1.101:10001, \r delimiter)
        services.AddSingleton(sp => new TcpDeviceTransport(
            new TcpTransportOptions
            {
                Host = "192.168.1.101",
                Port = 10001,
                FramingMode = TcpFramingMode.Delimiter,
                DelimiterByte = 0x0D   // \r terminator
            },
            sp.GetRequiredService<ILogger<TcpDeviceTransport>>()));
        services.AddSingleton(sp => new KnauerAzura(
            sp.GetRequiredService<TcpDeviceTransport>(),
            sp.GetRequiredService<ILogger<KnauerAzura>>()));

        // SystagOpcUaClient — OPC UA endpoint (override in appsettings for production)
        services.AddSingleton(sp => new OpcDeviceTransport(
            new OpcTransportOptions
            {
                EndpointUrl = "opc.tcp://localhost:4840",
                AutoAcceptUntrustedCertificates = false
            },
            sp.GetRequiredService<ILogger<OpcDeviceTransport>>()));
        services.AddSingleton(sp => new SystagOpcUaClient(
            sp.GetRequiredService<OpcDeviceTransport>(),
            sp.GetRequiredService<ILogger<SystagOpcUaClient>>()));

        // Populate CapabilityRegistry with all device capabilities via the bridge
        services.AddSingleton<CapabilityRegistry>(sp =>
        {
            var registry = new CapabilityRegistry();
            var logger = sp.GetRequiredService<ILogger<CapabilityRegistry>>();

            // VapourtecR4
            var adapter = sp.GetRequiredService<VapourtecR4>();
            foreach (var capability in adapter.GetCapabilities())
            {
                var bridge = new HelixAdapterCapabilityBridge(adapter);
                registry.Register(capability.Name, bridge);
                logger.LogInformation("Registered capability: {CapabilityName}", capability.Name);
            }

            logger.LogInformation(
                "CapabilityRegistry initialized with {Count} VapourtecR4 capabilities.",
                adapter.GetCapabilities().Count);

            // KnauerAzura (Phase 3)
            var knauer = sp.GetRequiredService<KnauerAzura>();
            foreach (var cap in knauer.GetCapabilities())
            {
                registry.Register(cap.Name, new HelixAdapterCapabilityBridge(knauer));
                logger.LogInformation("Registered capability: {CapabilityName}", cap.Name);
            }

            // SystagOpcUaClient (Phase 3)
            var systag = sp.GetRequiredService<SystagOpcUaClient>();
            foreach (var cap in systag.GetCapabilities())
            {
                registry.Register(cap.Name, new HelixAdapterCapabilityBridge(systag));
                logger.LogInformation("Registered capability: {CapabilityName}", cap.Name);
            }

            return registry;
        });

        services.AddSingleton<ICapabilityInvoker>(sp => sp.GetRequiredService<CapabilityRegistry>());

        return services;
    }
}
