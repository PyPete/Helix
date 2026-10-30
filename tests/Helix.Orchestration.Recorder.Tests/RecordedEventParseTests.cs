using Helix.Orchestration.Recorder.Models;

namespace Helix.Orchestration.Recorder.Tests;

public class RecordedEventParseTests
{
    [Fact]
    public void TryParseFromMqttMessage_ValidHelixTopic_ParsesCorrectly()
    {
        // Arrange
        var topic = "helix/site-01/fumehood-02/exp-123/trends/Scalar/trend-456";
        var payload = @"{""value"": 42.5, ""timestamp"": ""2025-01-15T10:30:00Z""}";

        // Act
        var success = RecordedEvent.TryParseFromMqttMessage(topic, payload, out var recordedEvent);

        // Assert
        Assert.True(success);
        Assert.NotNull(recordedEvent);
        Assert.Equal(topic, recordedEvent.Topic);
        Assert.Equal("exp-123", recordedEvent.ExperimentId);
        Assert.Equal("trend-456", recordedEvent.TrendId);
        Assert.Equal("Scalar", recordedEvent.TrendKind);
        Assert.Equal(payload, recordedEvent.PayloadJson);
    }

    [Theory]
    [InlineData("helix/site-01/fumehood-02")]  // Too short
    [InlineData("invalid/site-01/fumehood-02/exp-123/trends/Scalar/trend-456")]  // Wrong prefix
    [InlineData("helix/site-01/fumehood-02/exp-123/events/Scalar/trend-456")]  // Wrong segment (not "trends")
    [InlineData("helix/site-01/fumehood-02//trends/Scalar/trend-456")]  // Empty experimentId
    [InlineData("helix/site-01/fumehood-02/exp-123/trends//trend-456")]  // Empty trendKind
    [InlineData("helix/site-01/fumehood-02/exp-123/trends/Scalar/")]  // Empty trendId
    public void TryParseFromMqttMessage_InvalidTopic_ReturnsFalse(string topic)
    {
        // Arrange
        var payload = @"{""value"": 42.5}";

        // Act
        var success = RecordedEvent.TryParseFromMqttMessage(topic, payload, out var recordedEvent);

        // Assert
        Assert.False(success);
        Assert.Null(recordedEvent);
    }

    [Fact]
    public void TryParseFromMqttMessage_NullOrEmptyTopic_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(RecordedEvent.TryParseFromMqttMessage(null!, "payload", out _));
        Assert.False(RecordedEvent.TryParseFromMqttMessage("", "payload", out _));
        Assert.False(RecordedEvent.TryParseFromMqttMessage("   ", "payload", out _));
    }

    [Fact]
    public void TryParseFromMqttMessage_NullOrEmptyPayload_ReturnsFalse()
    {
        // Arrange
        var topic = "helix/site-01/fumehood-02/exp-123/trends/Scalar/trend-456";

        // Act & Assert
        Assert.False(RecordedEvent.TryParseFromMqttMessage(topic, null!, out _));
        Assert.False(RecordedEvent.TryParseFromMqttMessage(topic, "", out _));
        Assert.False(RecordedEvent.TryParseFromMqttMessage(topic, "   ", out _));
    }

    [Fact]
    public void TryParseFromMqttMessage_PayloadIsPreservedVerbatim()
    {
        // Arrange
        var topic = "helix/site-01/fumehood-02/exp-123/trends/Spectral/trend-789";
        var payload = @"  { ""data"": [ 1, 2, 3 ], ""note"": ""whitespace preserved"" }  ";

        // Act
        var success = RecordedEvent.TryParseFromMqttMessage(topic, payload, out var recordedEvent);

        // Assert
        Assert.True(success);
        Assert.NotNull(recordedEvent);
        Assert.Equal(payload, recordedEvent.PayloadJson);  // Exact match including whitespace
    }
}
