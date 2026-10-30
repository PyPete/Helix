using System.Threading.Channels;
using Helix.Orchestration.Recorder.Configuration;
using Helix.Orchestration.Recorder.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;

namespace Helix.Orchestration.Recorder.Services;

/// <summary>
/// Subscribes to the MQTT broker and enqueues RecordedEvents onto the in-process channel
/// for RecorderWriterService to batch and persist.
/// </summary>
public sealed class MqttRecorderService : BackgroundService
{
    private readonly ILogger<MqttRecorderService> _logger;
    private readonly RecorderOptions _options;
    private readonly Channel<RecordedEvent> _channel;

    public MqttRecorderService(
        ILogger<MqttRecorderService> logger,
        IOptions<RecorderOptions> options,
        Channel<RecordedEvent> channel)
    {
        _logger = logger;
        _options = options.Value;
        _channel = channel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();

        client.ApplicationMessageReceivedAsync += args =>
        {
            var topic = args.ApplicationMessage.Topic;
            var payload = args.ApplicationMessage.ConvertPayloadToString();

            if (RecordedEvent.TryParseFromMqttMessage(topic, payload, out var evt) && evt is not null)
            {
                if (!_channel.Writer.TryWrite(evt))
                    _logger.LogWarning("Channel full — dropping event for topic {Topic}", topic);
            }

            return Task.CompletedTask;
        };

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.MqttBrokerHost, _options.MqttBrokerPort)
            .WithClientId($"helix-recorder-{Environment.MachineName}")
            .WithCleanSession(true)
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await client.ConnectAsync(mqttOptions, stoppingToken);
                _logger.LogInformation("Connected to MQTT broker {Host}:{Port}", _options.MqttBrokerHost, _options.MqttBrokerPort);

                await client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(_options.TopicFilter)
                    .Build(), stoppingToken);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT connection lost — reconnecting in 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(cancellationToken: CancellationToken.None);
            }
        }
    }
}
