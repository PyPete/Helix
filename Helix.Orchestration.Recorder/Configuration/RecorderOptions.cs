namespace Helix.Orchestration.Recorder.Configuration;

public class RecorderOptions
{
    public const string SectionName = "Recorder";

    /// <summary>MQTT broker hostname or IP.</summary>
    public string MqttBrokerHost { get; set; } = "localhost";

    /// <summary>MQTT broker port.</summary>
    public int MqttBrokerPort { get; set; } = 1883;

    /// <summary>MQTT topic filter to subscribe to (supports wildcards).</summary>
    public string TopicFilter { get; set; } = "helix/#";

    /// <summary>Number of records to accumulate before flushing to storage.</summary>
    public int FlushAfterRecordCount { get; set; } = 1000;

    /// <summary>Maximum seconds between flushes regardless of record count.</summary>
    public int FlushIntervalSeconds { get; set; } = 60;
}
