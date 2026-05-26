using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Helix.Orchestration.Infrastructure.Storage;
using Helix.Orchestration.Recorder.Configuration;
using Helix.Orchestration.Recorder.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Helix.Orchestration.Recorder.Services;

/// <summary>
/// Reads RecordedEvents from an in-process channel, batches them by (experimentId, trendId),
/// and flushes NDJSON objects to IObjectStorageSink when the count threshold or time interval is reached.
/// </summary>
public sealed class RecorderWriterService : BackgroundService
{
    private readonly ILogger<RecorderWriterService> _logger;
    private readonly RecorderOptions _options;
    private readonly Channel<RecordedEvent> _channel;
    private readonly IObjectStorageSink _storageSink;

    private readonly Dictionary<(string ExperimentId, string TrendId), List<RecordedEvent>> _buffer = new();
    private int _totalBuffered;

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
        using var flushTimer = new PeriodicTimer(TimeSpan.FromSeconds(_options.FlushIntervalSeconds));
        var timerTask = WaitForTimerAsync(flushTimer, stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Try to drain available events without blocking
                while (_channel.Reader.TryRead(out var evt))
                {
                    var key = (evt.ExperimentId, evt.TrendId);
                    if (!_buffer.TryGetValue(key, out var list))
                    {
                        list = new List<RecordedEvent>();
                        _buffer[key] = list;
                    }
                    list.Add(evt);
                    _totalBuffered++;
                }

                if (_totalBuffered >= _options.FlushAfterRecordCount)
                {
                    await FlushAsync(stoppingToken);
                    continue;
                }

                // Wait for next event or timer tick
                var readTask = _channel.Reader.WaitToReadAsync(stoppingToken).AsTask();
                var completedTask = await Task.WhenAny(readTask, timerTask);

                if (completedTask == timerTask)
                {
                    if (_totalBuffered > 0)
                        await FlushAsync(stoppingToken);

                    // Restart timer task
                    timerTask = WaitForTimerAsync(flushTimer, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown — flush remaining
        }
        finally
        {
            if (_totalBuffered > 0)
            {
                try { await FlushAsync(CancellationToken.None); }
                catch (Exception ex) { _logger.LogError(ex, "Final flush failed during shutdown"); }
            }
        }
    }

    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        if (_buffer.Count == 0) return;

        var snapshot = new Dictionary<(string, string), List<RecordedEvent>>(_buffer);
        _buffer.Clear();
        _totalBuffered = 0;

        foreach (var ((experimentId, trendId), events) in snapshot)
        {
            var timestamp = events[0].ReceivedAt.UtcDateTime.ToString("yyyyMMddHHmmss");
            var objectKey = $"{experimentId}/{trendId}/{timestamp}.ndjson";

            try
            {
                using var ms = new MemoryStream();
                await using var writer = new StreamWriter(ms, Encoding.UTF8, leaveOpen: true);

                foreach (var evt in events)
                {
                    await writer.WriteLineAsync(JsonSerializer.Serialize(evt));
                }

                await writer.FlushAsync(cancellationToken);
                ms.Position = 0;

                await _storageSink.WriteAsync(objectKey, ms, "application/x-ndjson", cancellationToken);
                _logger.LogInformation("Flushed {Count} records to {Key}", events.Count, objectKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush {Count} records for {Key}", events.Count, objectKey);
            }
        }
    }

    private static async Task WaitForTimerAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try { await timer.WaitForNextTickAsync(cancellationToken); }
        catch (OperationCanceledException) { /* expected on shutdown */ }
    }
}
