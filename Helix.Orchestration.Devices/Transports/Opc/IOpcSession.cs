namespace Helix.Orchestration.Devices.Transports.Opc;

/// <summary>
/// Abstraction for OPC UA session operations.
/// This interface allows swapping the underlying OPC UA library without affecting transport code.
/// </summary>
public interface IOpcSession : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the session is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Reads a value from an OPC UA node.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="nodeId">The OPC UA node identifier (e.g., "ns=2;s=Temperature").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The node value.</returns>
    Task<T?> ReadNodeAsync<T>(string nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a value to an OPC UA node.
    /// </summary>
    /// <param name="nodeId">The OPC UA node identifier.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteNodeAsync(string nodeId, object value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to changes on an OPC UA node.
    /// </summary>
    /// <param name="nodeId">The OPC UA node identifier.</param>
    /// <param name="callback">Callback invoked when the node value changes.</param>
    /// <param name="samplingIntervalMs">Sampling interval in milliseconds (default: 1000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A subscription handle that can be disposed to unsubscribe.</returns>
    Task<IOpcSubscription> SubscribeNodeAsync(
        string nodeId,
        Action<object?> callback,
        int samplingIntervalMs = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls an OPC UA method.
    /// </summary>
    /// <param name="objectId">The object node identifier that contains the method.</param>
    /// <param name="methodId">The method node identifier.</param>
    /// <param name="parameters">Method input parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Method output parameters.</returns>
    Task<object[]> CallMethodAsync(
        string objectId,
        string methodId,
        object[] parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Browses the child nodes of a given parent node.
    /// </summary>
    /// <param name="parentNodeId">The parent node identifier (null or empty for root).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of child node descriptors.</returns>
    Task<IReadOnlyList<OpcNodeDescriptor>> BrowseChildrenAsync(
        string? parentNodeId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an active OPC UA subscription.
/// </summary>
public interface IOpcSubscription : IDisposable
{
    /// <summary>
    /// Gets the node ID being monitored.
    /// </summary>
    string NodeId { get; }

    /// <summary>
    /// Gets a value indicating whether the subscription is active.
    /// </summary>
    bool IsActive { get; }
}

/// <summary>
/// Describes an OPC UA node discovered during browsing.
/// </summary>
public sealed class OpcNodeDescriptor
{
    /// <summary>
    /// The node identifier.
    /// </summary>
    public required string NodeId { get; init; }

    /// <summary>
    /// The display name of the node.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The node class (Variable, Object, Method, etc.).
    /// </summary>
    public string? NodeClass { get; init; }

    /// <summary>
    /// Whether the node has children.
    /// </summary>
    public bool HasChildren { get; init; }
}
