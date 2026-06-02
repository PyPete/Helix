using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using Helix.Orchestration.Core.Trends;
using Helix.Orchestration.SiteServer.WebSockets;

namespace Helix.Orchestration.SiteServer.Mqtt;

/// <summary>
/// BackgroundService that bridges ITrendProvider.Values observables to MQTT topics.
/// Topic structure: helix/{siteId}/{fumehoodId}/{experimentId}/trends/{kind}/{trendId}
/// </summary>
public sealed class MqttTrendBridge : BackgroundService
{
    private readonly ITrendProvider _trendProvider;
    private readonly TrendWebSocketManager _trendWsManager;
    private readonly IOptions<MqttBridgeOptions> _options;
    private readonly ILogger<MqttTrendBridge> _logger;
    private IMqttClient? _mqttClient;
    private readonly List<IDisposable> _subscriptions = new();

    public MqttTrendBridge(
        ITrendProvider trendProvider,
        TrendWebSocketManager trendWsManager,
        IOptions<MqttBridgeOptions> options,
        ILogger<MqttTrendBridge> logger)
    {
        _trendProvider = trendProvider;
        _trendWsManager = trendWsManager;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = _options.Value;

        // Create MQTT client
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(config.BrokerHost, config.BrokerPort)
            .WithClientId($"helix-trend-bridge-{Guid.NewGuid():N}")
            .WithCleanSession()
            .Build();

        _logger.LogInformation("MqttTrendBridge: Connecting to MQTT broker at {Host}:{Port}",
            config.BrokerHost, config.BrokerPort);

        try
        {
            await _mqttClient.ConnectAsync(clientOptions, stoppingToken);
            _logger.LogInformation("MqttTrendBridge: Connected to MQTT broker.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MqttTrendBridge: Failed to connect to MQTT broker. Trends will not be published.");
            return;
        }

        // Subscribe to all trends
        var trends = _trendProvider.GetAllTrends().ToList();
        _logger.LogInformation("MqttTrendBridge: Found {TrendCount} trends to bridge.", trends.Count);

        foreach (var trend in trends)
        {
            var subscription = trend.Values
                .Subscribe(
                    onNext: value => PublishTrendValue(trend, value, config, stoppingToken),
                    onError: ex => _logger.LogError(ex, "MqttTrendBridge: Error in trend {TrendId} observable.", trend.TrendId)
                );

            _subscriptions.Add(subscription);
        }

        _logger.LogInformation("MqttTrendBridge: Subscribed to {SubscriptionCount} trend observables.", _subscriptions.Count);

        // Keep alive until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void PublishTrendValue(ITrend trend, TrendValue value, MqttBridgeOptions config, CancellationToken ct)
    {
        if (_mqttClient == null || !_mqttClient.IsConnected)
        {
            _logger.LogWarning("MqttTrendBridge: Cannot publish — MQTT client is not connected.");
            return;
        }

        try
        {
            // Build topic: helix/{siteId}/{fumehoodId}/{experimentId}/trends/{kind}/{trendId}
            var topic = $"helix/{config.SiteId}/{config.FumehoodId}/{trend.ExperimentId ?? "default"}/trends/{trend.TrendKind.ToString().ToLowerInvariant()}/{trend.TrendId}";

            // Serialize value to JSON
            var payload = JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(payload))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            // Fire-and-forget publish (async void is acceptable here within a subscription callback)
            _ = _mqttClient.PublishAsync(message, ct);

            // Relay to connected WebSocket dashboard clients (E5 / D-02)
            if (value is ScalarValue<double> scalar)
            {
                var dto = new TrendValueWireDto(
                    Type:      "trend-value",
                    TrendName: trend.TrendName,
                    Value:     scalar.Value,
                    Unit:      trend.Units ?? "",
                    Timestamp: DateTimeOffset.UtcNow.ToString("O"));
                _ = _trendWsManager.BroadcastAsync(dto, ct);
            }

            _logger.LogTrace("MqttTrendBridge: Published {TrendId} to {Topic}", trend.TrendId, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MqttTrendBridge: Failed to publish trend {TrendId}.", trend.TrendId);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MqttTrendBridge: Stopping...");

        // Dispose all subscriptions
        foreach (var sub in _subscriptions)
        {
            sub.Dispose();
        }
        _subscriptions.Clear();

        // Disconnect MQTT client
        if (_mqttClient != null && _mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("MqttTrendBridge: Disconnected from MQTT broker.");
        }

        _mqttClient?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Configuration options for MqttTrendBridge.
/// </summary>
public sealed class MqttBridgeOptions
{
    public string BrokerHost { get; set; } = "localhost";
    public int BrokerPort { get; set; } = 1883;
    public string SiteId { get; set; } = "site-001";
    public string FumehoodId { get; set; } = "fumehood-001";
}
