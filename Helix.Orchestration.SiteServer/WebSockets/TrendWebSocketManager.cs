using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Helix.Orchestration.SiteServer.WebSockets;

/// <summary>
/// Wire-format DTO for trend values sent to dashboard WebSocket clients.
/// Matches the frontend TrendValue interface: { type, trendName, value, unit, timestamp }.
/// </summary>
public sealed record TrendValueWireDto(
    [property: JsonPropertyName("type")]      string Type,
    [property: JsonPropertyName("trendName")] string TrendName,
    [property: JsonPropertyName("value")]     double Value,
    [property: JsonPropertyName("unit")]      string Unit,
    [property: JsonPropertyName("timestamp")] string Timestamp
);

/// <summary>
/// Singleton WebSocket relay for live trend streaming to dashboard clients.
/// MqttTrendBridge calls BroadcastAsync on each MQTT tick.
/// Dead sockets are removed on every BroadcastAsync (T-7-04 DoS mitigation).
/// </summary>
public sealed class TrendWebSocketManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public void Register(string clientId, WebSocket socket) =>
        _clients[clientId] = socket;

    public void Remove(string clientId) =>
        _clients.TryRemove(clientId, out _);

    public async Task HandleClientAsync(WebSocket ws, CancellationToken ct)
    {
        var clientId = Guid.NewGuid().ToString("N");
        Register(clientId, ws);
        try
        {
            var buffer = new byte[256];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, "done",
                        CancellationToken.None);
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            Remove(clientId);
        }
    }

    /// <summary>
    /// Broadcast a trend value DTO to all connected dashboard clients.
    /// Removes dead sockets on each call (T-7-04 mitigation).
    /// </summary>
    public async Task BroadcastAsync(TrendValueWireDto dto, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        foreach (var (clientId, ws) in _clients.ToArray())
        {
            if (ws.State != WebSocketState.Open) { Remove(clientId); continue; }
            try
            {
                await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
            }
            catch
            {
                Remove(clientId);
            }
        }
    }
}
