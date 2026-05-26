using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Opc;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for a Systag OPC UA server.
/// Communicates over an OPC UA transport to read/write/subscribe to nodes.
/// </summary>
public sealed class SystagOpcUaClient : DeviceAdapterBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="SystagOpcUaClient"/>.
    /// </summary>
    /// <param name="transport">The OPC UA transport to the Systag server.</param>
    /// <param name="logger">Logger instance.</param>
    public SystagOpcUaClient(OpcDeviceTransport transport, ILogger<SystagOpcUaClient> logger)
        : base(transport, logger)
    {
    }

    /// <summary>
    /// Internal constructor for testing with a mock transport.
    /// </summary>
    /// <param name="transport">The device transport (may be a test double).</param>
    /// <param name="logger">Logger instance.</param>
    internal SystagOpcUaClient(IDeviceTransport transport, ILogger<SystagOpcUaClient> logger)
        : base(transport, logger)
    {
    }

    // ── OPC command helpers ───────────────────────────────────────────────────

    private static byte[] OpcRead(string nodeId)
        => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { nodeId, operation = "read" }));

    private static byte[] OpcWrite(string nodeId, object value)
        => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { nodeId, operation = "write", value }));

    private static byte[] OpcSubscribe(string nodeId)
        => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { nodeId, operation = "subscribe" }));

    private static string DecodeUtf8(byte[] response)
        => Encoding.UTF8.GetString(response).Trim();

    // ── Capability registration ───────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // 1. GetSystemInfo — read server root node as a connectivity check
        builder.RegisterSimple(
            "GetSystemInfo",
            "Read OPC UA server root node as a connectivity check",
            async ct =>
            {
                var response = await Transport.SendReceiveAsync(OpcRead("ns=0;i=84"), ct);
                return CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(DecodeUtf8(response)));
            });

        // 2. ReadNode — read a node value by caller-supplied node ID
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "ReadNode",
                "Read an OPC UA node value by node ID (caller-supplied)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"nodeId\":{\"type\":\"string\"}},\"required\":[\"nodeId\"]}"),
            async (parameters, _, ct) =>
            {
                var nodeId = parameters.GetProperty("nodeId").GetString()
                    ?? throw new ArgumentException("nodeId is required");

                var response = await Transport.SendReceiveAsync(OpcRead(nodeId), ct);
                return CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(new { nodeId, value = DecodeUtf8(response) }));
            });

        // 3. WriteNode — write a value to a node by caller-supplied node ID
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "WriteNode",
                "Write a value to an OPC UA node by node ID (caller-supplied; OPC UA server enforces range limits)",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"nodeId\":{\"type\":\"string\"},\"value\":{\"type\":\"number\"}},\"required\":[\"nodeId\",\"value\"]}"),
            async (parameters, _, ct) =>
            {
                var nodeId = parameters.GetProperty("nodeId").GetString()
                    ?? throw new ArgumentException("nodeId is required");
                var value = parameters.GetProperty("value").GetDouble();

                var response = await Transport.SendReceiveAsync(OpcWrite(nodeId, value), ct);
                var responseText = DecodeUtf8(response);
                return responseText.Contains("\"Success\":true", StringComparison.OrdinalIgnoreCase)
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed(responseText);
            });

        // 4. SubscribeNode — subscribe to live value updates from a node
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SubscribeNode",
                "Subscribe to live value updates from an OPC UA node",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"nodeId\":{\"type\":\"string\"}},\"required\":[\"nodeId\"]}"),
            async (parameters, _, ct) =>
            {
                var nodeId = parameters.GetProperty("nodeId").GetString()
                    ?? throw new ArgumentException("nodeId is required");

                var response = await Transport.SendReceiveAsync(OpcSubscribe(nodeId), ct);
                var responseText = DecodeUtf8(response);
                return responseText.Contains("\"Success\":true", StringComparison.OrdinalIgnoreCase)
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed(responseText);
            });
    }

    // ── Trend streaming ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Transport is not OpcDeviceTransport opcTransport)
            yield break;

        var channel = Channel.CreateUnbounded<TrendSample>();

        using var sub = opcTransport.DataReceived.Subscribe(data =>
        {
            try
            {
                using var doc = JsonDocument.Parse(data);
                var nodeId = doc.RootElement.GetProperty("NodeId").GetString() ?? "unknown";
                var value = doc.RootElement.GetProperty("Value").GetDouble();
                channel.Writer.TryWrite(new TrendSample
                {
                    TrendName = $"systag.{nodeId}",
                    Value = value,
                    Timestamp = DateTimeOffset.UtcNow,
                    Unit = ""
                });
            }
            catch { /* skip unparseable subscription events */ }
        });

        await foreach (var sample in channel.Reader.ReadAllAsync(cancellationToken))
            yield return sample;
    }
}
