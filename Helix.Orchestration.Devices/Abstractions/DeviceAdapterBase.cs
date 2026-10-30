using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Abstractions;

/// <summary>
/// Base class for device adapters that simplifies capability registration and transport lifecycle management.
/// </summary>
public abstract class DeviceAdapterBase : IHelixAdapter
{
    private readonly IDeviceTransport _transport;
    private readonly ILogger _logger;
    private readonly Dictionary<string, Func<JsonElement, string?, CancellationToken, Task<CapabilityInvocationResult>>> _capabilityHandlers = new();
    private readonly List<CapabilityDescriptor> _capabilities = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceAdapterBase"/> class.
    /// </summary>
    /// <param name="transport">The underlying device transport.</param>
    /// <param name="logger">Logger instance.</param>
    protected DeviceAdapterBase(IDeviceTransport transport, ILogger logger)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var builder = new CapabilityDispatchBuilder(_capabilityHandlers, _capabilities);
        RegisterCapabilities(builder);
    }

    /// <summary>
    /// Gets the underlying transport for derived classes.
    /// </summary>
    protected IDeviceTransport Transport => _transport;

    /// <summary>
    /// Gets the logger for derived classes.
    /// </summary>
    protected ILogger Logger => _logger;

    /// <summary>
    /// Registers capabilities using the provided builder.
    /// </summary>
    /// <param name="builder">The capability dispatch builder.</param>
    protected abstract void RegisterCapabilities(CapabilityDispatchBuilder builder);

    /// <inheritdoc/>
    public IReadOnlyList<CapabilityDescriptor> GetCapabilities() => _capabilities.AsReadOnly();

    /// <inheritdoc/>
    public async Task<CapabilityInvocationResult> InvokeCapabilityAsync(
        string name,
        JsonElement parameters,
        string? leaseId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_capabilityHandlers.TryGetValue(name, out var handler))
        {
            _logger.LogWarning("Capability '{CapabilityName}' not found", name);
            return CapabilityInvocationResult.Failed($"Capability '{name}' not found");
        }

        try
        {
            return await handler(parameters, leaseId, cancellationToken);
        }
        catch (TransportFaultException ex)
        {
            _logger.LogError(ex, "Transport fault while invoking capability '{CapabilityName}'", name);
            return CapabilityInvocationResult.Failed($"Transport fault: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking capability '{CapabilityName}'", name);
            return CapabilityInvocationResult.Failed($"Invocation error: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public virtual async IAsyncEnumerable<TrendSample> StreamTrendsAsync(CancellationToken cancellationToken = default)
    {
        // Default implementation: no trends
        await Task.CompletedTask;
        yield break;
    }

    /// <inheritdoc/>
    public virtual async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        return _transport.Status == TransportStatus.Connected;
    }

    /// <summary>
    /// Starts the adapter by connecting the underlying transport.
    /// </summary>
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting adapter with transport");
        await _transport.ConnectAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogInformation("Disposing adapter and transport");
        await _transport.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Builder for registering capabilities in a device adapter.
/// </summary>
public sealed class CapabilityDispatchBuilder
{
    private readonly Dictionary<string, Func<JsonElement, string?, CancellationToken, Task<CapabilityInvocationResult>>> _handlers;
    private readonly List<CapabilityDescriptor> _descriptors;

    internal CapabilityDispatchBuilder(
        Dictionary<string, Func<JsonElement, string?, CancellationToken, Task<CapabilityInvocationResult>>> handlers,
        List<CapabilityDescriptor> descriptors)
    {
        _handlers = handlers;
        _descriptors = descriptors;
    }

    /// <summary>
    /// Registers a capability with a handler.
    /// </summary>
    public CapabilityDispatchBuilder Register(
        CapabilityDescriptor descriptor,
        Func<JsonElement, string?, CancellationToken, Task<CapabilityInvocationResult>> handler)
    {
        _descriptors.Add(descriptor);
        _handlers[descriptor.Name] = handler;
        return this;
    }

    /// <summary>
    /// Registers a simple capability with no parameters.
    /// </summary>
    public CapabilityDispatchBuilder RegisterSimple(
        string name,
        string description,
        Func<CancellationToken, Task<CapabilityInvocationResult>> handler)
    {
        var descriptor = CapabilityDescriptor.Create(name, description);
        _descriptors.Add(descriptor);
        _handlers[name] = (_, _, ct) => handler(ct);
        return this;
    }
}
