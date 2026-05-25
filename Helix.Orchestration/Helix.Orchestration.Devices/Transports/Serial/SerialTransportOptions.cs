using System.IO.Ports;

namespace Helix.Orchestration.Devices.Transports.Serial;

/// <summary>
/// Configuration options for serial port transport.
/// </summary>
public sealed class SerialTransportOptions
{
    /// <summary>
    /// The COM port name (e.g., "COM1", "/dev/ttyUSB0").
    /// </summary>
    public required string PortName { get; init; }

    /// <summary>
    /// Baud rate (default: 9600).
    /// </summary>
    public int BaudRate { get; init; } = 9600;

    /// <summary>
    /// Data bits (default: 8).
    /// </summary>
    public int DataBits { get; init; } = 8;

    /// <summary>
    /// Parity setting (default: None).
    /// </summary>
    public Parity Parity { get; init; } = Parity.None;

    /// <summary>
    /// Stop bits (default: One).
    /// </summary>
    public StopBits StopBits { get; init; } = StopBits.One;

    /// <summary>
    /// Command terminator string (default: "\r\n").
    /// </summary>
    public string CommandTerminator { get; init; } = "\r\n";

    /// <summary>
    /// Read timeout in milliseconds (default: 500).
    /// </summary>
    public int ReadTimeoutMs { get; init; } = 500;

    /// <summary>
    /// Write timeout in milliseconds (default: 500).
    /// </summary>
    public int WriteTimeoutMs { get; init; } = 500;

    /// <summary>
    /// Enable Request-To-Send (RTS) signal (default: false).
    /// </summary>
    public bool RtsEnable { get; init; } = false;

    /// <summary>
    /// Enable Data-Terminal-Ready (DTR) signal (default: false).
    /// </summary>
    public bool DtrEnable { get; init; } = false;

    /// <summary>
    /// Handshake mode (default: None).
    /// </summary>
    public Handshake Handshake { get; init; } = Handshake.None;
}
