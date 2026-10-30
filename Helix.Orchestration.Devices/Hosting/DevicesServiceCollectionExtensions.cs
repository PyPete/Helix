using System.Runtime.CompilerServices;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.FaultTolerance;
using Helix.Orchestration.Devices.Transports.CDll;
using Helix.Orchestration.Devices.Transports.Modbus;
using Helix.Orchestration.Devices.Transports.Opc;
using Helix.Orchestration.Devices.Transports.Serial;
using Helix.Orchestration.Devices.Transports.Tcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// DI Lifetime Note (B7):
// CapabilityRegistry is registered as Singleton in DeviceAdapterExtensions.AddMigratedDeviceAdapters().
// Transports and adapters registered via these extension methods must also be Singleton.
// Scoped or Transient transports captured inside a Singleton would produce a captive dependency.
// Hot-reconnect is handled by TransportWatchdog at the transport level, not by DI re-creation.

namespace Helix.Orchestration.Devices.Hosting;

/// <summary>
/// Service registration helpers for Helix device transports and adapters.
/// </summary>
public static class DevicesServiceCollectionExtensions
{
    public static IServiceCollection AddHelixSerialAdapter<TAdapter>(
        this IServiceCollection services,
        Action<SerialTransportOptions> configureOptions)
        where TAdapter : class, IHelixAdapter
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = BuildOptions(configureOptions);
        services.TryAddSingleton(ReconnectPolicy.Default);
        services.AddSingleton(options);
        services.AddSingleton<IDeviceTransport, SerialDeviceTransport>();
        services.AddSingleton<TAdapter>();
        services.AddSingleton<IHelixAdapter>(sp => sp.GetRequiredService<TAdapter>());
        services.AddSingleton<TransportWatchdog>();
        return services;
    }

    public static IServiceCollection AddHelixTcpAdapter<TAdapter>(
        this IServiceCollection services,
        Action<TcpTransportOptions> configureOptions)
        where TAdapter : class, IHelixAdapter
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = BuildOptions(configureOptions);
        services.TryAddSingleton(options.ReconnectPolicy ?? ReconnectPolicy.Default);
        services.AddSingleton(options);
        services.AddSingleton<IDeviceTransport, TcpDeviceTransport>();
        services.AddSingleton<TAdapter>();
        services.AddSingleton<IHelixAdapter>(sp => sp.GetRequiredService<TAdapter>());
        services.AddSingleton<TransportWatchdog>();
        return services;
    }

    public static IServiceCollection AddHelixModbusAdapter<TAdapter>(
        this IServiceCollection services,
        Action<ModbusTransportOptions> configureOptions)
        where TAdapter : class, IHelixAdapter
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = BuildOptions(configureOptions);
        services.TryAddSingleton(ReconnectPolicy.Default);
        services.AddSingleton(options);
        services.AddSingleton<IDeviceTransport, ModbusDeviceTransport>();
        services.AddSingleton<TAdapter>();
        services.AddSingleton<IHelixAdapter>(sp => sp.GetRequiredService<TAdapter>());
        services.AddSingleton<TransportWatchdog>();
        return services;
    }

    public static IServiceCollection AddHelixOpcAdapter<TAdapter>(
        this IServiceCollection services,
        Action<OpcTransportOptions> configureOptions)
        where TAdapter : class, IHelixAdapter
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = BuildOptions(configureOptions);
        services.TryAddSingleton(ReconnectPolicy.Default);
        services.AddSingleton(options);
        services.AddSingleton<IDeviceTransport, OpcDeviceTransport>();
        services.AddSingleton<TAdapter>();
        services.AddSingleton<IHelixAdapter>(sp => sp.GetRequiredService<TAdapter>());
        services.AddSingleton<TransportWatchdog>();
        return services;
    }

    public static IServiceCollection AddHelixCDllAdapter<TAdapter>(
        this IServiceCollection services,
        Action<CDllTransportOptions> configureOptions)
        where TAdapter : class, IHelixAdapter
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = BuildOptions(configureOptions);
        services.TryAddSingleton(ReconnectPolicy.Default);
        services.AddSingleton(options);
        services.AddSingleton<IDeviceTransport, CDllDeviceTransport>();
        services.AddSingleton<TAdapter>();
        services.AddSingleton<IHelixAdapter>(sp => sp.GetRequiredService<TAdapter>());
        services.AddSingleton<TransportWatchdog>();
        return services;
    }

    private static TOptions BuildOptions<TOptions>(Action<TOptions> configureOptions)
        where TOptions : class
    {
        var options = (TOptions)RuntimeHelpers.GetUninitializedObject(typeof(TOptions));
        configureOptions(options);
        return options;
    }
}