namespace Helix.Orchestration.SiteServer.Agent;

public sealed class AgentProxyOptions
{
    public const string SectionName = "AgentProxy";

    public string Mode { get; set; } = "sim";

    public string GrpcAddress { get; set; } = "http://localhost:5080";
}
