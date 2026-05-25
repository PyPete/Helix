using System.Text;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Transports;

public sealed class SerialTransportTests
{
    [Fact]
    public async Task SendReceiveAsync_HappyPath_ReturnsResponse()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        var transport = CreateTransport(port, "\r\n");

        await transport.ConnectAsync();
        var response = await transport.SendReceiveAsync(Encoding.ASCII.GetBytes("PING"));

        Assert.Equal("OK\r\n", Encoding.ASCII.GetString(response));
        Assert.Equal("PING", Encoding.ASCII.GetString(port.Writes.Single()));
    }

    [Fact]
    public async Task SendReceiveAsync_WhenWriteThrowsIOException_SetsFaulted()
    {
        var port = new FakeSerialPortConnection
        {
            WriteException = new IOException("write failed")
        };
        var transport = CreateTransport(port, "\r\n");

        await transport.ConnectAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => transport.SendReceiveAsync(Encoding.ASCII.GetBytes("PING")));
        await EventuallyAsync(() => transport.Status == TransportStatus.Faulted);

        Assert.Equal(TransportStatus.Faulted, transport.Status);
    }

    [Fact]
    public async Task SendReceiveAsync_WhenWriteThrowsUnauthorizedAccessException_SetsFaulted()
    {
        var port = new FakeSerialPortConnection
        {
            WriteException = new UnauthorizedAccessException("usb unplugged")
        };
        var transport = CreateTransport(port, "\r\n");

        await transport.ConnectAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => transport.SendReceiveAsync(Encoding.ASCII.GetBytes("PING")));
        await EventuallyAsync(() => transport.Status == TransportStatus.Faulted);

        Assert.Equal(TransportStatus.Faulted, transport.Status);
    }

    [Fact]
    public async Task DisconnectAsync_StopsWorker_AndDrainsPendingWork()
    {
        var port = new FakeSerialPortConnection();
        port.ReadDelay = TimeSpan.FromMilliseconds(200);
        port.EnqueueRead("OK\r\n");
        var transport = CreateTransport(port, "\r\n");

        await transport.ConnectAsync();
        var pending = transport.SendReceiveAsync(Encoding.ASCII.GetBytes("PING"));
        await Task.Delay(25);

        await transport.DisconnectAsync();

        Assert.Equal(TransportStatus.Disconnected, transport.Status);
        await Assert.ThrowsAnyAsync<Exception>(async () => await pending);
    }

    [Fact]
    public async Task SendReceiveAsync_UsesCommandTerminatorFraming()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("PART1");
        port.EnqueueRead("PART2<END>");
        var transport = CreateTransport(port, "<END>");

        await transport.ConnectAsync();
        var response = await transport.SendReceiveAsync(Encoding.ASCII.GetBytes("PING"));

        Assert.Equal("PART1PART2<END>", Encoding.ASCII.GetString(response));
    }

    private static SerialDeviceTransport CreateTransport(FakeSerialPortConnection port, string terminator)
    {
        return new SerialDeviceTransport(
            new SerialTransportOptions { PortName = "COM1", CommandTerminator = terminator },
            NullLogger<SerialDeviceTransport>.Instance,
            new FakeSerialPortConnectionFactory(port));
    }

    private static async Task EventuallyAsync(Func<bool> predicate)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (predicate())
            {
                return;
            }

            await Task.Delay(25);
        }

        Assert.True(predicate(), "Condition was not met within the expected time.");
    }

    private sealed class FakeSerialPortConnectionFactory : ISerialPortConnectionFactory
    {
        private readonly FakeSerialPortConnection _connection;

        public FakeSerialPortConnectionFactory(FakeSerialPortConnection connection)
        {
            _connection = connection;
        }

        public ISerialPortConnection Create(SerialTransportOptions options) => _connection;
    }

    private sealed class FakeSerialPortConnection : ISerialPortConnection
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

    private sealed class FakeSerialStream : Stream
    {
        private readonly Queue<byte[]> _reads;
        private readonly List<byte[]> _writes;
        private readonly Func<Exception?> _writeException;
        private readonly Func<TimeSpan> _readDelay;

        public FakeSerialStream(
            Queue<byte[]> reads,
            List<byte[]> writes,
            Func<Exception?> writeException,
            Func<TimeSpan> readDelay)
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

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var delay = _readDelay();
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }

            if (_reads.Count == 0)
            {
                return 0;
            }

            var next = _reads.Dequeue();
            next.CopyTo(buffer);
            return next.Length;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var exception = _writeException();
            if (exception is not null)
            {
                return ValueTask.FromException(exception);
            }

            _writes.Add(buffer.ToArray());
            return ValueTask.CompletedTask;
        }
    }
}
