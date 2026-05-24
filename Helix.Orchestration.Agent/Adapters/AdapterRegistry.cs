using System.Collections.Concurrent;

namespace Helix.Orchestration.Agent.Adapters;

/// <summary>
/// Thread-safe in-memory registry of discovered adapter proxies.
/// </summary>
public sealed class AdapterRegistry
{
    private readonly ConcurrentDictionary<string, AdapterProxy> _adapters = new();

    /// <summary>
    /// Adds or replaces an adapter proxy under the specified adapter identifier.
    /// </summary>
    public void Upsert(string adapterId, AdapterProxy proxy) => _adapters[adapterId] = proxy;

    /// <summary>
    /// Attempts to retrieve an adapter proxy by identifier.
    /// </summary>
    public bool TryGet(string adapterId, out AdapterProxy proxy) => _adapters.TryGetValue(adapterId, out proxy!);

    /// <summary>
    /// Returns a snapshot view of all known adapters.
    /// </summary>
    public IReadOnlyDictionary<string, AdapterProxy> GetAll() => _adapters;
}
