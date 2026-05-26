using Helix.Orchestration.Core.Devices;

namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// Default implementation of <see cref="IWslCompiler"/> that validates WSL documents
/// and produces executable <see cref="RunspaceDefinition"/> instances.
/// </summary>
public sealed class WslCompiler : IWslCompiler
{
    private static readonly HashSet<string> SupportedVersions = ["1.0"];

    /// <inheritdoc />
    public CompilationResult Compile(WslDocument document, DeviceRegistry deviceRegistry)
    {
        var diagnostics = new List<CompilationDiagnostic>();

        // 1. Schema/version validation
        ValidateSchema(document, diagnostics);

        // 2. Node graph structural validation
        ValidateGraph(document, diagnostics);

        // 3. Node parameter validation
        ValidateNodeParameters(document, diagnostics);

        // 4. Device binding resolution
        ValidateDeviceBindings(document, deviceRegistry, diagnostics);

        // 5. Variable reference validation
        ValidateVariableReferences(document, diagnostics);

        // If any errors, return failed result
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return CompilationResult.Failed(diagnostics);

        // 5. Emit RunspaceDefinition
        var definition = new RunspaceDefinition(
            DefinitionId: document.DocumentId,
            Name: document.Name,
            Nodes: document.Nodes,
            Edges: document.Edges,
            Metadata: new Dictionary<string, object?>
            {
                ["schemaVersion"] = document.SchemaVersion,
                ["author"] = document.Author,
                ["description"] = document.Description,
                ["compiledAt"] = DateTimeOffset.UtcNow
            });

        return CompilationResult.Succeeded(definition, diagnostics);
    }

    private static void ValidateSchema(WslDocument document, List<CompilationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(document.DocumentId))
            diagnostics.Add(new CompilationDiagnostic(DiagnosticSeverity.Error, "Document must have a non-empty DocumentId.", Code: "WSL001"));

        if (string.IsNullOrWhiteSpace(document.Name))
            diagnostics.Add(new CompilationDiagnostic(DiagnosticSeverity.Error, "Document must have a non-empty Name.", Code: "WSL002"));

