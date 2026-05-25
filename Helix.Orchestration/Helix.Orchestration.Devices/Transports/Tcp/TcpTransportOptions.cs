using Helix.Orchestration.Devices.FaultTolerance;

namespace Helix.Orchestration.Devices.Transports.Tcp;

/// <summary>
/// Configuration options for TCP socket transport.
/// </summary>
public sealed class TcpTransportOptions
{
    /// <summary>
    /// Remote host address (IP or hostname).
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// Remote port number.
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// Connection timeout in milliseconds (default: 3000).
    /// </summary>
    public int ConnectTimeoutMs { get; init; } = 3000;

    /// <summary>
    /// Send timeout in milliseconds (default: 5000).
    /// </summary>
    public int SendTimeoutMs { get; init; } = 5000;

    /// <summary>
    /// Receive timeout in milliseconds (default: 5000).
    /// </summary>
    public int ReceiveTimeoutMs { get; init; } = 5000;

    /// <summary>
    /// Message framing mode (default: Delimiter).
    /// </summary>
    public TcpFramingMode FramingMode { get; init; } = TcpFramingMode.Delimiter;

    /// <summary>
    /// Message delimiter byte (used when FramingMode is Delimiter, default: 0x0A = '\n').
    /// </summary>
    public byte DelimiterByte { get; init; } = 0x0A;

    /// <summary>
    /// Whether to use length-prefix framing (4-byte int length prefix before each message).
    /// Used when FramingMode is LengthPrefix.
    /// </summary>
    public bool UseLengthPrefix { get; init; } = false;

    /// <summary>
    /// Reconnection policy for handling disconnections.
    /// </summary>
    public ReconnectPolicy? ReconnectPolicy { get; init; }
}

/// <summary>
/// TCP message framing mode.
/// </summary>
public enum TcpFramingMode
{
    /// <summary>
    /// Messages are delimited by a specific byte (e.g., newline).
    /// </summary>
    Delimiter,

    /// <summary>
    /// Messages are prefixed with a 4-byte length header.
    /// </summary>
    LengthPrefix,

    /// <summary>
    /// No framing; read fixed-size buffers or all available data.
    /// </summary>
    Raw
}
