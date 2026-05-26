using System.Text;
using System.Threading.Channels;
using Helix.Orchestration.Recorder.Configuration;
using Helix.Orchestration.Recorder.Models;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;

namespace Helix.Orchestration.Recorder.Services;

/// <summary>
/// Background service that connects to the MQTT broker, subscribes to configured topics,
/// and writes parsed events to a channel for downstream processing.
/// </summary>
public sealed class MqttRecorderService : BackgroundService
{
    private readonly ILogger<MqttRecorderService> _logger;
    private readonly RecorderOptions _options;
    private readonly Channel<RecordedEvent> _channel;
    private readonly IMqttClient _mqttClient;

    public MqttRecorderService(
        ILogger<MqttRecorderService> logger,
        IOptions<RecorderOptions> options,
        Channel<RecordedEvent> channel)
    {
        _logger = logger;
        _options = options.Value;
        _channel = channel;
        _mqttClient = new MqttClientFactory().CreateMqttClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MqttRecorderService starting...");

        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

        var retryDelaySeconds = 1;
        const int maxRetryDelaySeconds = 60;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndSubscribeAsync(stoppingToken);

                _logger.LogInformation("MqttRecorderService connected and subscribed. Waiting for messages...");

                // Keep running until disconnected or cancellation requested
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("MqttRecorderService stopping due to cancellation.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MqttRecorderService encountered an error. Retrying in {Delay}s...", retryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), stoppingToken);

                // Exponential back-off with cap
                retryDelaySeconds = Math.Min(retryDelaySeconds * 2, maxRetryDelaySeconds);
            }
        }

        await DisconnectAsync();
        _logger.LogInformation("MqttRecorderService stopped.");
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken ct)
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.BrokerHost, _options.BrokerPort)
            .WithCleanSession()
            .Build();

        await _mqttClient.ConnectAsync(options, ct);
        _logger.LogInformation("Connected to MQTT broker at {Host}:{Port}", _options.BrokerHost, _options.BrokerPort);

        foreach (var topicFilter in _options.TopicFilters)
        {
            await _mqttClient.SubscribeAsync(topicFilter, cancellationToken: ct);
            _logger.LogInformation("Subscribed to topic filter: {TopicFilter}", topicFilter);
        }
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var topic = args.ApplicationMessage.Topic;
            var payloadJson = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

            if (RecordedEvent.TryParseFromMqttMessage(topic, payloadJson, out var recordedEvent) && recordedEvent != null)
            {
                // Try to write to channel (non-blocking)
                if (_channel.Writer.TryWrite(recordedEvent))
                {
                    _logger.LogDebug("Enqueued event for experiment {ExperimentId}, trend {TrendId}", 
                        recordedEvent.ExperimentId, recordedEvent.TrendId);
                }
                else
                {
                    _logger.LogWarning("Channel is full. Dropped event for experiment {ExperimentId}, trend {TrendId}",
                        recordedEvent.ExperimentId, recordedEvent.TrendId);
                }
            }
            else
            {
                _logger.LogWarning("Failed to parse MQTT message from topic: {Topic}", topic);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT message");
        }

        return Task.CompletedTask;
    }

    private async Task DisconnectAsync()
    {
        try
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
                _logger.LogInformation("Disconnected from MQTT broker.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from MQTT broker");
        }
    }

    public override void Dispose()
    {
        _mqttClient?.Dispose();
        base.Dispose();
    }
}
