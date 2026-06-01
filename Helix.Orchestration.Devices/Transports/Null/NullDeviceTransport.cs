using System.Reactive.Subjects;
using Helix.Orchestration.Devices.Abstractions;

namespace Helix.Orchestration.Devices.Transports.Null;

/// <summary>
/// A no-op IDeviceTransport for facade drivers that do not use transport I/O.
/// All methods throw NotSupportedException as transport operations are not applicable.
/// </summary>
public sealed class NullDeviceTransport : IDeviceTransport
{
    private readonly Subject<TransportStatus> _statusChanged = new();
    private readonly Subject<byte[]> _dataReceived = new();

    /// <inheritdoc/>
    public TransportStatus Status => TransportStatus.Disconnected;

    /// <inheritdoc/>
    public IObservable<TransportStatus> StatusChanged => _statusChanged;

    /// <inheritdoc/>
    public IObservable<byte[]> DataReceived => _dataReceived;

    /// <inheritdoc/>
    public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<byte[]> SendReceiveAsync(byte[] command, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This driver does not use a transport.");

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        _statusChanged.Dispose();
        _dataReceived.Dispose();
        return ValueTask.CompletedTask;
    }
}
