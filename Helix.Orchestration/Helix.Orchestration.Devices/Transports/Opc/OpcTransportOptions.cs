namespace Helix.Orchestration.Devices.Transports.Opc;

/// <summary>
/// Configuration options for OPC UA transport.
/// </summary>
public sealed class OpcTransportOptions
{
    /// <summary>
    /// OPC UA server endpoint URL (e.g., "opc.tcp://localhost:4840").
    /// </summary>
    public required string EndpointUrl { get; init; }

    /// <summary>
    /// Session timeout in milliseconds (default: 30000).
    /// </summary>
    public int SessionTimeoutMs { get; init; } = 30_000;

    /// <summary>
    /// Subscription publishing interval in milliseconds (default: 200).
    /// </summary>
    public int SubscriptionPublishingIntervalMs { get; init; } = 200;

    /// <summary>
    /// Reconnection check period in milliseconds (default: 5000).
    /// </summary>
    public int ReconnectPeriodMs { get; init; } = 5000;

    /// <summary>
    /// OPC UA security mode (default: None).
    /// </summary>
    public OpcSecurityMode SecurityMode { get; init; } = OpcSecurityMode.None;

    /// <summary>
    /// Application name for the OPC UA client (default: "Helix.Orchestration.Devices").
    /// </summary>
    public string ApplicationName { get; init; } = "Helix.Orchestration.Devices";

    /// <summary>
    /// Application URI for the OPC UA client (default: "urn:Helix:Orchestration:Devices").
    /// </summary>
    public string ApplicationUri { get; init; } = "urn:Helix:Orchestration:Devices";

    /// <summary>
    /// Username for authentication (optional).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Password for authentication (optional).
    /// </summary>
    public string? Password { get; init; }
}

/// <summary>
/// OPC UA security modes.
/// </summary>
public enum OpcSecurityMode
{
    /// <summary>
    /// No security (plain text communication).
    /// </summary>
    None,

    /// <summary>
    /// Messages are signed but not encrypted.
    /// </summary>
    Sign,

    /// <summary>
    /// Messages are signed and encrypted.
    /// </summary>
    SignAndEncrypt
}
