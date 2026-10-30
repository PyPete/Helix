using System.Text;
using System.Threading.Channels;
using Helix.Orchestration.Infrastructure.Storage;
using Helix.Orchestration.Recorder.Configuration;
using Helix.Orchestration.Recorder.Models;
using Helix.Orchestration.Recorder.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Helix.Orchestration.Recorder.Tests;

public class RecorderWriterServiceTests
{
    private readonly Mock<ILogger<RecorderWriterService>> _mockLogger;
    private readonly Mock<IObjectStorageSink> _mockStorageSink;

    public RecorderWriterServiceTests()
    {
        _mockLogger = new Mock<ILogger<RecorderWriterService>>();
        _mockStorageSink = new Mock<IObjectStorageSink>();
    }

    [Fact]
    public async Task ExecuteAsync_FlushesAfterRecordCountThreshold()
    {
        // Arrange
        var options = new RecorderOptions
        {
            FlushAfterRecordCount = 3,
            FlushIntervalSeconds = 3600  // Long interval to ensure count triggers first
        };

        var channel = Channel.CreateUnbounded<RecordedEvent>();
        var service = new RecorderWriterService(
            _mockLogger.Object,
            Options.Create(options),
            channel,
            _mockStorageSink.Object);

        // Write 3 events (should trigger flush)
        await channel.Writer.WriteAsync(CreateTestEvent("exp-1", "trend-1"));
        await channel.Writer.WriteAsync(CreateTestEvent("exp-1", "trend-1"));
        await channel.Writer.WriteAsync(CreateTestEvent("exp-1", "trend-1"));

        var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(2000);  // Wait for flush to occur
        cts.Cancel();
        await executeTask;

        // Assert
        _mockStorageSink.Verify(
            s => s.WriteAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                "application/x-ndjson",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_FlushesAfterTimeInterval()
    {
        // Arrange
        var options = new RecorderOptions
        {
            FlushAfterRecordCount = 1000,  // High count to ensure time triggers first
            FlushIntervalSeconds = 2
        };

        var channel = Channel.CreateUnbounded<RecordedEvent>();
        var service = new RecorderWriterService(
            _mockLogger.Object,
            Options.Create(options),
            channel,
            _mockStorageSink.Object);

        // Write 1 event (below threshold)
        await channel.Writer.WriteAsync(CreateTestEvent("exp-1", "trend-1"));

        var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(4000);  // Wait for time-based flush
        cts.Cancel();
        await executeTask;

        // Assert
        _mockStorageSink.Verify(
            s => s.WriteAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                "application/x-ndjson",
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ProducesSeparateObjectsForDistinctExperimentTrendPairs()
    {
        // Arrange
        var options = new RecorderOptions
        {
            FlushAfterRecordCount = 4,
            FlushIntervalSeconds = 3600
        };

        var channel = Channel.CreateUnbounded<RecordedEvent>();
        var service = new RecorderWriterService(
            _mockLogger.Object,
            Options.Create(options),
            channel,
            _mockStorageSink.Object);

        // Write events for two distinct (experimentId, trendId) pairs
        await channel.Writer.WriteAsync(CreateTestEvent("exp-1", "trend-1"));
        await channel.Writer.WriteAsync(CreateTestEvent("exp-1", "trend-1"));
        await channel.Writer.WriteAsync(CreateTestEvent("exp-2", "trend-2"));
        await channel.Writer.WriteAsync(CreateTestEvent("exp-2", "trend-2"));

        var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(2000);
        cts.Cancel();
        await executeTask;

        // Assert - Should write twice (once per group)
        _mockStorageSink.Verify(
            s => s.WriteAsync(
                It.Is<string>(key => key.Contains("exp-1") && key.Contains("trend-1")),
                It.IsAny<Stream>(),
                "application/x-ndjson",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockStorageSink.Verify(
            s => s.WriteAsync(
                It.Is<string>(key => key.Contains("exp-2") && key.Contains("trend-2")),
                It.IsAny<Stream>(),
                "application/x-ndjson",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyChannel_DoesNotWrite()
    {
        // Arrange
        var options = new RecorderOptions
        {
            FlushAfterRecordCount = 10,
            FlushIntervalSeconds = 1
        };

        var channel = Channel.CreateUnbounded<RecordedEvent>();
        var service = new RecorderWriterService(
            _mockLogger.Object,
            Options.Create(options),
            channel,
            _mockStorageSink.Object);

        var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(2000);  // Wait beyond flush interval
        cts.Cancel();
        await executeTask;

        // Assert
        _mockStorageSink.Verify(
            s => s.WriteAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ProducesValidJsonl_WithCorrectLineCount()
    {
        // Arrange
        var options = new RecorderOptions
        {
            FlushAfterRecordCount = 3,
            FlushIntervalSeconds = 3600
        };

        var channel = Channel.CreateUnbounded<RecordedEvent>();

        string? capturedJsonl = null;
        _mockStorageSink
            .Setup(s => s.WriteAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                "application/x-ndjson",
                It.IsAny<CancellationToken>()))
            .Callback<string, Stream, string, CancellationToken>((key, stream, contentType, ct) =>
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                capturedJsonl = reader.ReadToEnd();
            })
            .Returns(Task.CompletedTask);

        var service = new RecorderWriterService(
            _mockLogger.Object,
            Options.Create(options),
            channel,
            _mockStorageSink.Object);

        // Write 3 events
        await channel.Writer.WriteAsync(CreateTestEvent("exp-1", "trend-1"));
        await channel.Writer.WriteAsync(CreateTestEvent("exp-1", "trend-1"));
        await channel.Writer.WriteAsync(CreateTestEvent("exp-1", "trend-1"));

        var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(2000);
        cts.Cancel();
        await executeTask;

        // Assert
        Assert.NotNull(capturedJsonl);
        var lines = capturedJsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);  // 3 JSONL records
    }

    private static RecordedEvent CreateTestEvent(string experimentId, string trendId)
    {
        return new RecordedEvent(
            Topic: $"helix/site-01/fumehood-01/{experimentId}/trends/Scalar/{trendId}",
            ExperimentId: experimentId,
            TrendId: trendId,
            TrendKind: "Scalar",
            ReceivedAt: DateTimeOffset.UtcNow,
            PayloadJson: @"{""value"": 42.0}");
    }
}
