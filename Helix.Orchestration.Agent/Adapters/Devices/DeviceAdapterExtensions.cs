using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Transports.Null;
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
        AddPlan01SerialAdapters(services);
        AddPlan02NetworkAdapters(services);
        AddPlan03SpecialAdapters(services);
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

            // Plan 03 — special/facade drivers
            // AtlasReactionSystem
            var atlas = sp.GetRequiredService<AtlasReactionSystem>();
            foreach (var cap in atlas.GetCapabilities())
            {
                var key = $"atlasreactionsystem.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(atlas));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            // Crystalline
            var crystalline = sp.GetRequiredService<Crystalline>();
            foreach (var cap in crystalline.GetCapabilities())
            {
                var key = $"crystalline.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(crystalline));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            // IfmMasterController
            var ifm = sp.GetRequiredService<IfmMasterController>();
            foreach (var cap in ifm.GetCapabilities())
            {
                var key = $"ifmmastercontroller.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(ifm));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            // OceanViewUv
            var oceanViewUv = sp.GetRequiredService<OceanViewUv>();
            foreach (var cap in oceanViewUv.GetCapabilities())
            {
                var key = $"oceanviewuv.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(oceanViewUv));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            // OceanViewNir
            var oceanViewNir = sp.GetRequiredService<OceanViewNir>();
            foreach (var cap in oceanViewNir.GetCapabilities())
            {
                var key = $"oceanviewnir.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(oceanViewNir));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            // PicoTc08
            var pico = sp.GetRequiredService<PicoTc08>();
            foreach (var cap in pico.GetCapabilities())
            {
                var key = $"picotc08.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(pico));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            // NOTE: OceanViewDriver and PicoTc08LowLevelWrapper are NOT registered in the
            // CapabilityRegistry — they are native integration layers, not DeviceAdapterBase adapters.

            // SimulatorDevice — in-process simulator (always registered)
            var simulator = sp.GetRequiredService<SimulatorDevice>();
            foreach (var cap in simulator.GetCapabilities())
            {
                var key = $"simulator.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(simulator));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }

            // Plan 01 — 32 serial drivers
            RegisterPlan01Capabilities(sp, registry, logger);

            return registry;
        });

        services.AddSingleton<ICapabilityInvoker>(sp => sp.GetRequiredService<CapabilityRegistry>());

        return services;
    }

    /// <summary>
    /// Registers all 32 Plan 01 serial device adapters.
    /// Each driver uses its own inline SerialTransportOptions to avoid singleton collisions.
    /// PortName values are placeholders; override via appsettings or environment variables in production.
    /// </summary>
    private static void AddPlan01SerialAdapters(IServiceCollection services)
    {
        // AalborgDfm - BaudRate 9600, CmdTerminator \r
        services.AddSingleton(sp => new AalborgDfm(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM4", BaudRate = 9600, CommandTerminator = "\r" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<AalborgDfm>>()));

        // Alicat - BaudRate 19200, CmdTerminator \r
        services.AddSingleton(sp => new Alicat(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM5", BaudRate = 19200, CommandTerminator = "\r" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<Alicat>>()));

        // ArduinoConductivity - BaudRate 9600, CmdTerminator \r\n (no-suffix → uses \r\n as flush signal)
        services.AddSingleton(sp => new ArduinoConductivity(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM6", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<ArduinoConductivity>>()));

        // ArduinoLeak - BaudRate 9600
        services.AddSingleton(sp => new ArduinoLeak(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM7", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<ArduinoLeak>>()));

        // ArduinopH - BaudRate 9600
        services.AddSingleton(sp => new ArduinopH(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM8", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<ArduinopH>>()));

        // ArduinoPressure - BaudRate 9600
        services.AddSingleton(sp => new ArduinoPressure(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM9", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<ArduinoPressure>>()));

        // ArduinoSignalTower - BaudRate 9600
        services.AddSingleton(sp => new ArduinoSignalTower(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM10", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<ArduinoSignalTower>>()));

        // ArduinoTemp - BaudRate 9600
        services.AddSingleton(sp => new ArduinoTemp(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM11", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<ArduinoTemp>>()));

        // ArduinoValves - BaudRate 9600
        services.AddSingleton(sp => new ArduinoValves(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM12", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<ArduinoValves>>()));

        // AtlasSyringePump - BaudRate 57600, CmdTerminator \r\n
        services.AddSingleton(sp => new AtlasSyringePump(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM13", BaudRate = 57600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<AtlasSyringePump>>()));

        // Carino9 - BaudRate 9600
        services.AddSingleton(sp => new Carino9(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM14", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<Carino9>>()));

        // Cml18 - BaudRate 9600
        services.AddSingleton(sp => new Cml18(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM15", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<Cml18>>()));

        // ColeParmerTouch - BaudRate 115200, CmdTerminator \r
        services.AddSingleton(sp => new ColeParmerTouch(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM16", BaudRate = 115200, CommandTerminator = "\r" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<ColeParmerTouch>>()));

        // FoxyR1 - BaudRate 19200
        services.AddSingleton(sp => new FoxyR1(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM17", BaudRate = 19200, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<FoxyR1>>()));

        // Gynkotek - BaudRate 9600, no-suffix commands
        services.AddSingleton(sp => new Gynkotek(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM18", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<Gynkotek>>()));

        // Heidolph - BaudRate 9600
        services.AddSingleton(sp => new Heidolph(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM19", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<Heidolph>>()));

        // IkaDigitalHotPlate - BaudRate 9600, DataBits 7, Parity Even
        services.AddSingleton(sp => new IkaDigitalHotPlate(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM20", BaudRate = 9600, CommandTerminator = "\r\n", DataBits = 7, Parity = System.IO.Ports.Parity.Even }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<IkaDigitalHotPlate>>()));

        // IkaOverheadStirrer - BaudRate 9600
        services.AddSingleton(sp => new IkaOverheadStirrer(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM21", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<IkaOverheadStirrer>>()));

        // IkaOverheadStirrerOldDevice - BaudRate 9600, DataBits 7, Parity Even
        services.AddSingleton(sp => new IkaOverheadStirrerOldDevice(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM22", BaudRate = 9600, CommandTerminator = "\r\n", DataBits = 7, Parity = System.IO.Ports.Parity.Even }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<IkaOverheadStirrerOldDevice>>()));

        // IkaUltraTurrax - BaudRate 9600
        services.AddSingleton(sp => new IkaUltraTurrax(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM23", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<IkaUltraTurrax>>()));

        // KernPcb - BaudRate 9600
        services.AddSingleton(sp => new KernPcb(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM24", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<KernPcb>>()));

        // KnauerP2And4Pump - BaudRate 9600, CmdTerminator \r
        services.AddSingleton(sp => new KnauerP2And4Pump(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM25", BaudRate = 9600, CommandTerminator = "\r" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<KnauerP2And4Pump>>()));

        // LambdaDoser - BaudRate 2400, Parity Odd
        services.AddSingleton(sp => new LambdaDoser(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM26", BaudRate = 2400, CommandTerminator = "\r", Parity = System.IO.Ports.Parity.Odd }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<LambdaDoser>>()));

        // MasterFlexLsDigital - BaudRate 4800, DataBits 7, Parity Odd
        services.AddSingleton(sp => new MasterFlexLsDigital(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM27", BaudRate = 4800, CommandTerminator = "\r", DataBits = 7, Parity = System.IO.Ports.Parity.Odd }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<MasterFlexLsDigital>>()));

        // MettlerBalance - BaudRate 9600
        services.AddSingleton(sp => new MettlerBalance(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM28", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<MettlerBalance>>()));

        // NewEra1000 - BaudRate 19200, CmdTerminator \r
        services.AddSingleton(sp => new NewEra1000(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM29", BaudRate = 19200, CommandTerminator = "\r" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<NewEra1000>>()));

        // OrionVersaStarPro - BaudRate 115200, CmdTerminator \r
        services.AddSingleton(sp => new OrionVersaStarPro(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM30", BaudRate = 115200, CommandTerminator = "\r" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<OrionVersaStarPro>>()));

        // PilotOne - BaudRate 9600
        services.AddSingleton(sp => new PilotOne(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM31", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<PilotOne>>()));

        // SartoriusBalance - BaudRate 1200, DataBits 7, Parity Odd
        services.AddSingleton(sp => new SartoriusBalance(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM32", BaudRate = 1200, CommandTerminator = "\r\n", DataBits = 7, Parity = System.IO.Ports.Parity.Odd }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<SartoriusBalance>>()));

        // SuperDew3 - BaudRate 9600
        services.AddSingleton(sp => new SuperDew3(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM33", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<SuperDew3>>()));

        // Vario - BaudRate 9600
        services.AddSingleton(sp => new Vario(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM34", BaudRate = 9600, CommandTerminator = "\r\n" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<Vario>>()));

        // ViciActuator - BaudRate 9600, CmdTerminator \r
        services.AddSingleton(sp => new ViciActuator(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM35", BaudRate = 9600, CommandTerminator = "\r" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<ViciActuator>>()));

        // ViciMultiValveActuator - BaudRate 9600, CmdTerminator \r
        services.AddSingleton(sp => new ViciMultiValveActuator(
            new SerialDeviceTransport(new SerialTransportOptions { PortName = "COM36", BaudRate = 9600, CommandTerminator = "\r" }, sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
            sp.GetRequiredService<ILogger<ViciMultiValveActuator>>()));
    }

    private static void RegisterPlan01Capabilities(IServiceProvider sp, CapabilityRegistry registry, ILogger logger)
    {
        void RegisterDriver<T>(string prefix, T driver) where T : Helix.Orchestration.Devices.Abstractions.DeviceAdapterBase
        {
            foreach (var cap in driver.GetCapabilities())
            {
                var key = $"{prefix}.{cap.Name}";
                registry.Register(key, new HelixAdapterCapabilityBridge(driver));
                logger.LogInformation("Registered capability: {CapabilityName}", key);
            }
        }

        RegisterDriver("aalborgdfm", sp.GetRequiredService<AalborgDfm>());
        RegisterDriver("alicat", sp.GetRequiredService<Alicat>());
        RegisterDriver("arduinoconductivity", sp.GetRequiredService<ArduinoConductivity>());
        RegisterDriver("arduinoleak", sp.GetRequiredService<ArduinoLeak>());
        RegisterDriver("arduinoph", sp.GetRequiredService<ArduinopH>());
        RegisterDriver("arduinopressure", sp.GetRequiredService<ArduinoPressure>());
        RegisterDriver("arduinosignaltower", sp.GetRequiredService<ArduinoSignalTower>());
        RegisterDriver("arduinotemp", sp.GetRequiredService<ArduinoTemp>());
        RegisterDriver("arduinovalves", sp.GetRequiredService<ArduinoValves>());
        RegisterDriver("atlassyringepump", sp.GetRequiredService<AtlasSyringePump>());
        RegisterDriver("carino9", sp.GetRequiredService<Carino9>());
        RegisterDriver("cml18", sp.GetRequiredService<Cml18>());
        RegisterDriver("coleparmertouch", sp.GetRequiredService<ColeParmerTouch>());
        RegisterDriver("foxyr1", sp.GetRequiredService<FoxyR1>());
        RegisterDriver("gynkotek", sp.GetRequiredService<Gynkotek>());
        RegisterDriver("heidolph", sp.GetRequiredService<Heidolph>());
        RegisterDriver("ikadigitalhotplate", sp.GetRequiredService<IkaDigitalHotPlate>());
        RegisterDriver("ikaoverheadstirrer", sp.GetRequiredService<IkaOverheadStirrer>());
        RegisterDriver("ikaoverheadstirrerolddevice", sp.GetRequiredService<IkaOverheadStirrerOldDevice>());
        RegisterDriver("ikaultraturrax", sp.GetRequiredService<IkaUltraTurrax>());
        RegisterDriver("kernpcb", sp.GetRequiredService<KernPcb>());
        RegisterDriver("knauerp2and4pump", sp.GetRequiredService<KnauerP2And4Pump>());
        RegisterDriver("lambdadoser", sp.GetRequiredService<LambdaDoser>());
        RegisterDriver("masterflexlsdigital", sp.GetRequiredService<MasterFlexLsDigital>());
        RegisterDriver("mettlerbalance", sp.GetRequiredService<MettlerBalance>());
        RegisterDriver("newera1000", sp.GetRequiredService<NewEra1000>());
        RegisterDriver("orionversastarpro", sp.GetRequiredService<OrionVersaStarPro>());
        RegisterDriver("pilotone", sp.GetRequiredService<PilotOne>());
        RegisterDriver("sartoriusbalance", sp.GetRequiredService<SartoriusBalance>());
        RegisterDriver("superdew3", sp.GetRequiredService<SuperDew3>());
        RegisterDriver("vario", sp.GetRequiredService<Vario>());
        RegisterDriver("viciactuator", sp.GetRequiredService<ViciActuator>());
        RegisterDriver("vicimultivalveactuator", sp.GetRequiredService<ViciMultiValveActuator>());
    }

    /// <summary>
    /// Registers Plan 03 special/facade drivers (File I/O, SQL, IO-Link, CDll/P-Invoke).
    /// All facade drivers receive a NullDeviceTransport singleton as the stub transport.
    /// OceanViewDriver and PicoTc08LowLevelWrapper are registered as domain interface implementations only.
    /// </summary>
    private static void AddPlan03SpecialAdapters(IServiceCollection services)
    {
        // NullDeviceTransport — shared singleton stub for all facade drivers
        services.AddSingleton<NullDeviceTransport>();

        // SimulatorDevice — in-process simulator for development/UAT
        services.AddSingleton<SimulatorDevice>();

        // AtlasReactionSystem — File I/O facade
        // Null-object stubs — replace with concrete implementations when Atlas hardware is present.
        services.AddSingleton<IDirectoryInfo>(new NullDirectoryInfo());
        services.AddSingleton<IReadCsvFile>(new NullReadCsvFile());
        services.AddSingleton<IGetProcess>(new NullGetProcess());
        services.AddSingleton(sp => new AtlasReactionSystem(
            sp.GetRequiredService<IDirectoryInfo>(),
            sp.GetRequiredService<IReadCsvFile>(),
            sp.GetRequiredService<IGetProcess>(),
            sp.GetRequiredService<NullDeviceTransport>(),
            sp.GetRequiredService<ILogger<AtlasReactionSystem>>()));

        // Crystalline — SQL facade (null stub until Crystalline SQL client is linked)
        services.AddSingleton<ICrystallineSqlClient>(new NullCrystallineSqlClient());
        services.AddSingleton(sp => new Crystalline(
            sp.GetRequiredService<ICrystallineSqlClient>(),
            sp.GetRequiredService<NullDeviceTransport>(),
            sp.GetRequiredService<ILogger<Crystalline>>()));

        // IfmMasterController — IO-Link HTTP facade (null stubs until IFM client is linked)
        services.AddSingleton<IPopIoLinkClient>(new NullPopIoLinkClient());
        services.AddSingleton<IFile>(new NullFile());
        services.AddSingleton(sp => new IfmMasterController(
            sp.GetRequiredService<IPopIoLinkClient>(),
            sp.GetRequiredService<IFile>(),
            sp.GetRequiredService<NullDeviceTransport>(),
            sp.GetRequiredService<ILogger<IfmMasterController>>()));

        // OceanViewDriver — CDll IOceanViewDriver implementation (native integration layer)
        services.AddSingleton<IOceanViewDriver, OceanViewDriver>();

        // OceanViewUv — wraps IOceanViewDriver
        services.AddSingleton(sp => new OceanViewUv(
            sp.GetRequiredService<IOceanViewDriver>(),
            sp.GetRequiredService<NullDeviceTransport>(),
            sp.GetRequiredService<ILogger<OceanViewUv>>()));

        // OceanViewNir — wraps IOceanViewDriver
        services.AddSingleton(sp => new OceanViewNir(
            sp.GetRequiredService<IOceanViewDriver>(),
            sp.GetRequiredService<NullDeviceTransport>(),
            sp.GetRequiredService<ILogger<OceanViewNir>>()));

        // PicoTc08LowLevelWrapper — IPicoApi implementation (native integration layer)
        services.AddSingleton<IPicoApi, PicoTc08LowLevelWrapper>();

        // PicoTc08 — wraps IPicoApi
        services.AddSingleton(sp => new PicoTc08(
            sp.GetRequiredService<IPicoApi>(),
            sp.GetRequiredService<NullDeviceTransport>(),
            sp.GetRequiredService<ILogger<PicoTc08>>()));
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

        // ── IMettlerOpcCommand — null-object stub (no OPC client linked yet) ─
        // EasyMax/Fbrm/ReactIr require this interface at DI construction time.
        // The stub lets the Agent start without an OPC server; replace with a
        // concrete OpcUa client adapter when Mettler hardware is present.
        services.AddSingleton<IMettlerOpcCommand>(new NullMettlerOpcCommand());

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

// ── Null-object stubs for unlinked external dependencies ─────────────────────
// These allow the Agent to start without every third-party client installed.
// Replace each with a concrete adapter when the corresponding hardware is present.

internal sealed class NullDirectoryInfo : IDirectoryInfo
{
    public bool CheckFolderExists(string path) => false;
    public IEnumerable<IFileInfo> GetFiles() => [];
    public void SetPath(string path) { }
}

internal sealed class NullReadCsvFile : IReadCsvFile
{
    public System.Data.DataTable Read(string filePath) => new();
}

internal sealed class NullGetProcess : IGetProcess
{
    public (bool ProcessFound, string MainWindowTitle) FindProcessByName(string processName)
        => (false, string.Empty);
}

internal sealed class NullCrystallineSqlClient : ICrystallineSqlClient
{
    public bool CreateSession(string endPoint) => false;
    public bool CloseSession() => false;
    public bool SubscribeToReactor(string reactor) => false;
    public bool ReactorLive(string reactor) => false;
    public IEnumerable<(string TrendName, double Value, string Unit)> GetRuntimeDataForReactor(string reactor) => [];
    public (Helix.Orchestration.Devices.Model.DeviceResult Result, byte[]? Image) GrabImage(string reactor)
        => (Helix.Orchestration.Devices.Model.DeviceResult.Fail("No Crystalline client registered"), null);
    public IReadOnlyList<(string Name, string Value)> GetDeviceInfo() => [];
    public IReadOnlyList<(string Name, string Value)> GetDeviceInfo(string reactor) => [];
    public Dictionary<int, (string Name, string Unit)> FindVariableNames(string reactor) => [];
    public IEnumerable<string> FindReactors() => [];
}

internal sealed class NullPopIoLinkClient : IPopIoLinkClient
{
    public Task<IEnumerable<(string DeviceId, byte Port)>> FindActiveSensorsFromEndPointAsync(string endPoint)
        => Task.FromResult(Enumerable.Empty<(string, byte)>());
    public Task<bool> CreateSessionAsync(string endPoint, byte portNumber) => Task.FromResult(false);
    public void CloseSession() { }
    public List<(string Id, ushort Index, double Gradient, double Offset)> GetAllVariablesForDevice() => [];
    public Task<(bool Success, object? Data)> GetVariableDataAtIndexAsync(ushort index)
        => Task.FromResult((false, (object?)null));
    public Task<List<(string Name, object Value)>> GetAllProcessDataForDeviceAsync()
        => Task.FromResult(new List<(string, object)>());
    public (string ProductId, uint DeviceId, ushort VendorId) GetDeviceIdentifiers()
        => (string.Empty, 0u, 0);
}

internal sealed class NullFile : IFile
{
    public string ReadAllText(string path) => string.Empty;
    public Task<string> ReadAllTextAsync(string path) => Task.FromResult(string.Empty);
}

/// <summary>
/// Null-object implementation of IMettlerOpcCommand. Returns safe no-op defaults
/// so EasyMax/Fbrm/ReactIr can be registered without an OPC server present.
/// Replace with a concrete OpcUa client when Mettler hardware is available.
/// </summary>
internal sealed class NullMettlerOpcCommand : IMettlerOpcCommand
{
    public (bool flag, OpcConnectionEnum connectionEnum, string reason) CreateSession(string endPoint)
        => (false, OpcConnectionEnum.NotConnected, "No OPC client registered");

    public bool CloseSession() => false;

    public T GetAtNode<T>(string node) => default!;

    public void CallMethod(string objectId, string methodId, params object[] parameters) { }

    public IEnumerable<string> ConnectedDevices(string parentNode, string deviceName, bool stripBlank = true)
        => [];

    public void SubscribeToNode(string node, OpcDataChangeReceivedEventHandler callback) { }

    public List<Tuple<double[], double[]>> ReturnRaw(IEnumerable<string> spectraNodes) => [];

    public List<(string name, double value, string unit)> GetTrendsAtNode(string trendNode) => [];
}
