namespace Helix.Orchestration.Recorder.Models;

/// <summary>
/// An MQTT trend message received from a fumehood agent, ready to be written to object storage.
/// Topic format: helix/{siteId}/{fumehoodId}/{experimentId}/trends/{trendKind}/{trendId}
/// </summary>
public sealed record RecordedEvent(
    string Topic,
    string ExperimentId,
    string TrendId,
    string TrendKind,
    DateTimeOffset ReceivedAt,
    string PayloadJson)
{
    /// <summary>
    /// Parses a RecordedEvent from an MQTT topic and payload.
    /// Returns false if the topic does not match the expected helix trend format.
    /// </summary>
    public static bool TryParseFromMqttMessage(string topic, string payload, out RecordedEvent? recordedEvent)
    {
        recordedEvent = null;

        if (string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(payload))
            return false;

        // helix/{siteId}/{fumehoodId}/{experimentId}/trends/{trendKind}/{trendId}
        var parts = topic.Split('/');
        if (parts.Length != 7)
            return false;

        if (parts[0] != "helix")
            return false;

        if (parts[4] != "trends")
            return false;

        var experimentId = parts[3];
        var trendKind = parts[5];
        var trendId = parts[6];

        if (string.IsNullOrEmpty(experimentId) || string.IsNullOrEmpty(trendKind) || string.IsNullOrEmpty(trendId))
            return false;

        recordedEvent = new RecordedEvent(
            Topic: topic,
            ExperimentId: experimentId,
            TrendId: trendId,
            TrendKind: trendKind,
            ReceivedAt: DateTimeOffset.UtcNow,
            PayloadJson: payload);

        return true;
    }
}
