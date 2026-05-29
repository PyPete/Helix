using System.Threading.Channels;

namespace Helix.Orchestration.Agent.Events;

/// <summary>
/// Defines the in-process event bus for runspace lifecycle and live data events.
/// Producers call <see cref="Publish"/> to deliver events; consumers read from <see cref="Events"/>.
/// </summary>
public interface IRunspaceEventEmitter
{
    /// <summary>
    /// Publishes an event to all active subscribers.
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
