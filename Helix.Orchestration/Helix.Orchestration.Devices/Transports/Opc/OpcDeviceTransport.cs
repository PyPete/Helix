using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Microsoft.Extensions.Logging;
using Opc.Ua;

namespace Helix.Orchestration.Devices.Transports.Opc;

/// <summary>
/// OPC UA device transport wrapping an IOpcSession implementation.
/// </summary>
public sealed class OpcDeviceTransport : IDeviceTransport
{
    private readonly OpcTransportOptions _options;
    private readonly ILogger<OpcDeviceTransport> _logger;
    private readonly IOpcSessionFactory _sessionFactory;
    private readonly Subject<TransportStatus> _statusSubject;
    private readonly Subject<byte[]> _dataReceivedSubject;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly List<IOpcSubscription> _subscriptions = [];

    private IOpcSession? _session;
    private Task? _watchdogTask;
    private CancellationTokenSource? _watchdogCts;
    private TransportStatus _status = TransportStatus.Disconnected;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpcDeviceTransport"/> class.
    /// </summary>
    public OpcDeviceTransport(OpcTransportOptions options, ILogger<OpcDeviceTransport> logger)
        : this(options, logger, new OpcFoundationSessionFactory())
    {
    }

    internal OpcDeviceTransport(
        OpcTransportOptions options,
        ILogger<OpcDeviceTransport> logger,
        IOpcSessionFactory sessionFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
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

            // Create OPC UA application configuration
            var appConfig = new ApplicationConfiguration
            {
                ApplicationName = _options.ApplicationName,
                ApplicationUri = _options.ApplicationUri,
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = _options.SessionTimeoutMs },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = _options.SessionTimeoutMs }
            };

            await appConfig.Validate(ApplicationType.Client);

            _session = _sessionFactory.Create(_options, appConfig);

            if (_session is OpcFoundationSession foundationSession)
            {
                await foundationSession.ConnectAsync(cancellationToken);
            }

            _logger.LogInformation("Connected to OPC UA server at {EndpointUrl}", _options.EndpointUrl);

            // Start session watchdog
            _watchdogCts = new CancellationTokenSource();
            _watchdogTask = Task.Run(() => SessionWatchdogAsync(_watchdogCts.Token), _watchdogCts.Token);

            SetStatus(TransportStatus.Connected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to OPC UA server");
            SetStatus(TransportStatus.Faulted);
            throw new TransportFaultException($"Failed to connect to OPC UA server at {_options.EndpointUrl}", "OPC UA", TransportStatus.Connecting, ex);
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

            _watchdogCts?.Cancel();

            if (_watchdogTask != null)
            {
                try
                {
                    await _watchdogTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }

            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            if (_session != null)
            {
                await _session.DisposeAsync();
                _session = null;
            }

            _logger.LogInformation("Disconnected from OPC UA server");
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
        if (_session == null || _status != TransportStatus.Connected)
            throw new TransportFaultException("OPC UA transport is not connected", "OPC UA", _status);

        try
        {
            // Decode command as JSON with nodeId
            var commandJson = Encoding.UTF8.GetString(command);
            var commandObj = JsonSerializer.Deserialize<OpcCommand>(commandJson)
                ?? throw new ArgumentException("Invalid OPC command format");

            object? result = null;

            switch (commandObj.Operation?.ToLowerInvariant())
            {
                case "read":
                    result = await _session.ReadNodeAsync<object>(commandObj.NodeId, cancellationToken);
                    break;

                case "write":
                    if (commandObj.Value == null)
                        throw new ArgumentException("Write operation requires a Value property");
                    await _session.WriteNodeAsync(commandObj.NodeId, commandObj.Value, cancellationToken);
                    result = new { Success = true };
                    break;

                case "call":
                    if (commandObj.MethodId == null || commandObj.Parameters == null)
                        throw new ArgumentException("Call operation requires MethodId and Parameters");
                    var methodResult = await _session.CallMethodAsync(commandObj.NodeId, commandObj.MethodId, commandObj.Parameters, cancellationToken);
                    result = methodResult;
                    break;

                case "subscribe":
                    var subscription = await _session.SubscribeNodeAsync(
                        commandObj.NodeId,
                        value => PublishSubscriptionValue(commandObj.NodeId, value),
                        cancellationToken: cancellationToken);
                    _subscriptions.Add(subscription);
                    result = new { Success = true, SubscribedNode = commandObj.NodeId };
                    break;

                default:
                    throw new NotSupportedException($"OPC operation '{commandObj.Operation}' not supported");
            }

            var responseJson = JsonSerializer.Serialize(result);
            return Encoding.UTF8.GetBytes(responseJson);
        }
        catch (TransportFaultException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing OPC UA command");
            throw new TransportFaultException("OPC UA command execution failed", "OPC UA", TransportStatus.Connected, ex);
        }
    }

    private void PublishSubscriptionValue(string nodeId, object? value)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new { NodeId = nodeId, Value = value });
        _dataReceivedSubject.OnNext(payload);
    }

    private async Task SessionWatchdogAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("OPC UA session watchdog started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.ReconnectPeriodMs, cancellationToken);

                if (_session != null && !_session.IsConnected)
                {
                    _logger.LogWarning("OPC UA session disconnected, setting status to Reconnecting");
                    SetStatus(TransportStatus.Reconnecting);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OPC UA session watchdog");
            }
        }

        _logger.LogDebug("OPC UA session watchdog stopped");
    }

    private void SetStatus(TransportStatus newStatus)
    {
        if (_status != newStatus)
        {
            var oldStatus = _status;
            _status = newStatus;
            _logger.LogInformation("OPC UA transport status: {OldStatus} -> {NewStatus}", oldStatus, newStatus);
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
        _watchdogCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class OpcCommand
    {
        public required string NodeId { get; set; }
        public string? Operation { get; set; } = "read";
        public object? Value { get; set; }
        public string? MethodId { get; set; }
        public object[]? Parameters { get; set; }
    }
}
