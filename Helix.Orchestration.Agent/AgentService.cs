using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Agent.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent;

/// <summary>
/// Hosted service that manages WebSocket client connections and broadcasts
/// <see cref="RunspaceEvent"/> frames to all connected clients.
/// </summary>
public sealed class AgentService(
    IRunspaceEventEmitter emitter,
    ILogger<AgentService> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private CancellationTokenSource _cts = new();

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        _ = BroadcastLoopAsync(_cts.Token);
        logger.LogInformation("Helix Agent service started.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        logger.LogInformation("Stopping Helix Agent service.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Keeps a WebSocket client connected until it disconnects or the token is canceled.
    /// Called by the /ws middleware after accepting the upgrade.
    /// </summary>
    /// <param name="ws">The accepted WebSocket connection.</param>
    /// <param name="ct">Cancellation token tied to the HTTP request lifetime.</param>
    public async Task HandleClientAsync(WebSocket ws, CancellationToken ct)
    {
        var clientId = Guid.NewGuid().ToString("N");
        _clients[clientId] = ws;
        logger.LogInformation("WebSocket client {ClientId} connected.", clientId);

        try
        {
            var buffer = new byte[1024];
            while (!ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (WebSocketException ex)
        {
            logger.LogWarning(ex, "WebSocket error for client {ClientId}.", clientId);
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            logger.LogInformation("WebSocket client {ClientId} disconnected.", clientId);
            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
                }
                catch
                {
                    // best-effort close
                }
            }
        }
    }

    /// <summary>
    /// Background loop that reads events from the emitter and broadcasts JSON frames
    /// to all connected WebSocket clients.
    /// </summary>
    private async Task BroadcastLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var evt in emitter.Events.ReadAllAsync(ct))
            {
                var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var bytes = Encoding.UTF8.GetBytes(json);

                foreach (var (clientId, ws) in _clients.ToArray())
                {
                    if (ws.State != WebSocketState.Open)
                    {
                        _clients.TryRemove(clientId, out _);
                        continue;
                    }

                    try
                    {
                        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to send event to client {ClientId}; removing.", clientId);
                        _clients.TryRemove(clientId, out _);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }
}
