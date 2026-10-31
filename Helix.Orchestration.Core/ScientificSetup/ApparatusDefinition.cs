namespace Helix.Orchestration.Core.ScientificSetup;

/// <summary>Typed scientific setup content. It never confers device authority.</summary>
public sealed record ApparatusDefinition(Guid ApparatusId, string Name, IReadOnlyList<ApparatusComponent> Components)
{
    public void Validate()
    {
        if (ApparatusId == Guid.Empty || string.IsNullOrWhiteSpace(Name)) throw new ArgumentException("Apparatus identity and name are required.");
        foreach (var component in Components)
        {
            if (component.ComponentId == Guid.Empty || string.IsNullOrWhiteSpace(component.Type) || component.Roles.Count == 0) throw new ArgumentException("Components require identity, type, and role.");
            if (component.Type == "Vessel" && !component.Roles.Any(role => role is "Source" or "Collection")) throw new ArgumentException("A Vessel must be Source or Collection.");
        }
    }
}
public sealed record ApparatusComponent(Guid ComponentId, string Type, IReadOnlyList<string> Roles);