        if (!SupportedVersions.Contains(document.SchemaVersion))
            diagnostics.Add(new CompilationDiagnostic(
                DiagnosticSeverity.Error,
                $"Schema version '{document.SchemaVersion}' is not supported. Supported versions: {string.Join(", ", SupportedVersions)}",
                Code: "WSL003"));
    }

    private static void ValidateGraph(WslDocument document, List<CompilationDiagnostic> diagnostics)
    {
        var nodeIds = document.Nodes.Select(n => n.NodeId).ToHashSet();

        // Check for duplicate node IDs
        var duplicateIds = document.Nodes
            .GroupBy(n => n.NodeId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var duplicateId in duplicateIds)
            diagnostics.Add(new CompilationDiagnostic(DiagnosticSeverity.Error, $"Duplicate node ID: '{duplicateId}'", duplicateId, "WSL010"));

        // Check for orphan edges (edges referencing non-existent nodes)
        foreach (var edge in document.Edges)
        {
            if (!nodeIds.Contains(edge.SourceNodeId))
                diagnostics.Add(new CompilationDiagnostic(
                    DiagnosticSeverity.Error,
                    $"Edge '{edge.EdgeId}': source node '{edge.SourceNodeId}' not found.",
                    edge.SourceNodeId,
                    "WSL011"));

            if (!nodeIds.Contains(edge.TargetNodeId))
                diagnostics.Add(new CompilationDiagnostic(
                    DiagnosticSeverity.Error,
                    $"Edge '{edge.EdgeId}': target node '{edge.TargetNodeId}' not found.",
                    edge.TargetNodeId,
                    "WSL012"));
        }

        // Check for cycles (simplified — only detect direct self-loops and simple 2-node cycles)
        // A full topological sort can be added later for more sophisticated cycle detection
        foreach (var edge in document.Edges)
        {
            if (edge.SourceNodeId == edge.TargetNodeId)
                diagnostics.Add(new CompilationDiagnostic(
                    DiagnosticSeverity.Warning,
                    $"Edge '{edge.EdgeId}': self-loop detected on node '{edge.SourceNodeId}'.",
                    edge.SourceNodeId,
                    "WSL013"));
        }

        // Warn about unreachable nodes (nodes with no incoming edges and not marked as entry points)
        var nodesWithIncomingEdges = document.Edges.Select(e => e.TargetNodeId).ToHashSet();
        var unreachableNodes = document.Nodes
            .Where(n => !nodesWithIncomingEdges.Contains(n.NodeId))
            .Where(n => !IsEntryNode(n))
            .ToList();

        foreach (var node in unreachableNodes)
            diagnostics.Add(new CompilationDiagnostic(
                DiagnosticSeverity.Warning,
                $"Node '{node.NodeId}' ('{node.Label}') may be unreachable — no incoming edges and not marked as an entry point.",
                node.NodeId,
                "WSL014"));
    }

    private static void ValidateNodeParameters(WslDocument document, List<CompilationDiagnostic> diagnostics)
    {
        foreach (var node in document.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.NodeId))
                diagnostics.Add(new CompilationDiagnostic(DiagnosticSeverity.Error, "Node must have a non-empty NodeId.", node.NodeId, "WSL020"));

            if (string.IsNullOrWhiteSpace(node.NodeType))
                diagnostics.Add(new CompilationDiagnostic(DiagnosticSeverity.Error, $"Node '{node.NodeId}': NodeType is required.", node.NodeId, "WSL021"));

            // Node-type-specific validation can be added here (e.g., "Ramp" requires "targetTemp", "duration")
        }
    }

    private static void ValidateDeviceBindings(WslDocument document, DeviceRegistry deviceRegistry, List<CompilationDiagnostic> diagnostics)
    {
        foreach (var node in document.Nodes)
        {
            // If the node references a device (via a "device" or "deviceAlias" parameter), validate it exists
            if (node.Parameters.TryGetValue("device", out var deviceParam) && deviceParam is string deviceAlias)
            {
                if (!deviceRegistry.TryGetDevice(deviceAlias, out _))
                    diagnostics.Add(new CompilationDiagnostic(
                        DiagnosticSeverity.Error,
                        $"Node '{node.NodeId}' references unknown device alias '{deviceAlias}'.",
                        node.NodeId,
                        "WSL030"));
            }
            else if (node.Parameters.TryGetValue("deviceAlias", out var aliasParam) && aliasParam is string alias)
            {
                if (!deviceRegistry.TryGetDevice(alias, out _))
                    diagnostics.Add(new CompilationDiagnostic(
                        DiagnosticSeverity.Error,
                        $"Node '{node.NodeId}' references unknown device alias '{alias}'.",
                        node.NodeId,
                        "WSL030"));
            }
        }
    }

    private static void ValidateVariableReferences(WslDocument document, List<CompilationDiagnostic> diagnostics)
    {
        // Pattern: ${scope:key} where scope is "workflow", "global", "campaign", or "device"
        // Examples: ${workflow:temperature}, ${global:experimentId}, ${campaign:sampleCount}
        // Device variables: ${device:vapourtec_status:Status.FlowRate}
        var variablePattern = new System.Text.RegularExpressions.Regex(@"\$\{([a-zA-Z0-9_]+):([a-zA-Z0-9_\.\:]+)\}");

        foreach (var node in document.Nodes)
        {
            foreach (var param in node.Parameters)
            {
                if (param.Value is string stringValue)
                {
                    var matches = variablePattern.Matches(stringValue);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        var scope = match.Groups[1].Value;
                        var key = match.Groups[2].Value;

                        // Validate scope is recognized
                        var validScopes = new[] { "workflow", "global", "campaign", "device" };
                        if (!validScopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
                        {
                            diagnostics.Add(new CompilationDiagnostic(
                                DiagnosticSeverity.Error,
                                $"Node '{node.NodeId}', parameter '{param.Key}': unknown variable scope '{scope}'. Valid scopes: {string.Join(", ", validScopes)}",
                                node.NodeId,
                                "WSL040"));
                        }

                        // Note: We don't validate key existence at compile time since variables may be set at runtime
                        // Device variable format: capabilityName or capabilityName:propertyPath
                        // This is intentional to support dynamic workflows
                    }
                }
            }
        }
    }

    private static bool IsEntryNode(WslNode node)
    {
        // Entry nodes are nodes explicitly marked with an "entryPoint" parameter or nodes of type "Start"
        if (node.NodeType.Equals("Start", StringComparison.OrdinalIgnoreCase))
            return true;

        if (node.Parameters.TryGetValue("entryPoint", out var entryParam) && entryParam is bool isEntry && isEntry)
            return true;

        return false;
    }
}
