namespace Helix.Orchestration.Recorder.Models;

/// <summary>
/// Represents a recorded event from the MQTT broker.
/// Topic scheme: helix/{siteId}/{fumehoodId}/{experimentId}/trends/{kind}/{trendId}
/// </summary>
/// <param name="Topic">The full MQTT topic the message was received on.</param>
/// <param name="ExperimentId">The experiment ID parsed from the topic.</param>
/// <param name="TrendId">The trend ID parsed from the topic.</param>
/// <param name="TrendKind">The trend kind parsed from the topic (e.g., "Scalar", "Spectral").</param>
/// <param name="ReceivedAt">The timestamp when this event was received.</param>
/// <param name="PayloadJson">The raw JSON payload from the MQTT message.</param>
public sealed record RecordedEvent(
    string Topic,
    string ExperimentId,
    string TrendId,
    string TrendKind,
    DateTimeOffset ReceivedAt,
    string PayloadJson)
{
    /// <summary>
    /// Attempts to parse a RecordedEvent from an MQTT message.
    /// Expected topic format: helix/{siteId}/{fumehoodId}/{experimentId}/trends/{kind}/{trendId}
    /// </summary>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="payloadJson">The JSON payload.</param>
    /// <param name="recordedEvent">The parsed event, if successful.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParseFromMqttMessage(string topic, string payloadJson, out RecordedEvent? recordedEvent)
    {
        recordedEvent = null;

        if (string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(payloadJson))
            return false;

        // Expected: helix/{siteId}/{fumehoodId}/{experimentId}/trends/{kind}/{trendId}
        var segments = topic.Split('/');

        if (segments.Length < 7)
            return false;

        if (segments[0] != "helix" || segments[4] != "trends")
            return false;

        var experimentId = segments[3];
        var trendKind = segments[5];
        var trendId = segments[6];

        if (string.IsNullOrWhiteSpace(experimentId) || 
            string.IsNullOrWhiteSpace(trendKind) || 
            string.IsNullOrWhiteSpace(trendId))
            return false;

        recordedEvent = new RecordedEvent(
            Topic: topic,
            ExperimentId: experimentId,
            TrendId: trendId,
            TrendKind: trendKind,
            ReceivedAt: DateTimeOffset.UtcNow,
            PayloadJson: payloadJson);

        return true;
    }
}
