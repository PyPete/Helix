using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class VapourtecR4Tests
{
    // ── Helper ────────────────────────────────────────────────────────────────

    private static async Task<(VapourtecR4 driver, FakeSerialPortConnection port)> CreateDriverAsync(
        params string[] responses)
    {
        var port = new FakeSerialPortConnection();
        foreach (var r in responses)
        {
            port.EnqueueRead(r.EndsWith("\r\n") ? r : r + "\r\n");
        }

        var transport = new SerialDeviceTransport(
            new SerialTransportOptions { PortName = "COM_FAKE", CommandTerminator = "\r\n" },
            NullLogger<SerialDeviceTransport>.Instance,
            new FakeSerialPortConnectionFactory(port));

        await transport.ConnectAsync();

        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);
        return (driver, port);
    }

    private static JsonElement EmptyElement()
    {
        using var doc = JsonDocument.Parse("{}");
        return doc.RootElement.Clone();
    }

    private static JsonElement ElementFor(object obj)
        => JsonSerializer.SerializeToElement(obj);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Start_SendsPnCommand_ReturnsSuccess()
    {
        var (driver, port) = await CreateDriverAsync("OK\r\n", "OK\r\n");

        var result = await driver.InvokeCapabilityAsync("Start", EmptyElement(), null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(Encoding.ASCII.GetBytes("PN \r\n"), port.Writes.First());
    }

    [Fact]
    public async Task Start_ErrResponse_ReturnsFailure()
    {
        var (driver, port) = await CreateDriverAsync("ERR ER103\r\n");

        var result = await driver.InvokeCapabilityAsync("Start", EmptyElement(), null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("ERR", result.ErrorMessage);
    }

    [Fact]
    public async Task Stop_SendsPfCommand_ReturnsSuccess()
    {
        var (driver, port) = await CreateDriverAsync("OK\r\n", "OK\r\n");

        var result = await driver.InvokeCapabilityAsync("Stop", EmptyElement(), null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(Encoding.ASCII.GetBytes("PF\r\n"), port.Writes.First());
    }

    [Fact]
    public async Task SetFlowRate_PumpA_SendsFrCommand_WithCorrectRate()
    {
        var (driver, port) = await CreateDriverAsync("OK\r\n");
        var parameters = ElementFor(new { pump = 0, rateMlMin = 1.5 });

        var result = await driver.InvokeCapabilityAsync("SetFlowRate", parameters, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(Encoding.ASCII.GetBytes("FR 0 1500 \r\n"), port.Writes.First());
    }

    [Fact]
    public async Task SetFlowRate_PumpC_SendsFwFrCommand()
    {
        var (driver, port) = await CreateDriverAsync("OK\r\n");
        var parameters = ElementFor(new { pump = 2, rateMlMin = 0.5 });

        var result = await driver.InvokeCapabilityAsync("SetFlowRate", parameters, null, CancellationToken.None);

        var written = Encoding.ASCII.GetString(port.Writes.First());
        Assert.StartsWith("FW FR", written);
    }

    [Fact]
    public async Task SetTemperature_SendsR4StCommand()
    {
        var (driver, port) = await CreateDriverAsync("OK\r\n");
        var parameters = ElementFor(new { reactor = 1, targetCelsius = 60 });

        var result = await driver.InvokeCapabilityAsync("SetTemperature", parameters, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(Encoding.ASCII.GetBytes("R4 ST 1 60 \r\n"), port.Writes.First());
    }

    [Fact]
    public async Task SelectReagent_ChannelA_SendsKp1Command()
    {
        var (driver, port) = await CreateDriverAsync("OK\r\n");
        var parameters = ElementFor(new { channel = 0 });

        var result = await driver.InvokeCapabilityAsync("SelectReagent", parameters, null, CancellationToken.None);

        Assert.Equal(Encoding.ASCII.GetBytes("KP 1\r\n"), port.Writes.First());
    }

    [Fact]
    public async Task SelectReagent_ChannelC_SendsFwKp1Command()
    {
        var (driver, port) = await CreateDriverAsync("OK\r\n");
        var parameters = ElementFor(new { channel = 2 });

        var result = await driver.InvokeCapabilityAsync("SelectReagent", parameters, null, CancellationToken.None);

        Assert.Equal(Encoding.ASCII.GetBytes("FW KP 1\r\n"), port.Writes.First());
    }

    [Fact]
    public async Task SelectSolvent_ChannelA_SendsKp0Command()
    {
        var (driver, port) = await CreateDriverAsync("OK\r\n");
        var parameters = ElementFor(new { channel = 0 });

        var result = await driver.InvokeCapabilityAsync("SelectSolvent", parameters, null, CancellationToken.None);

        Assert.Equal(Encoding.ASCII.GetBytes("KP 0\r\n"), port.Writes.First());
    }

    [Fact]
    public async Task SelectLoopFill_LoopA_SendsKp4Command()
    {
        var (driver, port) = await CreateDriverAsync("OK\r\n");
        var parameters = ElementFor(new { loop = 0 });

        var result = await driver.InvokeCapabilityAsync("SelectLoopFill", parameters, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(Encoding.ASCII.GetBytes("KP 4\r\n"), port.Writes.First());
    }

    [Fact]
    public async Task SelectLoopFill_LoopC_SendsFwKp4Command()
    {
        var (driver, port) = await CreateDriverAsync("OK\r\n");
        var parameters = ElementFor(new { loop = 2 });

        var result = await driver.InvokeCapabilityAsync("SelectLoopFill", parameters, null, CancellationToken.None);

        Assert.Equal(Encoding.ASCII.GetBytes("FW KP 4\r\n"), port.Writes.First());
    }

    [Fact]
    public async Task SelectLoopInject_LoopA_SendsKp5Command()
    {
        var (driver, port) = await CreateDriverAsync("OK\r\n");
        var parameters = ElementFor(new { loop = 0 });

        var result = await driver.InvokeCapabilityAsync("SelectLoopInject", parameters, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(Encoding.ASCII.GetBytes("KP 5\r\n"), port.Writes.First());
    }

    [Fact]
    public async Task GetFirmwareVersion_ReturnsVersionString()
    {
        var (driver, port) = await CreateDriverAsync("v1.64 8-Jan-2010\r\n");

        var result = await driver.InvokeCapabilityAsync("GetFirmwareVersion", EmptyElement(), null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("v1.64", result.ReturnValue.Value.ToString());
        Assert.Equal(Encoding.ASCII.GetBytes("GV\r\n"), port.Writes.First());
    }

    [Fact]
    public async Task GetTemperatures_ParsesHtResponse_ReturnsValidReactors()
    {
        // Fields: 0=timestamp, 2=A(-1000), 4=B(-1000), 6=C(201=20.1), 8=D(-1000)
        var (driver, port) = await CreateDriverAsync("4913,U,-1000,U,-1000,U,201,U,-1000\r\n");

        var result = await driver.InvokeCapabilityAsync("GetTemperatures", EmptyElement(), null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);

        var json = result.ReturnValue.Value;
        // Reactor C (field index 6 = 201 / 10.0 = 20.1) should be present
        Assert.True(json.TryGetProperty("C", out var cTemp));
        Assert.Equal(20.1, cTemp.GetDouble(), precision: 5);

        // A, B, D are -1000 and should be absent
        Assert.False(json.TryGetProperty("A", out _));
        Assert.False(json.TryGetProperty("B", out _));
        Assert.False(json.TryGetProperty("D", out _));
    }

    [Fact]
    public async Task InvokeCapabilityAsync_ErrResponse_ReturnsFailure()
    {
        var (driver, port) = await CreateDriverAsync("ERR INVALID_COMMAND\r\n");

        var result = await driver.InvokeCapabilityAsync("Start", EmptyElement(), null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("ERR", result.ErrorMessage);
    }

    // ── Fakes ─────────────────────────────────────────────────────────────────

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
            _stream = new FakeSerialStream(_reads, _writes);
        }

        public bool IsOpen { get; private set; }

        public Stream BaseStream => _stream;

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

        public FakeSerialStream(Queue<byte[]> reads, List<byte[]> writes)
        {
            _reads = reads;
            _writes = writes;
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

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_reads.Count == 0)
                return ValueTask.FromResult(0);

            var next = _reads.Dequeue();
            next.CopyTo(buffer);
            return ValueTask.FromResult(next.Length);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _writes.Add(buffer.ToArray());
            return ValueTask.CompletedTask;
        }
    }
}
