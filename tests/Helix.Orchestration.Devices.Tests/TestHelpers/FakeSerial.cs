using System.Text;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging.Abstractions;

namespace Helix.Orchestration.Devices.Tests.TestHelpers;

/// <summary>
/// Shared test infrastructure for serial driver tests.
/// Provides FakeSerialPortConnection, FakeSerialPortConnectionFactory, FakeSerialStream,
/// and a MakeTransport helper for constructing a SerialDeviceTransport backed by a fake port.
/// </summary>
public static class FakeSerial
{
    /// <summary>
    /// Creates a <see cref="SerialDeviceTransport"/> backed by the given <paramref name="port"/>.
    /// </summary>
    public static SerialDeviceTransport MakeTransport(
        FakeSerialPortConnection port,
        string portName = "COM1",
        int baudRate = 9600,
        string commandTerminator = "\r\n",
        System.IO.Ports.Parity parity = System.IO.Ports.Parity.None,
        int dataBits = 8)
    {
        return new SerialDeviceTransport(
            new SerialTransportOptions
            {
                PortName = portName,
                BaudRate = baudRate,
                CommandTerminator = commandTerminator,
                Parity = parity,
                DataBits = dataBits
            },
            NullLogger<SerialDeviceTransport>.Instance,
            new FakeSerialPortConnectionFactory(port));
    }
}

/// <summary>Fake serial port connection factory for testing.</summary>
public sealed class FakeSerialPortConnectionFactory : ISerialPortConnectionFactory
{
    private readonly FakeSerialPortConnection _connection;

    public FakeSerialPortConnectionFactory(FakeSerialPortConnection connection)
        => _connection = connection;

    public ISerialPortConnection Create(SerialTransportOptions options) => _connection;
}

/// <summary>Fake serial port connection that allows enqueuing reads and recording writes.</summary>
public sealed class FakeSerialPortConnection : ISerialPortConnection
{
    private readonly Queue<byte[]> _reads = new();
    private readonly List<byte[]> _writes = new();
    private readonly FakeSerialStream _stream;

    public FakeSerialPortConnection()
    {
        _stream = new FakeSerialStream(_reads, _writes, () => WriteException, () => ReadDelay);
    }

    public bool IsOpen { get; private set; }
    public Stream BaseStream => _stream;
    public Exception? WriteException { get; set; }
    public TimeSpan ReadDelay { get; set; }
    public IReadOnlyList<byte[]> Writes => _writes;

    public void EnqueueRead(string payload) => _reads.Enqueue(Encoding.ASCII.GetBytes(payload));
    public void Open() => IsOpen = true;
    public void Close() => IsOpen = false;
    public void Dispose() => IsOpen = false;
}

/// <summary>Async stream backed by in-memory read queue and write list.</summary>
public sealed class FakeSerialStream : Stream
{
    private readonly Queue<byte[]> _reads;
    private readonly List<byte[]> _writes;
    private readonly Func<Exception?> _writeException;
    private readonly Func<TimeSpan> _readDelay;

    public FakeSerialStream(Queue<byte[]> reads, List<byte[]> writes, Func<Exception?> writeException, Func<TimeSpan> readDelay)
    {
        _reads = reads;
        _writes = writes;
        _writeException = writeException;
        _readDelay = readDelay;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var delay = _readDelay();
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, cancellationToken);
        if (_reads.Count == 0)
            return 0;
        var next = _reads.Dequeue();
        next.CopyTo(buffer);
        return next.Length;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var exception = _writeException();
        if (exception is not null)
            return ValueTask.FromException(exception);
        _writes.Add(buffer.ToArray());
        return ValueTask.CompletedTask;
    }
}
