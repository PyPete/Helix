using System.Threading.Channels;

namespace Helix.Orchestration.Agent.Events;

/// <summary>
/// Defines the in-process event bus dedicated to experiment lifecycle events.
/// Producers call <see cref="Publish"/> to deliver events; the persistence worker reads from <see cref="Events"/>.
/// </summary>
public interface IExperimentEventChannel
{
    /// <summary>
    /// Publishes an event to the experiment event channel.
    /// This method is fire-and-forget; it never blocks the caller.
    /// </summary>
    /// <param name="evt">The event to publish.</param>
    void Publish(RunspaceEvent evt);

    /// <summary>
    /// A reader over the unbounded event channel.
    /// Consumers use <c>await foreach</c> over <see cref="ChannelReader{T}.ReadAllAsync"/> to receive events.
    /// </summary>
    ChannelReader<RunspaceEvent> Events { get; }
}
