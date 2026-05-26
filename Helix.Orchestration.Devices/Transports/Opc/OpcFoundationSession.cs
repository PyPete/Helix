using Helix.Orchestration.Devices.Abstractions;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;

namespace Helix.Orchestration.Devices.Transports.Opc;

/// <summary>
/// OPC UA session implementation using the OPC Foundation .NET Standard library.
/// </summary>
public sealed class OpcFoundationSession : IOpcSession
{
    private readonly string _endpointUrl;
    private readonly ILogger<OpcFoundationSession> _logger;
    private readonly ApplicationConfiguration _appConfig;
    private readonly List<OpcFoundationSubscription> _activeSubscriptions = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    private ISession? _session;
    private Subscription? _defaultSubscription;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpcFoundationSession"/> class.
    /// </summary>
    public OpcFoundationSession(string endpointUrl, ApplicationConfiguration appConfig, ILogger<OpcFoundationSession> logger)
    {
        _endpointUrl = endpointUrl ?? throw new ArgumentNullException(nameof(endpointUrl));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool IsConnected => _session?.Connected ?? false;

    /// <summary>
    /// Connects to the OPC UA server and creates a session.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_session != null && _session.Connected)
            {
                _logger.LogWarning("Already connected to OPC UA server");
                return;
            }

            var endpoint = CoreClientUtils.SelectEndpoint(_endpointUrl, false);
            var endpointConfiguration = EndpointConfiguration.Create(_appConfig);
            var configuredEndpoint = new ConfiguredEndpoint(null, endpoint, endpointConfiguration);

            _session = await Session.Create(
                _appConfig,
                configuredEndpoint,
                false,
                $"Helix-{Guid.NewGuid():N}",
                60000,
                new UserIdentity(new AnonymousIdentityToken()),
                null);

            _logger.LogInformation("Connected to OPC UA server at {EndpointUrl}", _endpointUrl);

            // Create default subscription for monitored items
            _defaultSubscription = new Subscription(_session.DefaultSubscription)
            {
                PublishingInterval = 1000,
                PublishingEnabled = true
            };

            _session.AddSubscription(_defaultSubscription);
            _defaultSubscription.Create();
        }
        catch (ServiceResultException ex)
        {
            _logger.LogError(ex, "OPC UA service error during connection");
            throw new TransportFaultException($"Failed to connect to OPC UA server: {ex.Message}", "OPC UA", TransportStatus.Connecting, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to OPC UA server");
            throw new TransportFaultException($"Failed to connect to OPC UA server", "OPC UA", TransportStatus.Connecting, ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<T?> ReadNodeAsync<T>(string nodeId, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var node = new NodeId(nodeId);
            var value = await Task.Run(() => _session!.ReadValue(node), cancellationToken);
            var rawValue = value?.Value;

            _logger.LogTrace("Read node {NodeId}: {Value}", nodeId, rawValue);

            if (rawValue is null)
                return default;

            if (rawValue is T typedValue)
                return typedValue;

            return (T?)Convert.ChangeType(rawValue, typeof(T));
        }
        catch (ServiceResultException ex)
        {
            _logger.LogError(ex, "OPC UA service error reading node {NodeId}", nodeId);
            throw new TransportFaultException($"Failed to read OPC UA node {nodeId}", "OPC UA", TransportStatus.Connected, ex);
        }
    }

    /// <inheritdoc/>
    public async Task WriteNodeAsync(string nodeId, object value, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var node = new NodeId(nodeId);
            var writeValue = new WriteValue
            {
                NodeId = node,
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(value))
            };

            var writeValueCollection = new WriteValueCollection { writeValue };

            await Task.Run(() =>
            {
                _session!.Write(null, writeValueCollection, out var results, out _);
                if (results == null || results.Count == 0 || StatusCode.IsBad(results[0]))
                {
                    throw new ServiceResultException(results != null && results.Count > 0 ? results[0] : StatusCodes.BadUnexpectedError);
                }
            }, cancellationToken);

            _logger.LogTrace("Wrote value {Value} to node {NodeId}", value, nodeId);
        }
        catch (ServiceResultException ex)
        {
            _logger.LogError(ex, "OPC UA service error writing node {NodeId}", nodeId);
            throw new TransportFaultException($"Failed to write OPC UA node {nodeId}", "OPC UA", TransportStatus.Connected, ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IOpcSubscription> SubscribeNodeAsync(
        string nodeId,
        Action<object?> callback,
        int samplingIntervalMs = 1000,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var node = new NodeId(nodeId);
            var monitoredItem = new MonitoredItem(_defaultSubscription!.DefaultItem)
            {
                StartNodeId = node,
                AttributeId = Attributes.Value,
                SamplingInterval = samplingIntervalMs,
                QueueSize = 10,
                DiscardOldest = true
            };

            monitoredItem.Notification += (item, e) =>
            {
                if (e.NotificationValue is MonitoredItemNotification notification)
                {
                    callback(notification.Value.Value);
                }
            };

            _defaultSubscription.AddItem(monitoredItem);
            await Task.Run(() => _defaultSubscription.ApplyChanges(), cancellationToken);

            var subscription = new OpcFoundationSubscription(nodeId, monitoredItem, _defaultSubscription);
            _activeSubscriptions.Add(subscription);

            _logger.LogInformation("Subscribed to OPC UA node {NodeId} with {SamplingInterval}ms sampling", nodeId, samplingIntervalMs);

            return subscription;
        }
        catch (ServiceResultException ex)
        {
            _logger.LogError(ex, "OPC UA service error subscribing to node {NodeId}", nodeId);
            throw new TransportFaultException($"Failed to subscribe to OPC UA node {nodeId}", "OPC UA", TransportStatus.Connected, ex);
        }
    }

    /// <inheritdoc/>
    public async Task<object[]> CallMethodAsync(
        string objectId,
        string methodId,
        object[] parameters,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var objectNode = new NodeId(objectId);
            var methodNode = new NodeId(methodId);
            var inputArguments = new VariantCollection(parameters.Select(p => new Variant(p)));

            var results = await Task.Run(() =>
                _session!.Call(objectNode, methodNode, inputArguments), cancellationToken);

            _logger.LogTrace("Called OPC UA method {MethodId} on object {ObjectId}", methodId, objectId);

            return results.Cast<object>().ToArray();
        }
        catch (ServiceResultException ex)
        {
            _logger.LogError(ex, "OPC UA service error calling method {MethodId}", methodId);
            throw new TransportFaultException($"Failed to call OPC UA method {methodId}", "OPC UA", TransportStatus.Connected, ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OpcNodeDescriptor>> BrowseChildrenAsync(
        string? parentNodeId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var node = string.IsNullOrWhiteSpace(parentNodeId)
                ? ObjectIds.RootFolder
                : new NodeId(parentNodeId);

            var browseDescription = new BrowseDescription
            {
                NodeId = node,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                IncludeSubtypes = true,
                NodeClassMask = 0,
                ResultMask = (uint)BrowseResultMask.All
            };

            var results = await Task.Run(() =>
            {
                _session!.Browse(null, null, 0, new BrowseDescriptionCollection { browseDescription },
                    out var browseResults, out _);
                return browseResults;
            }, cancellationToken);

            var children = new List<OpcNodeDescriptor>();

            foreach (var reference in results[0].References)
            {
                children.Add(new OpcNodeDescriptor
                {
                    NodeId = reference.NodeId.ToString(),
                    DisplayName = reference.DisplayName.Text,
                    NodeClass = reference.NodeClass.ToString(),
                    HasChildren = reference.NodeClass == NodeClass.Object || reference.NodeClass == NodeClass.ObjectType
                });
            }

            _logger.LogTrace("Browsed {Count} children of node {ParentNodeId}", children.Count, parentNodeId ?? "Root");

            return children;
        }
        catch (ServiceResultException ex)
        {
            _logger.LogError(ex, "OPC UA service error browsing node {ParentNodeId}", parentNodeId);
            throw new TransportFaultException($"Failed to browse OPC UA node {parentNodeId}", "OPC UA", TransportStatus.Connected, ex);
        }
    }

    private void EnsureConnected()
    {
        if (_session == null || !_session.Connected)
            throw new TransportFaultException("OPC UA session is not connected", "OPC UA", TransportStatus.Disconnected);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _lock.WaitAsync();
        try
        {
            foreach (var subscription in _activeSubscriptions)
            {
                subscription.Dispose();
            }
            _activeSubscriptions.Clear();

            _session?.Close();
            _session?.Dispose();
            _session = null;

            _logger.LogInformation("OPC UA session closed");
        }
        finally
        {
            _lock.Release();
        }

        GC.SuppressFinalize(this);
    }

    private sealed class OpcFoundationSubscription : IOpcSubscription
    {
        private readonly MonitoredItem _monitoredItem;
        private readonly Subscription _subscription;
        private bool _disposed;

        public OpcFoundationSubscription(string nodeId, MonitoredItem monitoredItem, Subscription subscription)
        {
            NodeId = nodeId;
            _monitoredItem = monitoredItem;
            _subscription = subscription;
        }

        public string NodeId { get; }
        public bool IsActive => !_disposed && _monitoredItem.Created;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _subscription.RemoveItem(_monitoredItem);
            _subscription.ApplyChanges();
        }
    }
}
