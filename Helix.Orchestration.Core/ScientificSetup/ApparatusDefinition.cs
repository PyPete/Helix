namespace Helix.Orchestration.Core.ScientificSetup;

/// <summary>Typed scientific setup content. It never confers device authority.</summary>
public sealed record ApparatusDefinition(
    Guid ApparatusId,
    string Name,
    IReadOnlyList<ApparatusComponent> Components,
    IReadOnlyList<ApparatusConnection>? Connections = null,
    IReadOnlyList<ScientificAssertion>? Assertions = null,
    Guid? SourceRevisionId = null)
{
    public void Validate()
    {
        ScientificSetupValidator.Validate(this);
    }
}
public sealed record ApparatusComponent(Guid ComponentId, string Type, IReadOnlyList<string> Roles, IReadOnlyList<ApparatusPort>? Ports = null, string? DiscoveredDeviceId = null)
;
public sealed record ApparatusPort(Guid PortId, string Medium, string Direction, string Cardinality);
public sealed record ApparatusConnection(Guid ConnectionId, Guid FromPortId, Guid ToPortId);
