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
    private static VapourtecR4 CreateDriver(params string[] responses)
    {
        var port = new FakeSerialPortConnection();
        foreach (var r in responses)
            port.EnqueueRead(r + "\r\n");
        var factory = new FakeSerialPortConnectionFactory(port);
        var transport = new SerialDeviceTransport(
            new SerialTransportOptions { PortName = "COM1", BaudRate = 19200, CommandTerminator = "\r\n" },
            NullLogger<SerialDeviceTransport>.Instance,
            factory);
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);
        transport.ConnectAsync().GetAwaiter().GetResult();
        return driver;
    }

    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    // ---- Start capability ----

    [Fact]
    public async Task Start_SendsPnCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        port.EnqueueRead("OK\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("PN \r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task Start_ErrResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("ERR ER103\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("ERR", result.ErrorMessage);
    }

    // ---- Stop capability ----

    [Fact]
    public async Task Stop_SendsPfCommand_ReturnsSuccess()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        port.EnqueueRead("OK\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("PF\r\n"), port.Writes[0]);
    }

    // ---- SetFlowRate capability ----

    [Fact]
    public async Task SetFlowRate_PumpA_SendsFrCommandWithCorrectRate()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetFlowRate", Json(@"{""pump"":0,""rateMlMin"":1.5}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("FR 0 1500 \r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task SetFlowRate_PumpC_SendsFwFrCommand()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetFlowRate", Json(@"{""pump"":2,""rateMlMin"":0.5}"));

        Assert.True(result.Success);
        Assert.StartsWith("FW FR", Encoding.ASCII.GetString(port.Writes[0]));
    }

    [Fact]
    public async Task SetFlowRate_InvalidPump_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetFlowRate", Json(@"{""pump"":5,""rateMlMin"":1.0}"));

        Assert.False(result.Success);
        Assert.Contains("out of range", result.ErrorMessage);
    }

    // ---- SetTemperature capability ----

    [Fact]
    public async Task SetTemperature_SendsR4StCommandWithCorrectParams()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("SetTemperature", Json(@"{""reactor"":1,""targetCelsius"":60}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("R4 ST 1 60 \r\n"), port.Writes[0]);
    }

    // ---- SelectReagent capability ----

    [Fact]
    public async Task SelectReagent_ChannelA_SendsKp1Command()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("SelectReagent", Json(@"{""channel"":0}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("KP 1\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task SelectReagent_ChannelC_SendsFwKp1Command()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("SelectReagent", Json(@"{""channel"":2}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("FW KP 1\r\n"), port.Writes[0]);
    }

    // ---- SelectSolvent capability ----

    [Fact]
    public async Task SelectSolvent_ChannelA_SendsKp0Command()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("SelectSolvent", Json(@"{""channel"":0}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("KP 0\r\n"), port.Writes[0]);
    }

    // ---- SelectLoopFill capability ----

    [Fact]
    public async Task SelectLoopFill_LoopA_SendsKp4Command()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("SelectLoopFill", Json(@"{""loop"":0}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("KP 4\r\n"), port.Writes[0]);
    }

    [Fact]
    public async Task SelectLoopFill_LoopC_SendsFwKp4Command()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("SelectLoopFill", Json(@"{""loop"":2}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("FW KP 4\r\n"), port.Writes[0]);
    }

    // ---- SelectLoopInject capability ----

    [Fact]
    public async Task SelectLoopInject_LoopA_SendsKp5Command()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("OK\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("SelectLoopInject", Json(@"{""loop"":0}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("KP 5\r\n"), port.Writes[0]);
    }

    // ---- GetFirmwareVersion capability ----

    [Fact]
    public async Task GetFirmwareVersion_SendsGvCommand_ReturnsVersionString()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("v1.64 8-Jan-2010\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetFirmwareVersion", Json("{}"));

        Assert.True(result.Success);
        Assert.Equal(Ascii("GV\r\n"), port.Writes[0]);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("v1.64", result.ReturnValue.ToString());
    }

    // ---- GetTemperatures capability ----

    [Fact]
    public async Task GetTemperatures_ParsesHtResponse_ReturnsNonNullReactors()
    {
        // field[0]=4913 (timestamp), field[2]=-1000 (no probe A), field[4]=-1000 (no probe B),
        // field[6]=201 (reactor C = 20.1 C), field[8]=-1000 (no probe D)
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("4913,U,-1000,U,-1000,U,201,U,-1000\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("GetTemperatures", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        var json = result.ReturnValue!.Value.ToString();
        Assert.DoesNotContain("\"A\"", json);
        Assert.DoesNotContain("\"B\"", json);
        Assert.Contains("\"C\"", json);
        Assert.DoesNotContain("\"D\"", json);
        // Verify value is approximately 20.1
        var parsed = JsonDocument.Parse(json);
        Assert.Equal(20.1, parsed.RootElement.GetProperty("C").GetDouble(), 1);
    }

    // ---- ERR response handling ----

    [Fact]
    public async Task InvokeCapabilityAsync_ErrResponse_ReturnsFailure()
    {
        var port = new FakeSerialPortConnection();
        port.EnqueueRead("ERR INVALID_COMMAND\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("ERR", result.ErrorMessage);
    }

    // ---- StreamTrendsAsync ----

    [Fact]
    public async Task StreamTrendsAsync_EmitsTemperatureTrendSamples()
    {
        var port = new FakeSerialPortConnection();
        // Each poll cycle reads HT then HF; enqueue enough for 2 HT responses
        port.EnqueueRead("4913,U,-1000,U,-1000,U,201,U,-1000\r\n");
        port.EnqueueRead("19911,0,0\r\n"); // HF
        port.EnqueueRead("4913,U,-1000,U,-1000,U,201,U,-1000\r\n");
        port.EnqueueRead("19911,0,0\r\n");
        var transport = MakeTransport(port);
        await transport.ConnectAsync();
        var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

        var samples = new List<TrendSample>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 2)
                break;
        }

        Assert.True(samples.Count >= 1);
        Assert.Contains(samples, s => s.TrendName.StartsWith("vapourtec.temperature."));
    }

    // ---- Helpers ----

    private static SerialDeviceTransport MakeTransport(FakeSerialPortConnection port)
        => new SerialDeviceTransport(
            new SerialTransportOptions { PortName = "COM1", BaudRate = 19200, CommandTerminator = "\r\n" },
            NullLogger<SerialDeviceTransport>.Instance,
            new FakeSerialPortConnectionFactory(port));

    private sealed class FakeSerialPortConnectionFactory : ISerialPortConnectionFactory
    {
        private readonly FakeSerialPortConnection _connection;
        public FakeSerialPortConnectionFactory(FakeSerialPortConnection connection) => _connection = connection;
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

        public FakeSerialStream(Queue<byte[]> reads, List<byte[]> writes, Func<Exception?> writeException, Func<TimeSpan> readDelay)
        {
            _reads = reads; _writes = writes; _writeException = writeException; _readDelay = readDelay;
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
}