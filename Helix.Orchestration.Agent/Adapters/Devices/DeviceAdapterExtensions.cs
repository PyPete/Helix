using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Interfaces;
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
        AddPlan02NetworkAdapters(services);
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

        // Populate CapabilityRegistry with all device capabilities via the bridge.
        // Each adapter's capabilities are prefixed with a device namespace (e.g. "vapourtec.Start")
        // to avoid collisions when multiple adapters register identically-named capabilities.
        services.AddSingleton<CapabilityRegistry>(sp =>
        {
            var registry = new CapabilityRegistry();
            var logger = sp.GetRequiredService<ILogger<CapabilityRegistry>>();

            // VapourtecR4
            var adapter = sp.GetRequiredService<VapourtecR4>();
            foreach (var capability in adapter.GetCapabilities())
            {
                var key = $"vapourtec.{capability.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(adapter));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            logger.LogInformation(
                "CapabilityRegistry initialized with {Count} VapourtecR4 capabilities.",
                adapter.GetCapabilities().Count);

            // KnauerAzura (Phase 3)
            var knauer = sp.GetRequiredService<KnauerAzura>();
            foreach (var cap in knauer.GetCapabilities())
            {
                var key = $"knauer.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(knauer));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            // SystagOpcUaClient (Phase 3)
            var systag = sp.GetRequiredService<SystagOpcUaClient>();
            foreach (var cap in systag.GetCapabilities())
            {
                var key = $"systag.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(systag));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            // Microsaic (Plan 02)
            var microsaic = sp.GetRequiredService<Microsaic>();
            foreach (var cap in microsaic.GetCapabilities())
            {
                var key = $"microsaic.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(microsaic));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            // EasyMax (Plan 02)
            var easymax = sp.GetRequiredService<EasyMax>();
            foreach (var cap in easymax.GetCapabilities())
            {
                var key = $"easymax.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(easymax));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            // Fbrm (Plan 02)
            var fbrm = sp.GetRequiredService<Fbrm>();
            foreach (var cap in fbrm.GetCapabilities())
            {
                var key = $"fbrm.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(fbrm));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            // ReactIr (Plan 02)
            var reactir = sp.GetRequiredService<ReactIr>();
            foreach (var cap in reactir.GetCapabilities())
            {
                var key = $"reactir.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(reactir));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            return registry;
        });

        services.AddSingleton<ICapabilityInvoker>(sp => sp.GetRequiredService<CapabilityRegistry>());

        return services;
    }

    /// <summary>
    /// Registers the Plan 02 network-connected device adapters:
    /// Microsaic (TCP), EasyMax, Fbrm, ReactIr (OPC-UA via IMettlerOpcCommand).
    /// </summary>
    private static void AddPlan02NetworkAdapters(IServiceCollection services)
    {
        // ── Microsaic — TCP (10.0.0.1:10001, \n delimiter) ───────────────────
        // Uses a separate TcpDeviceTransport factory to avoid sharing the KnauerAzura transport.
        // Host and port are placeholders; override via appsettings in production.
        services.AddSingleton(sp => new TcpDeviceTransport(
            new TcpTransportOptions
            {
                Host = "10.0.0.1",                   // Microsaic default hostname from POP source
                Port = 10001,                         // placeholder — confirm from device config
                FramingMode = TcpFramingMode.Delimiter,
                DelimiterByte = 0x0A                  // '\n' — CmdEnd in POP source uses \n\n
            },
            sp.GetRequiredService<ILogger<TcpDeviceTransport>>()));
        services.AddSingleton(sp => new Microsaic(
            sp.GetRequiredService<TcpDeviceTransport>(),
            sp.GetRequiredService<ILogger<Microsaic>>()));

        // ── IMettlerOpcCommand — placeholder production implementation ────────
        // Replace with a concrete adapter before starting the application.
        services.AddSingleton<IMettlerOpcCommand>(_ =>
            throw new NotImplementedException(
                "IMettlerOpcCommand production implementation not yet registered. " +
                "Register a concrete OPC client adapter before starting the application."));

        // ── EasyMax — OPC-UA (opc.tcp://localhost:4841, placeholder endpoint) ─
        services.AddSingleton(sp => new EasyMax(
            new OpcDeviceTransport(
                new OpcTransportOptions
                {
                    EndpointUrl = "opc.tcp://localhost:4841",
                    AutoAcceptUntrustedCertificates = false
                },
                sp.GetRequiredService<ILogger<OpcDeviceTransport>>()),
            sp.GetRequiredService<IMettlerOpcCommand>(),
            sp.GetRequiredService<ILogger<EasyMax>>()));

        // ── Fbrm — OPC-UA (opc.tcp://localhost:4842, placeholder endpoint) ───
        services.AddSingleton(sp => new Fbrm(
            new OpcDeviceTransport(
                new OpcTransportOptions
                {
                    EndpointUrl = "opc.tcp://localhost:4842",
                    AutoAcceptUntrustedCertificates = false
                },
                sp.GetRequiredService<ILogger<OpcDeviceTransport>>()),
            sp.GetRequiredService<IMettlerOpcCommand>(),
            sp.GetRequiredService<ILogger<Fbrm>>()));

        // ── ReactIr — OPC-UA (opc.tcp://localhost:4843, placeholder endpoint) ─
        services.AddSingleton(sp => new ReactIr(
            new OpcDeviceTransport(
                new OpcTransportOptions
                {
                    EndpointUrl = "opc.tcp://localhost:4843",
                    AutoAcceptUntrustedCertificates = false
                },
                sp.GetRequiredService<ILogger<OpcDeviceTransport>>()),
            sp.GetRequiredService<IMettlerOpcCommand>(),
            sp.GetRequiredService<ILogger<ReactIr>>()));
    }
}
