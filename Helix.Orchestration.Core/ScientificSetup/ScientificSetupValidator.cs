namespace Helix.Orchestration.Core.ScientificSetup;

/// <summary>Validates scientific topology content; it never authorizes device activity.</summary>
public static class ScientificSetupValidator
{
    private static readonly IReadOnlyDictionary<string, string[]> RequiredRoles = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["Pump"] = ["Transfer"],
        ["Valve"] = ["Control"],
        ["Sensor"] = ["Observe"],
        ["Collector"] = ["Collection"]
    };

    private static readonly HashSet<string> ComponentTypes = new(StringComparer.Ordinal) { "Vessel", "Pump", "Valve", "Sensor", "Collector", "DeviceModule" };
    private static readonly HashSet<string> Roles = new(StringComparer.Ordinal) { "Source", "Transfer", "Control", "Observe", "Collection" };
    private static readonly HashSet<string> Media = new(StringComparer.Ordinal) { "Fluid", "Gas", "Signal" };

    public static void Validate(ApparatusDefinition apparatus)
    {
        ArgumentNullException.ThrowIfNull(apparatus);
        if (apparatus.ApparatusId == Guid.Empty || string.IsNullOrWhiteSpace(apparatus.Name))
            throw new ArgumentException("Apparatus identity and name are required.");

        var componentIds = new HashSet<Guid>();
        var bindings = new HashSet<string>(StringComparer.Ordinal);
        var ports = new Dictionary<Guid, ApparatusPort>();

        foreach (var component in apparatus.Components)
        {
            if (component.ComponentId == Guid.Empty || !componentIds.Add(component.ComponentId))
                throw new ArgumentException("Components require unique identities.");
            if (!ComponentTypes.Contains(component.Type) || component.Roles.Count == 0 || component.Roles.Any(role => !Roles.Contains(role)))
                throw new ArgumentException($"Component {component.ComponentId} has an invalid type or role.");
            if (component.Type == "Vessel" && !component.Roles.Any(role => role is "Source" or "Collection"))
                throw new ArgumentException("A Vessel must be Source or Collection.");
            if (RequiredRoles.TryGetValue(component.Type, out var required) && !required.All(component.Roles.Contains))
                throw new ArgumentException($"{component.Type} requires the {string.Join(", ", required)} role.");
            if (!string.IsNullOrWhiteSpace(component.DiscoveredDeviceId) && !bindings.Add(component.DiscoveredDeviceId))
                throw new ArgumentException("A discovered device identity can only be bound once.");

            foreach (var port in component.Ports ?? [])
            {
                if (port.PortId == Guid.Empty || !ports.TryAdd(port.PortId, port) || !Media.Contains(port.Medium) || port.Direction is not ("In" or "Out" or "Bidirectional") || port.Cardinality is not ("One" or "Many"))
                    throw new ArgumentException("Ports require unique identities and approved media, direction, and cardinality.");
            }
        }

        var endpointUses = new Dictionary<Guid, int>();
        var connections = new HashSet<(Guid From, Guid To)>();
        foreach (var connection in apparatus.Connections ?? [])
        {
            if (connection.ConnectionId == Guid.Empty || connection.FromPortId == connection.ToPortId || !connections.Add((connection.FromPortId, connection.ToPortId)) ||
                !ports.TryGetValue(connection.FromPortId, out var from) || !ports.TryGetValue(connection.ToPortId, out var to) ||
                from.Medium != to.Medium || !CanSend(from.Direction) || !CanReceive(to.Direction))
                throw new ArgumentException("Connections require distinct compatible ports.");
            endpointUses[connection.FromPortId] = endpointUses.GetValueOrDefault(connection.FromPortId) + 1;
            endpointUses[connection.ToPortId] = endpointUses.GetValueOrDefault(connection.ToPortId) + 1;
        }

        foreach (var (portId, uses) in endpointUses)
            if (ports[portId].Cardinality == "One" && uses > 1)
                throw new ArgumentException("A cardinality One port cannot have multiple connections.");

        foreach (var assertion in apparatus.Assertions ?? [])
            ValidateAssertion(assertion, DateTimeOffset.UtcNow);
    }

    public static void ValidateAssertion(ScientificAssertion assertion, DateTimeOffset nowUtc)
    {
        if (assertion.AssertionId == Guid.Empty || assertion.Kind is not ("CapabilityConstraint" or "CalibrationRecord" or "InspectionRecord") ||
            string.IsNullOrWhiteSpace(assertion.SourceReference) || string.IsNullOrWhiteSpace(assertion.RecordedBy) || assertion.ObservedAtUtc > nowUtc ||
            assertion.ValidThroughUtc is { } validThrough && validThrough < assertion.ObservedAtUtc)
            throw new ArgumentException("Assertion identity, provenance, and observation dates are invalid.");
        if (assertion.LowerBound is { } lower && assertion.UpperBound is { } upper && lower > upper)
            throw new ArgumentException("Assertion lower bound cannot exceed upper bound.");
        if (assertion.MeasuredValue.HasValue != (assertion.Unit is not null))
            throw new ArgumentException("An assertion measured value and unit must be supplied together.");
        if (assertion.Unit is not null)
            _ = ScientificUnits.DimensionFor(assertion.Unit);
    }

    private static bool CanSend(string direction) => direction is "Out" or "Bidirectional";
    private static bool CanReceive(string direction) => direction is "In" or "Bidirectional";
}
