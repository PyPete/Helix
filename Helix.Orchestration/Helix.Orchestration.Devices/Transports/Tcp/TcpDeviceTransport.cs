using System.Net.Sockets;
using System.Reactive.Subjects;
using Helix.Orchestration.Devices.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Transports.Tcp;

/// <summary>
/// TCP socket transport implementation with configurable framing modes.
/// </summary>
public sealed class TcpDeviceTransport : IDeviceTransport
{
    private readonly TcpTransportOptions _options;
    private readonly ILogger<TcpDeviceTransport> _logger;
    private readonly Subject<TransportStatus> _statusSubject;
    private readonly Subject<byte[]> _dataReceivedSubject;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private TcpClient? _client;
    private NetworkStream? _stream;
    private Task? _readLoopTask;
    private CancellationTokenSource? _readLoopCts;
    private TransportStatus _status = TransportStatus.Disconnected;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TcpDeviceTransport"/> class.
    /// </summary>
    public TcpDeviceTransport(TcpTransportOptions options, ILogger<TcpDeviceTransport> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            _client = new TcpClient
            {
                SendTimeout = _options.SendTimeoutMs,
                ReceiveTimeout = _options.ReceiveTimeoutMs
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.ConnectTimeoutMs);

            await _client.ConnectAsync(_options.Host, _options.Port, cts.Token);
            _stream = _client.GetStream();

            _logger.LogInformation("TCP connected to {Host}:{Port}", _options.Host, _options.Port);

            // Start background read loop for unsolicited data
            _readLoopCts = new CancellationTokenSource();
            _readLoopTask = Task.Run(() => ReadLoopAsync(_readLoopCts.Token), _readLoopCts.Token);

            SetStatus(TransportStatus.Connected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to {Host}:{Port}", _options.Host, _options.Port);
            SetStatus(TransportStatus.Faulted);
            throw new TransportFaultException($"Failed to connect to {_options.Host}:{_options.Port}", "TCP", TransportStatus.Connecting, ex);
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

            _readLoopCts?.Cancel();

            if (_readLoopTask != null)
            {
                try
                {
                    await _readLoopTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }

            _stream?.Close();
            _client?.Close();
            _stream = null;
            _client = null;

            _logger.LogInformation("TCP disconnected from {Host}:{Port}", _options.Host, _options.Port);
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
        if (_stream == null || _status != TransportStatus.Connected)
            throw new TransportFaultException("TCP transport is not connected", "TCP", _status);

        try
        {
            // Send command
            await _stream.WriteAsync(command, cancellationToken);
            _logger.LogTrace("Sent {ByteCount} bytes via TCP", command.Length);

            // Read response based on framing mode
            var response = _options.FramingMode switch
            {
                TcpFramingMode.Delimiter => await ReadUntilDelimiterAsync(cancellationToken),
                TcpFramingMode.LengthPrefix => await ReadLengthPrefixedAsync(cancellationToken),
                TcpFramingMode.Raw => await ReadRawAsync(cancellationToken),
                _ => throw new NotSupportedException($"Framing mode {_options.FramingMode} not supported")
            };

            _logger.LogTrace("Received {ByteCount} bytes via TCP", response.Length);
            return response;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "TCP I/O error");
            SetStatus(TransportStatus.Faulted);
            throw new TransportFaultException("TCP I/O error", "TCP", TransportStatus.Connected, ex);
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "TCP socket error");
            SetStatus(TransportStatus.Faulted);
            throw new TransportFaultException("TCP socket error", "TCP", TransportStatus.Connected, ex);
        }
    }

    private async Task<byte[]> ReadUntilDelimiterAsync(CancellationToken cancellationToken)
    {
        var buffer = new List<byte>();
        var readBuffer = new byte[1];

        while (true)
        {
            var bytesRead = await _stream!.ReadAsync(readBuffer, cancellationToken);
            if (bytesRead == 0)
                throw new IOException("Connection closed by remote host");

            buffer.Add(readBuffer[0]);

            if (readBuffer[0] == _options.DelimiterByte)
                break;
        }

        return buffer.ToArray();
    }

    private async Task<byte[]> ReadLengthPrefixedAsync(CancellationToken cancellationToken)
    {
        // Read 4-byte length prefix
        var lengthBuffer = new byte[4];
        await _stream!.ReadExactlyAsync(lengthBuffer, cancellationToken);
        var length = BitConverter.ToInt32(lengthBuffer, 0);

        if (length <= 0 || length > 10_000_000) // 10MB sanity check
            throw new InvalidOperationException($"Invalid length prefix: {length}");

        // Read message body
        var messageBuffer = new byte[length];
        await _stream.ReadExactlyAsync(messageBuffer, cancellationToken);
        return messageBuffer;
    }

    private async Task<byte[]> ReadRawAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var bytesRead = await _stream!.ReadAsync(buffer, cancellationToken);

        if (bytesRead == 0)
            throw new IOException("Connection closed by remote host");

        return buffer.Take(bytesRead).ToArray();
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("TCP read loop started");

        try
        {
            while (!cancellationToken.IsCancellationRequested && _stream != null)
            {
                var buffer = new byte[4096];
                var bytesRead = await _stream.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0)
                {
                    _logger.LogWarning("TCP connection closed by remote host");
                    SetStatus(TransportStatus.Faulted);
                    break;
                }

                var data = buffer.Take(bytesRead).ToArray();
                _dataReceivedSubject.OnNext(data);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("TCP read loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TCP read loop");
            SetStatus(TransportStatus.Faulted);
        }
    }

    private void SetStatus(TransportStatus newStatus)
    {
        if (_status != newStatus)
        {
            var oldStatus = _status;
            _status = newStatus;
            _logger.LogInformation("TCP transport status: {OldStatus} -> {NewStatus}", oldStatus, newStatus);
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
        _readLoopCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
