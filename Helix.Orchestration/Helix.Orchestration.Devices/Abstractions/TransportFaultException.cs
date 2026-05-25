namespace Helix.Orchestration.Devices.Abstractions;

/// <summary>
/// Exception thrown when a device transport encounters an unrecoverable fault.
/// Used by circuit breaker and reconnect logic to track fault context.
/// </summary>
public class TransportFaultException : Exception
{
    /// <summary>
    /// The status of the transport before the fault occurred.
    /// </summary>
    public TransportStatus PreviousStatus { get; }

    /// <summary>
    /// The type of transport that faulted (e.g., "OPC UA", "Modbus RTU", "Serial").
    /// </summary>
    public string TransportType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransportFaultException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="transportType">The type of transport that faulted.</param>
    /// <param name="previousStatus">The previous status before the fault.</param>
    /// <param name="innerException">The underlying exception that caused the fault.</param>
    public TransportFaultException(
        string message,
        string transportType,
        TransportStatus previousStatus,
        Exception? innerException = null)
        : base(message, innerException)
    {
        TransportType = transportType;
        PreviousStatus = previousStatus;
    }
}
