namespace Helix.Orchestration.Recorder.Configuration;

/// <summary>
/// Configuration options for the Recorder service.
/// </summary>
public sealed class RecorderOptions
{
    /// <summary>
    /// MQTT broker hostname or IP address.
    /// </summary>
    public string BrokerHost { get; set; } = "localhost";

    /// <summary>
    /// MQTT broker port.
    /// </summary>
    public int BrokerPort { get; set; } = 1883;

    /// <summary>
    /// MQTT topic filters to subscribe to (e.g., "helix/#" or "helix/site-01/#").
    /// </summary>
    public string[] TopicFilters { get; set; } = ["helix/#"];

    /// <summary>
    /// Maximum capacity of the internal event channel (bounded).
    /// </summary>
    public int ChannelCapacity { get; set; } = 4096;

    /// <summary>
    /// Flush batch to storage after this many records are accumulated.
    /// </summary>
    public int FlushAfterRecordCount { get; set; } = 100;

    /// <summary>
    /// Flush batch to storage after this many seconds have elapsed since the last flush.
    /// </summary>
    public int FlushIntervalSeconds { get; set; } = 30;
}
