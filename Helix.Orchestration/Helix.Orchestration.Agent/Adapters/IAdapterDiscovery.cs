namespace Helix.Orchestration.Agent.Adapters;

public interface IAdapterDiscovery
{
    Task DiscoverAsync(CancellationToken cancellationToken = default);
}
