using Helix.Orchestration.Core.Trends;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

namespace Helix.Orchestration.Agent.Trends;

/// <summary>
/// Background service that subscribes to MQTT topics and routes trend data to the <see cref="InMemoryTrendProvider"/>.
/// </summary>
public sealed class MqttTrendIngestionService : BackgroundService
{
    private readonly ILogger<MqttTrendIngestionService> _logger;
    private readonly InMemoryTrendProvider _trendProvider;
    private readonly IConfiguration _configuration;
    private readonly MqttFactory _mqttFactory;
    private IMqttClient? _mqttClient;

    private string BrokerHost => _configuration["Mqtt:BrokerHost"] ?? "localhost";
    private int BrokerPort => int.TryParse(_configuration["Mqtt:BrokerPort"], out var port) ? port : 1883;
    private string[] TopicFilters => _configuration.GetSection("Mqtt:TopicFilters").Get<string[]>() ?? ["helix/trends/#"];

    public MqttTrendIngestionService(
        ILogger<MqttTrendIngestionService> logger,
        InMemoryTrendProvider trendProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _trendProvider = trendProvider;
        _configuration = configuration;
        _mqttFactory = new MqttFactory();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MQTT Trend Ingestion Service starting...");

        _mqttClient = _mqttFactory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(BrokerHost, BrokerPort)
            .WithClientId($"helix-agent-{Guid.NewGuid():N}")
            .WithCleanSession()
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_mqttClient.IsConnected)
                {
                    _logger.LogInformation("Connecting to MQTT broker at {Host}:{Port}...", BrokerHost, BrokerPort);
                    await _mqttClient.ConnectAsync(options, stoppingToken);

                    _logger.LogInformation("Connected to MQTT broker. Subscribing to topics: {Topics}", string.Join(", ", TopicFilters));

                    foreach (var topicFilter in TopicFilters)
                    {
                        await _mqttClient.SubscribeAsync(
                            new MqttTopicFilterBuilder().WithTopic(topicFilter).Build(),
                            stoppingToken);
                    }

                    _logger.LogInformation("MQTT subscriptions active.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT connection error. Retrying in 10 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("MQTT Trend Ingestion Service stopping...");

        if (_mqttClient?.IsConnected == true)
        {
            await _mqttClient.DisconnectAsync();
        }

        _mqttClient?.Dispose();
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var topic = args.ApplicationMessage.Topic;
            var payloadString = args.ApplicationMessage.ConvertPayloadToString();

            _logger.LogDebug("Received MQTT message on topic '{Topic}': {Payload}", topic, payloadString);

            // Parse topic structure: helix/trends/{trendKind}/{trendName}
            var parts = topic.Split('/');
            if (parts.Length < 4 || !parts[0].Equals("helix", StringComparison.OrdinalIgnoreCase) || !parts[1].Equals("trends", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Ignoring message with unrecognized topic structure: {Topic}", topic);
                return Task.CompletedTask;
            }

            var trendKind = parts[2];
            var trendName = parts[3];

            RouteMessage(trendKind, trendName, payloadString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT message on topic '{Topic}'", args.ApplicationMessage.Topic);
        }

        return Task.CompletedTask;
    }

    private void RouteMessage(string trendKind, string trendName, string payloadJson)
    {
        try
        {
            switch (trendKind.ToLowerInvariant())
            {
                case "spectral":
                    RouteSpectralMessage(trendName, payloadJson);
                    break;

                case "diodearray":
                    RouteDiodeArrayMessage(trendName, payloadJson);
                    break;

                default:
                    _logger.LogWarning("Unknown trend kind '{TrendKind}' for trend '{TrendName}'", trendKind, trendName);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing message for trend '{TrendName}' (kind: {TrendKind})", trendName, trendKind);
        }
    }

    private void RouteSpectralMessage(string trendName, string payloadJson)
    {
        using var jsonDoc = JsonDocument.Parse(payloadJson);
        var root = jsonDoc.RootElement;

        // Detect whether this is a scan or a matrix based on the JSON structure
        if (root.TryGetProperty("wavelengths", out var wavelengthsProp) && root.TryGetProperty("intensities", out var intensitiesProp))
        {
            // This is a SpectralScan
            var wavelengths = JsonSerializer.Deserialize<double[]>(wavelengthsProp.GetRawText()) ?? [];
            var intensities = JsonSerializer.Deserialize<double[]>(intensitiesProp.GetRawText()) ?? [];
            var timestamp = root.TryGetProperty("timestamp", out var tsProp) ? tsProp.GetDateTime() : DateTime.UtcNow;
            var reactionTime = root.TryGetProperty("reactionTime", out var rtProp) ? rtProp.GetDouble() : (double?)null;

            var scan = new Spectrum1D(
                TrendId: trendName,
                X: wavelengths,
                Y: intensities,
                SpectralType: SpectralType.UV,
                Metadata: new GenericSpectralMetadata(new Dictionary<string, object?>
                {
                    ["reactionTime"] = reactionTime
                }),
                SampleId: null,
                Timestamp: new DateTimeOffset(timestamp, TimeSpan.Zero));
            _trendProvider.Push1D(trendName, scan);

            _logger.LogDebug("Pushed SpectralScan to trend '{TrendName}' ({Count} points)", trendName, wavelengths.Length);
        }
        else if (root.TryGetProperty("matrix", out var matrixProp))
        {
            // This is a SpectralMatrix
            var matrix = JsonSerializer.Deserialize<double[,]>(matrixProp.GetRawText()) ?? new double[0, 0];
            var wavelengths = root.TryGetProperty("wavelengths", out var wProp)
                ? JsonSerializer.Deserialize<double[]>(wProp.GetRawText()) ?? []
                : [];
            var reactionTimes = root.TryGetProperty("reactionTimes", out var rtsProp)
                ? JsonSerializer.Deserialize<double[]>(rtsProp.GetRawText()) ?? []
                : [];
            var timestamp = root.TryGetProperty("timestamp", out var tsProp) ? tsProp.GetDateTime() : DateTime.UtcNow;

            var spectralMatrix = new Spectrum2D(
                TrendId: trendName,
                Axis1: wavelengths,
                Axis2: reactionTimes,
                Intensity: matrix,
                SpectralType: SpectralType.UV,
                Metadata: new GenericSpectralMetadata(new Dictionary<string, object?>()),
                Timestamp: new DateTimeOffset(timestamp, TimeSpan.Zero));
            _trendProvider.Push2D(trendName, spectralMatrix);

            _logger.LogDebug("Pushed SpectralMatrix to trend '{TrendName}' ({Rows}x{Cols})", trendName, matrix.GetLength(0), matrix.GetLength(1));
        }
        else
        {
            _logger.LogWarning("Unrecognized spectral payload format for trend '{TrendName}'", trendName);
        }
    }

    private void RouteDiodeArrayMessage(string trendName, string payloadJson)
    {
        var slice = JsonSerializer.Deserialize<DiodeArraySlice>(payloadJson);
        if (slice is null)
        {
            _logger.LogWarning("Failed to deserialize DiodeArraySlice for trend '{TrendName}'", trendName);
            return;
        }

        _trendProvider.PushDiodeArraySlice(trendName, slice);

        _logger.LogDebug("Pushed DiodeArraySlice to trend '{TrendName}' ({Count} wavelengths)", trendName, slice.Wavelengths.Length);
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        if (args.Exception != null)
        {
            _logger.LogWarning(args.Exception, "MQTT client disconnected unexpectedly. Reconnecting...");
        }
        else
        {
            _logger.LogInformation("MQTT client disconnected gracefully.");
        }

        return Task.CompletedTask;
    }
}
