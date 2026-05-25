using System.IO.Ports;
using System.Text;
using Helix.Orchestration.Devices.Transport;

namespace Helix.Orchestration.Agent.Transport;

public sealed class SerialTransport : ISerialTransport, IDisposable
{
    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private readonly SerialPort _serialPort;

    public bool IsOpen => _serialPort.IsOpen;

    public SerialTransport()
    {
        _serialPort = new SerialPort
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };
    }

    public void Open(string portName, int baudRate = 9600)
    {
        if (_serialPort.IsOpen)
            _serialPort.Close();

        _serialPort.PortName = portName;
        _serialPort.BaudRate = baudRate;
        _serialPort.Open();
    }

    public void Close()
    {
        if (_serialPort.IsOpen)
            _serialPort.Close();
    }

    public string SendCommand(string command, int timeoutMs = 200)
    {
        _ioLock.Wait();
        try
        {
            _serialPort.Write(command);
            Thread.Sleep(timeoutMs);

            var bytes = _serialPort.BytesToRead;
            if (bytes <= 0)
                return string.Empty;

            var buffer = new byte[bytes];
            _serialPort.Read(buffer, 0, bytes);
            return Encoding.Default.GetString(buffer);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public string SendCommand(byte[] command, int timeoutMs = 200)
    {
        _ioLock.Wait();
        try
        {
            _serialPort.Write(command, 0, command.Length);
            Thread.Sleep(timeoutMs);

            var bytes = _serialPort.BytesToRead;
            if (bytes <= 0)
                return string.Empty;

            var buffer = new byte[bytes];
            _serialPort.Read(buffer, 0, bytes);
            return Encoding.Default.GetString(buffer);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public byte[] SendCommandFixedResponse(byte[] command, int expectedBytes, int timeoutMs = 200)
    {
        _ioLock.Wait();
        try
        {
            _serialPort.ReadExisting();
            _serialPort.Write(command, 0, command.Length);

            var response = new byte[expectedBytes];
            var read = 0;
            var start = Environment.TickCount;

            while (read < expectedBytes)
            {
                if (Environment.TickCount - start > timeoutMs)
                    break;

                if (_serialPort.BytesToRead <= 0)
                {
                    Thread.Sleep(1);
                    continue;
                }

                read += _serialPort.Read(response, read, expectedBytes - read);
            }

            return response;
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public void Dispose()
    {
        Close();
        _serialPort.Dispose();
        _ioLock.Dispose();
    }
}
