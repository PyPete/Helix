using System.Collections.Concurrent;

namespace Helix.Orchestration.Core.Capabilities;

/// <summary>
/// Registry for capability invokers.
/// Maps capability names to their implementations (OPC UA adapters, SiLA 2 adapters, SDK wrappers).
/// Workflows invoke capabilities by name through this registry.
/// </summary>
public sealed class CapabilityRegistry : ICapabilityInvoker
{
    private readonly ConcurrentDictionary<string, ICapabilityInvoker> _invokers = new();

    /// <summary>
    /// Registers a capability invoker.
    /// </summary>
    /// <param name="capabilityName">Unique name for the capability (e.g., "SetTemperature", "RunHPLC").</param>
    /// <param name="invoker">The invoker implementation.</param>
    /// <exception cref="ArgumentException">Thrown if a capability with the same name is already registered.</exception>
    public void Register(string capabilityName, ICapabilityInvoker invoker)
    {
        if (!_invokers.TryAdd(capabilityName, invoker))
        {
            throw new ArgumentException($"Capability '{capabilityName}' is already registered.", nameof(capabilityName));
        }
    }

    /// <summary>
    /// Unregisters a capability invoker.
    /// </summary>
    /// <param name="capabilityName">The capability name to unregister.</param>
    public void Unregister(string capabilityName)
    {
        _invokers.TryRemove(capabilityName, out _);
    }

    /// <summary>
    /// Gets all registered capability names.
    /// </summary>
    public IEnumerable<string> GetAllCapabilityNames() => _invokers.Keys;

    /// <inheritdoc/>
    public async Task<CapabilityInvocationResult> InvokeAsync(
        string capabilityName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (!_invokers.TryGetValue(capabilityName, out var invoker))
        {
            return CapabilityInvocationResult.Failed($"Capability '{capabilityName}' is not registered.");
        }

        try
        {
            return await invoker.InvokeAsync(capabilityName, parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            return CapabilityInvocationResult.Failed($"Capability '{capabilityName}' invocation failed.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(string capabilityName)
    {
        if (!_invokers.TryGetValue(capabilityName, out var invoker))
        {
            return false;
        }

        return await invoker.IsAvailableAsync(capabilityName);
    }
}
