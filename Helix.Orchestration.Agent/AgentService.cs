using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Agent.Events;
using Helix.Orchestration.Agent.Streaming;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Safety;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent;

/// <summary>
/// Hosted service that manages WebSocket client connections and broadcasts
/// <see cref="RunspaceEvent"/> frames to all connected clients.
/// Also publishes live trend and safety events sourced from
/// <see cref="TrendAggregator"/> and <see cref="ISafetyEngine"/>.
/// </summary>
public sealed class AgentService(
    IRunspaceEventEmitter emitter,
    TrendAggregator trendAggregator,
    ISafetyEngine safetyEngine,
    CapabilityRegistry registry,
    ILogger<AgentService> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private readonly HashSet<string> _prevViolations = [];
    private readonly Dictionary<string, string> _adapterStatus = new();
    private CancellationTokenSource _cts = new();

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        _ = BroadcastLoopAsync(_cts.Token);
        _ = TrendForwardLoopAsync(_cts.Token);
        _ = SafetyPollLoopAsync(_cts.Token);
        _ = DeviceHealthPollLoopAsync(_cts.Token);
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

    /// <summary>
    /// Background loop that reads scalar trend values from <see cref="TrendAggregator"/>
    /// and publishes them as <see cref="TrendValueEvent"/> frames.
    /// </summary>
    private async Task TrendForwardLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var notification in trendAggregator.ScalarValues.ReadAllAsync(ct))
            {
                emitter.Publish(new TrendValueEvent(
                    notification.TrendName,
                    notification.Value,
                    notification.Unit,
                    DateTimeOffset.UtcNow));
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trend forward loop terminated unexpectedly.");
        }
    }

    /// <summary>
    /// Background polling loop that checks <see cref="ISafetyEngine"/> for interlock violations
    /// every 2 seconds and publishes <see cref="SafetyEvent"/> frames when the violation set changes.
    /// </summary>
    private async Task SafetyPollLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);

                var violations = (await safetyEngine.GetViolationsAsync()).ToList();
                var currentIds = violations.Select(v => v.RuleId).ToHashSet();

                // Publish events for newly triggered violations
                foreach (var violation in violations.Where(v => !_prevViolations.Contains(v.RuleId)))
                {
                    emitter.Publish(new SafetyEvent(
                        violation.RuleId,
                        violation.Description,
                        violation.Severity.ToString(),
                        true,
                        DateTimeOffset.UtcNow));
                }

                // Publish cleared events for violations that resolved since last poll
                foreach (var clearedId in _prevViolations.Where(id => !currentIds.Contains(id)))
                {
                    emitter.Publish(new SafetyEvent(
                        clearedId,
                        string.Empty,
                        string.Empty,
                        false,
                        DateTimeOffset.UtcNow));
                }

                _prevViolations.Clear();
                foreach (var id in currentIds)
                    _prevViolations.Add(id);
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Safety poll loop terminated unexpectedly.");
        }
    }

    /// <summary>
    /// Background polling loop that checks adapter availability every 5 seconds via
    /// CapabilityRegistry.IsAvailableAsync and publishes a DeviceStatusEvent
    /// whenever an adapter availability changes.
    /// </summary>
    private async Task DeviceHealthPollLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);

                var adapterNames = registry.GetAllCapabilityNames().ToList();

                foreach (var adapterName in adapterNames)
                {
                    bool available = await registry.IsAvailableAsync(adapterName);
                    string status = available ? "online" : "offline";

                    if (!_adapterStatus.TryGetValue(adapterName, out var lastStatus) || lastStatus != status)
                    {
                        emitter.Publish(new DeviceStatusEvent(adapterName, status, new[] { adapterName }));
                        _adapterStatus[adapterName] = status;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Device health poll loop terminated unexpectedly.");
        }
    }
}
