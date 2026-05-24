using Helix.Orchestration.Agent.Adapters;
using Helix.Orchestration.Agent.Telemetry;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Streaming;

/// <summary>
/// Aggregates trend streams from registered adapters and forwards them to local buffering and MQTT publishing.
/// </summary>
public sealed class TrendAggregator(
    AdapterRegistry adapterRegistry,
    MqttTrendPublisher publisher,
    TrendBuffer trendBuffer,
    ILogger<TrendAggregator> logger)
{
    private static class LogEvents
    {
        public static readonly EventId StreamStarted = new(2600, nameof(StreamStarted));
        public static readonly EventId StreamStopped = new(2601, nameof(StreamStopped));
    }

    /// <summary>
    /// Starts trend stream fan-in from all currently discovered adapters.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var (adapterId, proxy) in adapterRegistry.GetAll())
        {
            _ = Task.Run(async () =>
            {
                logger.LogInformation(LogEvents.StreamStarted, "Trend stream started for adapter {AdapterId}", adapterId);

                try
                {
                    await foreach (var trend in proxy.StreamTrendsAsync(cancellationToken))
                    {
                        trendBuffer.Enqueue(trend);
                        await publisher.PublishAsync(adapterId, trend, cancellationToken);

                        HelixTelemetry.TrendsPublished.Add(1,
                            KeyValuePair.Create<string, object?>("adapter.id", adapterId),
                            KeyValuePair.Create<string, object?>("trend.id", trend.TrendId));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(LogEvents.StreamStopped, ex, "Trend stream ended for adapter {AdapterId}", adapterId);
                }
            }, cancellationToken);
        }

        await Task.CompletedTask;
    }
}
