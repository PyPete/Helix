using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Helix.Orchestration.Infrastructure.Storage;
using Helix.Orchestration.Recorder.Configuration;
using Helix.Orchestration.Recorder.Models;
using Microsoft.Extensions.Options;

namespace Helix.Orchestration.Recorder.Services;

/// <summary>
/// Background service that reads events from the channel, batches them,
/// and flushes to object storage as JSONL files.
/// </summary>
public sealed class RecorderWriterService : BackgroundService
{
    private readonly ILogger<RecorderWriterService> _logger;
    private readonly RecorderOptions _options;
    private readonly Channel<RecordedEvent> _channel;
    private readonly IObjectStorageSink _storageSink;

    public RecorderWriterService(
        ILogger<RecorderWriterService> logger,
        IOptions<RecorderOptions> options,
        Channel<RecordedEvent> channel,
        IObjectStorageSink storageSink)
    {
        _logger = logger;
        _options = options.Value;
        _channel = channel;
        _storageSink = storageSink;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecorderWriterService starting...");

        var batch = new List<RecordedEvent>();
        var lastFlushTime = DateTimeOffset.UtcNow;

        using var flushTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Try to read from channel with a timeout to check flush conditions periodically
                var readTask = _channel.Reader.ReadAsync(stoppingToken).AsTask();
                var timerTask = flushTimer.WaitForNextTickAsync(stoppingToken).AsTask();

                var completedTask = await Task.WhenAny(readTask, timerTask);

                if (completedTask == readTask && readTask.IsCompletedSuccessfully)
                {
                    var recordedEvent = await readTask;
                    batch.Add(recordedEvent);

                    _logger.LogDebug("Batched event for {ExperimentId}/{TrendId}. Current batch size: {BatchSize}",
                        recordedEvent.ExperimentId, recordedEvent.TrendId, batch.Count);
                }

                // Check flush conditions: record count OR time interval
                var timeSinceLastFlush = DateTimeOffset.UtcNow - lastFlushTime;
                var shouldFlushByCount = batch.Count >= _options.FlushAfterRecordCount;
                var shouldFlushByTime = timeSinceLastFlush.TotalSeconds >= _options.FlushIntervalSeconds && batch.Count > 0;

                if (shouldFlushByCount || shouldFlushByTime)
                {
                    await FlushBatchAsync(batch, stoppingToken);
                    batch.Clear();
                    lastFlushTime = DateTimeOffset.UtcNow;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("RecorderWriterService stopping. Flushing remaining {Count} events...", batch.Count);
                if (batch.Count > 0)
                {
                    await FlushBatchAsync(batch, CancellationToken.None);
                }
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RecorderWriterService");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("RecorderWriterService stopped.");
    }

    private async Task FlushBatchAsync(List<RecordedEvent> batch, CancellationToken ct)
    {
        if (batch.Count == 0)
            return;

        // Group by (experimentId, trendId) so each group gets its own object
        var groups = batch
            .GroupBy(e => (e.ExperimentId, e.TrendId))
            .ToList();

        _logger.LogInformation("Flushing {BatchCount} events across {GroupCount} groups to storage...", 
            batch.Count, groups.Count);

        foreach (var group in groups)
        {
            try
            {
                var (experimentId, trendId) = group.Key;
                var records = group.ToList();

                // Serialize to JSONL (one JSON object per line)
                var jsonlBuilder = new StringBuilder();
                foreach (var record in records)
                {
                    var json = JsonSerializer.Serialize(record, new JsonSerializerOptions 
                    { 
                        WriteIndented = false 
                    });
                    jsonlBuilder.AppendLine(json);
                }

                var jsonlContent = jsonlBuilder.ToString();
                var jsonlBytes = Encoding.UTF8.GetBytes(jsonlContent);

                // Object key pattern: experiments/{experimentId}/trends/{date:yyyy-MM-dd}/{trendId}/{batchTimestampUtc:yyyyMMddTHHmmssfffZ}.jsonl
                var now = DateTimeOffset.UtcNow;
                var dateSegment = now.ToString("yyyy-MM-dd");
                var timestampSegment = now.ToString("yyyyMMddTHHmmssfffZ");
                var objectKey = $"experiments/{experimentId}/trends/{dateSegment}/{trendId}/{timestampSegment}.jsonl";

                // Write to object storage
                using var stream = new MemoryStream(jsonlBytes);
                await _storageSink.WriteAsync(objectKey, stream, "application/x-ndjson", ct);

                _logger.LogInformation("Flushed {RecordCount} records to {ObjectKey}", records.Count, objectKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush batch for experiment {ExperimentId}, trend {TrendId}",
                    group.Key.ExperimentId, group.Key.TrendId);
            }
        }
    }
}
