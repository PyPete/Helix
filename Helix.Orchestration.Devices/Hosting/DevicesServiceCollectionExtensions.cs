using System.Runtime.Serialization;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.FaultTolerance;
using Helix.Orchestration.Devices.Transports.CDll;
using Helix.Orchestration.Devices.Transports.Modbus;
using Helix.Orchestration.Devices.Transports.Opc;
using Helix.Orchestration.Devices.Transports.Serial;
using Helix.Orchestration.Devices.Transports.Tcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddScoped<IDeviceTransport, SerialDeviceTransport>();
        services.AddScoped<TAdapter>();
        services.AddScoped<IHelixAdapter>(sp => sp.GetRequiredService<TAdapter>());
        services.AddScoped<TransportWatchdog>();
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
        services.AddScoped<IDeviceTransport, TcpDeviceTransport>();
        services.AddScoped<TAdapter>();
        services.AddScoped<IHelixAdapter>(sp => sp.GetRequiredService<TAdapter>());
        services.AddScoped<TransportWatchdog>();
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
        services.AddScoped<IDeviceTransport, ModbusDeviceTransport>();
        services.AddScoped<TAdapter>();
        services.AddScoped<IHelixAdapter>(sp => sp.GetRequiredService<TAdapter>());
        services.AddScoped<TransportWatchdog>();
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
        services.AddScoped<IDeviceTransport, OpcDeviceTransport>();
        services.AddScoped<TAdapter>();
        services.AddScoped<IHelixAdapter>(sp => sp.GetRequiredService<TAdapter>());
        services.AddScoped<TransportWatchdog>();
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
        services.AddScoped<IDeviceTransport, CDllDeviceTransport>();
        services.AddScoped<TAdapter>();
        services.AddScoped<IHelixAdapter>(sp => sp.GetRequiredService<TAdapter>());
        services.AddScoped<TransportWatchdog>();
        return services;
    }

    private static TOptions BuildOptions<TOptions>(Action<TOptions> configureOptions)
        where TOptions : class
    {
        var options = (TOptions)FormatterServices.GetUninitializedObject(typeof(TOptions));
        configureOptions(options);
        return options;
    }
}