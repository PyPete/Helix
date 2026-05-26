using System.ComponentModel;

namespace Helix.Orchestration.Devices.Abstractions;

/// <summary>
/// Represents the connection status of a device transport.
/// </summary>
public enum TransportStatus
{
    /// <summary>
    /// The transport is disconnected and not attempting to connect.
    /// </summary>
    [Description("Disconnected")]
    Disconnected = 0,

    /// <summary>
    /// The transport is attempting to establish a connection.
    /// </summary>
    [Description("Connecting")]
    Connecting = 1,

    /// <summary>
    /// The transport is connected and operational.
    /// </summary>
    [Description("Connected")]
    Connected = 2,

    /// <summary>
    /// The transport has experienced an unrecoverable fault and requires reconnection.
    /// </summary>
    [Description("Faulted")]
    Faulted = 3,

    /// <summary>
    /// The transport is attempting to reconnect after a fault or disconnection.
    /// </summary>
    [Description("Reconnecting")]
    Reconnecting = 4
}
