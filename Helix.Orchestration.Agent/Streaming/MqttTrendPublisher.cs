using Helix.Orchestration.Agent.Configuration;
using Helix.Orchestration.Contracts.Adapters;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;

namespace Helix.Orchestration.Agent.Streaming;

/// <summary>
/// Publishes adapter trend values to the configured MQTT broker.
/// </summary>
public sealed class MqttTrendPublisher
{
    private readonly AgentConfiguration _config;
    private readonly IMqttClient _client;
    private readonly ILogger<MqttTrendPublisher> _logger;

    private static class LogEvents
    {
        public static readonly EventId BrokerConnecting = new(2700, nameof(BrokerConnecting));
        public static readonly EventId BrokerConnected = new(2701, nameof(BrokerConnected));
        public static readonly EventId TrendPublishing = new(2702, nameof(TrendPublishing));
    }

    /// <summary>
    /// Creates MQTT trend publisher instance.
    /// </summary>
    public MqttTrendPublisher(AgentConfiguration config, ILogger<MqttTrendPublisher> logger)
    {
        _config = config;
        _logger = logger;
        _client = new MqttClientFactory().CreateMqttClient();
    }

    /// <summary>
    /// Connects to the configured MQTT broker.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(LogEvents.BrokerConnecting, "Connecting to MQTT broker {BrokerHost}:{BrokerPort}", _config.Mqtt.BrokerHost, _config.Mqtt.BrokerPort);

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(_config.Mqtt.BrokerHost, _config.Mqtt.BrokerPort)
            .WithClientId(_config.Mqtt.ClientId);

        if (_config.Mqtt.UseTls)
            optionsBuilder.WithTlsOptions(_ => _.UseTls());

        var options = optionsBuilder.Build();

        await _client.ConnectAsync(options, cancellationToken);
        _logger.LogInformation(LogEvents.BrokerConnected, "Connected to MQTT broker {BrokerHost}:{BrokerPort}", _config.Mqtt.BrokerHost, _config.Mqtt.BrokerPort);
    }

    /// <summary>
    /// Publishes a trend value to experiment-scoped topic hierarchy.
    /// </summary>
    public async Task PublishAsync(string experimentId, TrendValue value, CancellationToken cancellationToken)
    {
        var topic = $"helix/{_config.SiteId}/{_config.FumehoodId}/{experimentId}/trends/{value.TrendId}";

        _logger.LogInformation(LogEvents.TrendPublishing, "Publishing trend {TrendId} for experiment {ExperimentId} to topic {Topic}", value.TrendId, experimentId, topic);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value))
            .Build();

        await _client.PublishAsync(message, cancellationToken);
    }
}
