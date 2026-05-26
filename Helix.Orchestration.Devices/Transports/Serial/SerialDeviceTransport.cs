using System.IO.Ports;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Channels;
using Helix.Orchestration.Devices.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Transports.Serial;

/// <summary>
/// Serial port transport implementation using System.IO.Ports and async channels.
/// </summary>
public sealed class SerialDeviceTransport : IDeviceTransport
{
    private readonly SerialTransportOptions _options;
    private readonly ILogger<SerialDeviceTransport> _logger;
    private readonly ISerialPortConnectionFactory _serialPortFactory;
    private readonly Channel<SerialJob> _jobChannel;
    private readonly Subject<TransportStatus> _statusSubject;
    private readonly Subject<byte[]> _dataReceivedSubject;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private ISerialPortConnection? _serialPort;
    private Task? _workerTask;
    private CancellationTokenSource? _workerCts;
    private TransportStatus _status = TransportStatus.Disconnected;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerialDeviceTransport"/> class.
    /// </summary>
    public SerialDeviceTransport(SerialTransportOptions options, ILogger<SerialDeviceTransport> logger)
        : this(options, logger, new SerialPortConnectionFactory())
    {
    }

    internal SerialDeviceTransport(
        SerialTransportOptions options,
        ILogger<SerialDeviceTransport> logger,
        ISerialPortConnectionFactory serialPortFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serialPortFactory = serialPortFactory ?? throw new ArgumentNullException(nameof(serialPortFactory));
        _jobChannel = Channel.CreateUnbounded<SerialJob>();
        _statusSubject = new Subject<TransportStatus>();
        _dataReceivedSubject = new Subject<byte[]>();
    }

    /// <inheritdoc/>
    public TransportStatus Status => _status;

    /// <inheritdoc/>
    public IObservable<TransportStatus> StatusChanged => _statusSubject;

    /// <inheritdoc/>
    public IObservable<byte[]> DataReceived => _dataReceivedSubject;

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_status == TransportStatus.Connected || _status == TransportStatus.Connecting)
            {
                _logger.LogWarning("Already connected or connecting");
                return;
            }

            SetStatus(TransportStatus.Connecting);

            _serialPort = _serialPortFactory.Create(_options);
            _serialPort.Open();
            _logger.LogInformation("Serial port {PortName} opened at {BaudRate} baud", _options.PortName, _options.BaudRate);

            _workerCts = new CancellationTokenSource();
            _workerTask = Task.Run(() => WorkerLoopAsync(_workerCts.Token), _workerCts.Token);

            SetStatus(TransportStatus.Connected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open serial port {PortName}", _options.PortName);
            SetStatus(TransportStatus.Faulted);
            throw new TransportFaultException($"Failed to connect to serial port {_options.PortName}", "Serial", TransportStatus.Connecting, ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_status == TransportStatus.Disconnected)
                return;

            _workerCts?.Cancel();

            if (_workerTask != null)
            {
                try
                {
                    await _workerTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }

            _serialPort?.Close();
            _serialPort?.Dispose();
            _serialPort = null;

            _logger.LogInformation("Serial port {PortName} closed", _options.PortName);
            SetStatus(TransportStatus.Disconnected);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> SendReceiveAsync(byte[] command, CancellationToken cancellationToken = default)
    {
        if (_status != TransportStatus.Connected)
            throw new TransportFaultException("Serial transport is not connected", "Serial", _status);

        var job = new SerialJob(command);
        await _jobChannel.Writer.WriteAsync(job, cancellationToken);
        return await job.Completion.Task.WaitAsync(cancellationToken);
    }

    private async Task WorkerLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Serial worker loop started");

        try
        {
            await foreach (var job in _jobChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var response = await ExecuteJobAsync(job, cancellationToken);
                    job.Completion.SetResult(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing serial job");
                    job.Completion.SetException(ex);

                    if (ex is IOException or UnauthorizedAccessException)
                    {
                        _logger.LogError("Serial port fault detected, setting Faulted status");
                        SetStatus(TransportStatus.Faulted);
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Serial worker loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in serial worker loop");
            SetStatus(TransportStatus.Faulted);
        }
    }

    private async Task<byte[]> ExecuteJobAsync(SerialJob job, CancellationToken cancellationToken)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
            throw new TransportFaultException("Serial port is not open", "Serial", _status);

        // Write command
        await _serialPort.BaseStream.WriteAsync(job.Command, cancellationToken);
        _logger.LogTrace("Sent {ByteCount} bytes to serial port", job.Command.Length);

        // Read response (until command terminator or timeout)
        var buffer = new List<byte>();
        var terminatorBytes = Encoding.ASCII.GetBytes(_options.CommandTerminator);
        var readBuffer = new byte[256];

        while (true)
        {
            var bytesRead = await _serialPort.BaseStream.ReadAsync(readBuffer, cancellationToken);
            if (bytesRead == 0)
                break;

            buffer.AddRange(readBuffer.Take(bytesRead));

            // Check for terminator
            if (buffer.Count >= terminatorBytes.Length)
            {
                var lastBytes = buffer.TakeLast(terminatorBytes.Length).ToArray();
                if (lastBytes.SequenceEqual(terminatorBytes))
                    break;
            }
        }

        _logger.LogTrace("Received {ByteCount} bytes from serial port", buffer.Count);
        return buffer.ToArray();
    }

    private void SetStatus(TransportStatus newStatus)
    {
        if (_status != newStatus)
        {
            var oldStatus = _status;
            _status = newStatus;
            _logger.LogInformation("Serial transport status: {OldStatus} -> {NewStatus}", oldStatus, newStatus);
            _statusSubject.OnNext(newStatus);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await DisconnectAsync();
        _statusSubject.Dispose();
        _dataReceivedSubject.Dispose();
        _workerCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class SerialJob
    {
        public byte[] Command { get; }
        public TaskCompletionSource<byte[]> Completion { get; }

        public SerialJob(byte[] command)
        {
            Command = command;
            Completion = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
