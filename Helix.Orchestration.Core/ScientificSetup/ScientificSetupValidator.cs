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

    /// <summary>Validates procedure declarations without compiling for dispatch or contacting any device.</summary>
    public static void Validate(ProcedureDefinition procedure)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        if (procedure.ProcedureId == Guid.Empty || string.IsNullOrWhiteSpace(procedure.Name))
            throw new ArgumentException("Procedure identity and name are required.");

        // WslDocument validation is structural only; it does not invoke an Agent, driver, or admission path.
        _ = procedure.Document?.Compile() ?? throw new ArgumentException("A procedure document is required.");
        var nodeById = procedure.Document.Nodes.ToDictionary(node => node.NodeId, StringComparer.Ordinal);
        var declarationNames = new HashSet<string>(StringComparer.Ordinal);
        var targets = new HashSet<(string NodeId, string ParameterKey)>();

        foreach (var declaration in procedure.Parameters ?? [])
        {
            if (string.IsNullOrWhiteSpace(declaration.Name) || !declarationNames.Add(declaration.Name) ||
                string.IsNullOrWhiteSpace(declaration.Dimension) || declaration.Targets is null || declaration.Targets.Count == 0)
                throw new ArgumentException("Procedure declarations require unique names, dimensions, and targets.");
            if (ScientificUnits.DimensionFor(declaration.Unit) != declaration.Dimension ||
                declaration.LowerBound is { } lower && declaration.UpperBound is { } upper && lower > upper)
                throw new ArgumentException("Procedure declaration units or bounds are invalid.");

            foreach (var target in declaration.Targets)
            {
                if (!nodeById.TryGetValue(target.NodeId, out var node) || string.IsNullOrWhiteSpace(target.ParameterKey) ||
                    !node.Parameters.TryGetValue(target.ParameterKey, out var current) || current is not decimal ||
                    !targets.Add((target.NodeId, target.ParameterKey)))
                    throw new ArgumentException("Procedure targets must be unique existing decimal WSL parameters.");
            }
        }

        foreach (var collection in procedure.PlannedCollections ?? [])
            if (string.IsNullOrWhiteSpace(collection.Name) || !nodeById.ContainsKey(collection.NodeId))
                throw new ArgumentException("Planned collection descriptors require an existing WSL node and name.");
    }

    /// <summary>Validates exact plan values against the procedure declarations and pinned revision.</summary>
    public static void Validate(ExperimentPlanDefinition plan, ProcedureDefinition procedure)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Validate(procedure);
        if (plan.PlanId == Guid.Empty || plan.ProcedureRevisionId == Guid.Empty || string.IsNullOrWhiteSpace(plan.Name))
            throw new ArgumentException("Plan identity, name, and published procedure revision are required.");

        var declarations = procedure.Parameters.ToDictionary(value => value.Name, StringComparer.Ordinal);
        var supplied = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in plan.Parameters ?? [])
        {
            if (!declarations.TryGetValue(value.Name, out var declaration) || !supplied.Add(value.Name) ||
                ScientificUnits.DimensionFor(value.Unit) != declaration.Dimension || value.Unit != declaration.Unit ||
                declaration.LowerBound is { } lower && value.Value < lower || declaration.UpperBound is { } upper && value.Value > upper)
                throw new ArgumentException("Plan parameters must exactly match declared names, units, dimensions, and bounds.");
        }

        if (declarations.Values.Any(declaration => declaration.Required && !supplied.Contains(declaration.Name)))
            throw new ArgumentException("Plan omits a required procedure parameter.");
    }

    private static bool CanSend(string direction) => direction is "Out" or "Bidirectional";
    private static bool CanReceive(string direction) => direction is "In" or "Bidirectional";
}
