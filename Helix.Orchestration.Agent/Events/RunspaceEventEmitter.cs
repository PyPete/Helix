using System.Threading.Channels;

namespace Helix.Orchestration.Agent.Events;

/// <summary>
/// Channel-backed singleton implementation of <see cref="IRunspaceEventEmitter"/>.
/// Uses an unbounded channel so <see cref="Publish"/> is always non-blocking.
/// </summary>
public sealed class RunspaceEventEmitter : IRunspaceEventEmitter
{
    private readonly Channel<RunspaceEvent> _channel = Channel.CreateUnbounded<RunspaceEvent>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    /// <inheritdoc />
    public ChannelReader<RunspaceEvent> Events => _channel.Reader;

    /// <inheritdoc />
    /// <remarks>
    /// Unbounded channels never reject writes, so the return value of
    /// <see cref="ChannelWriter{T}.TryWrite"/> is silently discarded.
    /// </remarks>
    public void Publish(RunspaceEvent evt) => _channel.Writer.TryWrite(evt);
}
