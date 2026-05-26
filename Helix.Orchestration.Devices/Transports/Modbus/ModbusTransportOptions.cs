using System.IO.Ports;

namespace Helix.Orchestration.Devices.Transports.Modbus;

/// <summary>
/// Configuration options for Modbus transport (RTU or TCP).
/// </summary>
public sealed class ModbusTransportOptions
{
    /// <summary>
    /// Modbus mode (RTU over serial or TCP).
    /// </summary>
    public ModbusMode Mode { get; init; } = ModbusMode.Rtu;

    /// <summary>
    /// Default slave ID for Modbus operations.
    /// </summary>
    public byte SlaveId { get; init; } = 1;

    // RTU-specific options

    /// <summary>
    /// COM port name for RTU mode (e.g., "COM1", "/dev/ttyUSB0").
    /// </summary>
    public string? PortName { get; init; }

    /// <summary>
    /// Baud rate for RTU mode (default: 9600).
    /// </summary>
    public int BaudRate { get; init; } = 9600;

    /// <summary>
    /// Parity for RTU mode (default: Even).
    /// </summary>
    public Parity Parity { get; init; } = Parity.Even;

    /// <summary>
    /// Stop bits for RTU mode (default: One).
    /// </summary>
    public StopBits StopBits { get; init; } = StopBits.One;

    // TCP-specific options

    /// <summary>
    /// TCP host address for TCP mode (e.g., "192.168.1.100").
    /// </summary>
    public string? TcpHost { get; init; }

    /// <summary>
    /// TCP port for TCP mode (default: 502).
    /// </summary>
    public int TcpPort { get; init; } = 502;

    // Common options

    /// <summary>
    /// Read timeout in milliseconds (default: 1000).
    /// </summary>
    public int ReadTimeoutMs { get; init; } = 1000;

    /// <summary>
    /// Write timeout in milliseconds (default: 1000).
    /// </summary>
    public int WriteTimeoutMs { get; init; } = 1000;
}

/// <summary>
/// Modbus communication mode.
/// </summary>
public enum ModbusMode
{
    /// <summary>
    /// Modbus RTU over serial (RS232/RS485).
    /// </summary>
    Rtu,

    /// <summary>
    /// Modbus TCP over Ethernet.
    /// </summary>
    Tcp
}
