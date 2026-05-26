using System.Reactive.Subjects;
using FluentModbus;
using Helix.Orchestration.Devices.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Transports.Modbus;

/// <summary>
/// Modbus transport implementation using FluentModbus.
/// Currently supports RTU mode; TCP mode is extensible.
/// </summary>
public sealed class ModbusDeviceTransport : IDeviceTransport
{
    private readonly ModbusTransportOptions _options;
    private readonly ILogger<ModbusDeviceTransport> _logger;
    private readonly IModbusClientFactory _clientFactory;
    private readonly Subject<TransportStatus> _statusSubject;
    private readonly Subject<byte[]> _dataReceivedSubject;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IModbusClient? _client;
    private TransportStatus _status = TransportStatus.Disconnected;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModbusDeviceTransport"/> class.
    /// </summary>
    public ModbusDeviceTransport(ModbusTransportOptions options, ILogger<ModbusDeviceTransport> logger)
        : this(options, logger, new ModbusClientFactory())
    {
    }

    internal ModbusDeviceTransport(
        ModbusTransportOptions options,
        ILogger<ModbusDeviceTransport> logger,
        IModbusClientFactory clientFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _statusSubject = new Subject<TransportStatus>();
        _dataReceivedSubject = new Subject<byte[]>();

        if (_options.Mode == ModbusMode.Tcp)
            throw new NotSupportedException("Modbus TCP is not yet implemented");
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

            if (_options.Mode == ModbusMode.Rtu)
            {
                if (string.IsNullOrWhiteSpace(_options.PortName))
                    throw new ArgumentException("PortName is required for RTU mode");

                _client = _clientFactory.Create(_options);
                _client.Connect(_options.PortName, ModbusEndianness.BigEndian);

                _logger.LogInformation("Modbus RTU connected on {PortName} at {BaudRate} baud", _options.PortName, _options.BaudRate);
            }

            SetStatus(TransportStatus.Connected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect Modbus transport");
            SetStatus(TransportStatus.Faulted);
            throw new TransportFaultException("Failed to connect Modbus transport", "Modbus RTU", TransportStatus.Connecting, ex);
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

            _client?.Close();
            _client?.Dispose();
            _client = null;

            _logger.LogInformation("Modbus transport disconnected");
            SetStatus(TransportStatus.Disconnected);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> SendReceiveAsync(byte[] command, CancellationToken cancellationToken = default)
    {
        // Generic byte[] send-receive not directly supported by FluentModbus
        // Use typed methods (ReadHoldingRegistersAsync, etc.) instead
        throw new NotSupportedException("Use typed Modbus methods like ReadHoldingRegistersAsync instead of SendReceiveAsync");
    }

    /// <summary>
    /// Reads holding registers from the Modbus device.
    /// </summary>
    public async Task<byte[]> ReadHoldingRegistersAsync(int startingAddress, int quantity, CancellationToken cancellationToken = default)
    {
        if (_client == null || _status != TransportStatus.Connected)
            throw new TransportFaultException("Modbus transport is not connected", "Modbus RTU", _status);

        try
        {
            var result = await Task.Run(() => _client.ReadHoldingRegisters(_options.SlaveId, checked((ushort)startingAddress), checked((ushort)quantity)), cancellationToken);
            _logger.LogTrace("Read {Count} holding registers from address {Address}", quantity, startingAddress);
            return result;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IOException during Modbus read");
            SetStatus(TransportStatus.Faulted);
            throw new TransportFaultException("Modbus I/O error", "Modbus RTU", TransportStatus.Connected, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Modbus holding registers");
            throw;
        }
    }

    /// <summary>
    /// Reads input registers from the Modbus device.
    /// </summary>
    public async Task<byte[]> ReadInputRegistersAsync(int startingAddress, int quantity, CancellationToken cancellationToken = default)
    {
        if (_client == null || _status != TransportStatus.Connected)
            throw new TransportFaultException("Modbus transport is not connected", "Modbus RTU", _status);

        try
        {
            var result = await Task.Run(() => _client.ReadInputRegisters(_options.SlaveId, checked((ushort)startingAddress), checked((ushort)quantity)), cancellationToken);
            _logger.LogTrace("Read {Count} input registers from address {Address}", quantity, startingAddress);
            return result;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IOException during Modbus read");
            SetStatus(TransportStatus.Faulted);
            throw new TransportFaultException("Modbus I/O error", "Modbus RTU", TransportStatus.Connected, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Modbus input registers");
            throw;
        }
    }

    /// <summary>
    /// Writes multiple registers to the Modbus device.
    /// </summary>
    public async Task WriteMultipleRegistersAsync(int startingAddress, byte[] data, CancellationToken cancellationToken = default)
    {
        if (_client == null || _status != TransportStatus.Connected)
            throw new TransportFaultException("Modbus transport is not connected", "Modbus RTU", _status);

        try
        {
            await Task.Run(() => _client.WriteMultipleRegisters(_options.SlaveId, checked((ushort)startingAddress), data), cancellationToken);
            _logger.LogTrace("Wrote {Count} bytes to registers starting at address {Address}", data.Length, startingAddress);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IOException during Modbus write");
            SetStatus(TransportStatus.Faulted);
            throw new TransportFaultException("Modbus I/O error", "Modbus RTU", TransportStatus.Connected, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing Modbus registers");
            throw;
        }
    }

    /// <summary>
    /// Reads coils from the Modbus device.
    /// </summary>
    public async Task<byte[]> ReadCoilsAsync(int startingAddress, int quantity, CancellationToken cancellationToken = default)
    {
        if (_client == null || _status != TransportStatus.Connected)
            throw new TransportFaultException("Modbus transport is not connected", "Modbus RTU", _status);

        try
        {
            var result = await Task.Run(() => _client.ReadCoils(_options.SlaveId, checked((ushort)startingAddress), checked((ushort)quantity)), cancellationToken);
            _logger.LogTrace("Read {Count} coils from address {Address}", quantity, startingAddress);
            return result;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IOException during Modbus read");
            SetStatus(TransportStatus.Faulted);
            throw new TransportFaultException("Modbus I/O error", "Modbus RTU", TransportStatus.Connected, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Modbus coils");
            throw;
        }
    }

    private void SetStatus(TransportStatus newStatus)
    {
        if (_status != newStatus)
        {
            var oldStatus = _status;
            _status = newStatus;
            _logger.LogInformation("Modbus transport status: {OldStatus} -> {NewStatus}", oldStatus, newStatus);
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
        GC.SuppressFinalize(this);
    }
}
