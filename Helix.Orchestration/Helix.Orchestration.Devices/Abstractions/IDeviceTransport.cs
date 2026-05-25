namespace Helix.Orchestration.Devices.Abstractions;

/// <summary>
/// Represents a low-level device transport (Serial, TCP, Modbus, OPC UA, C DLL).
/// All transports support async connect/disconnect, status observability, and data exchange.
/// </summary>
public interface IDeviceTransport : IAsyncDisposable
{
    /// <summary>
    /// Gets the current status of the transport.
    /// </summary>
    TransportStatus Status { get; }

    /// <summary>
    /// Observable stream of status changes for reactive monitoring.
    /// </summary>
    IObservable<TransportStatus> StatusChanged { get; }

    /// <summary>
    /// Observable stream of unsolicited data received from the device (e.g., OPC subscriptions, serial push data).
    /// </summary>
    IObservable<byte[]> DataReceived { get; }

    /// <summary>
    /// Establishes a connection to the device.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous connect operation.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the device gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous disconnect operation.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command and waits for a response (request-reply pattern).
    /// </summary>
    /// <param name="command">The command bytes to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response bytes received from the device.</returns>
    /// <exception cref="TransportFaultException">Thrown when the transport encounters an unrecoverable fault.</exception>
    Task<byte[]> SendReceiveAsync(byte[] command, CancellationToken cancellationToken = default);
}
