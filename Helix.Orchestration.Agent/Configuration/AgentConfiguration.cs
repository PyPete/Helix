namespace Helix.Orchestration.Agent.Configuration;

public sealed class AgentConfiguration
{
    public string AgentId { get; set; } = "agent-fh-01";
    public string SiteId { get; set; } = "lab-cambridge";
    public string FumehoodId { get; set; } = "fh-01";
    public string SiteServerEndpoint { get; set; } = string.Empty;
    public MqttConfiguration Mqtt { get; set; } = new();
    public CleipnirConfiguration Cleipnir { get; set; } = new();
    public List<AdapterConfiguration> Adapters { get; set; } = [];
    public DeviceRegistrySyncConfiguration DeviceRegistry { get; set; } = new();
}

public sealed class MqttConfiguration
{
    public string BrokerHost { get; set; } = string.Empty;
    public int BrokerPort { get; set; } = 8883;
    public bool UseTls { get; set; } = true;
    public string ClientId { get; set; } = "agent-fh-01";
}

public sealed class CleipnirConfiguration
{
    public string ConnectionString { get; set; } = "Data Source=agent.db";
    public string StoreType { get; set; } = "SQLite";
}

public sealed class AdapterConfiguration
{
    public string AdapterId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public List<string> DeviceIds { get; set; } = [];
    public int HealthCheckIntervalSeconds { get; set; } = 30;
}

public sealed class DeviceRegistrySyncConfiguration
{
    public bool SyncFromSiteServer { get; set; } = true;
    public string LocalFallbackPath { get; set; } = "device-registry.json";
}
