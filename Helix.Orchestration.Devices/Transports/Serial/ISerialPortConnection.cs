using System.IO.Ports;

namespace Helix.Orchestration.Devices.Transports.Serial;

/// <summary>
/// Testable abstraction over a serial port connection.
/// </summary>
public interface ISerialPortConnection : IDisposable
{
    bool IsOpen { get; }

    Stream BaseStream { get; }

    void Open();

    void Close();
}

/// <summary>
/// Factory for creating serial port connections.
/// </summary>
public interface ISerialPortConnectionFactory
{
    ISerialPortConnection Create(SerialTransportOptions options);
}

internal sealed class SerialPortConnectionFactory : ISerialPortConnectionFactory
{
    public ISerialPortConnection Create(SerialTransportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new SerialPortConnection(new SerialPort
        {
            PortName = options.PortName,
            BaudRate = options.BaudRate,
            DataBits = options.DataBits,
            Parity = options.Parity,
            StopBits = options.StopBits,
            ReadTimeout = options.ReadTimeoutMs,
            WriteTimeout = options.WriteTimeoutMs,
            RtsEnable = options.RtsEnable,
            DtrEnable = options.DtrEnable,
            Handshake = options.Handshake
        });
    }
}

internal sealed class SerialPortConnection : ISerialPortConnection
{
    private readonly SerialPort _serialPort;

    public SerialPortConnection(SerialPort serialPort)
    {
        _serialPort = serialPort;
    }

    public bool IsOpen => _serialPort.IsOpen;

    public Stream BaseStream => _serialPort.BaseStream;

    public void Open() => _serialPort.Open();

    public void Close() => _serialPort.Close();

    public void Dispose() => _serialPort.Dispose();
}
