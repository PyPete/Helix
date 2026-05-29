using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.Safety;

namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// Default implementation of <see cref="IWslCompiler"/> that validates WSL documents
/// and produces executable <see cref="RunspaceDefinition"/> instances.
///
/// Applies four graph transformations before emitting the runspace:
/// <list type="number">
///   <item><description>Safety envelope injection (when a safety engine is provided)</description></item>
///   <item><description>Loop node expansion</description></item>
///   <item><description>Branch node flattening</description></item>
///   <item><description>Parallel node fork-join expansion</description></item>
/// </list>
/// </summary>
public sealed class WslCompiler : IWslCompiler
{
    // Threat T-06-07: cap loop iterations to prevent memory exhaustion from malformed WSL
    private const int MaxLoopIterations = 1000;

    private static readonly HashSet<string> SupportedVersions = ["1.0"];

    /// <inheritdoc />
    public CompilationResult Compile(
        WslDocument document,
        DeviceRegistry deviceRegistry,
        ISafetyEngine? safetyEngine = null)
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

        // If any errors, return failed result before running transforms
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return CompilationResult.Failed(diagnostics);

        // 6. Apply graph transformations (mutable copies)
        var transformedNodes = new List<WslNode>(document.Nodes);
        var transformedEdges = new List<WslEdge>(document.Edges);

        if (safetyEngine != null)
            transformedNodes = InjectSafetyEnvelope(transformedNodes, safetyEngine, diagnostics);

        (transformedNodes, transformedEdges) = ExpandLoopNodes(transformedNodes, transformedEdges, diagnostics);
        (transformedNodes, transformedEdges) = FlattenBranchNodes(transformedNodes, transformedEdges, diagnostics);
        (transformedNodes, transformedEdges) = FlattenParallelNodes(transformedNodes, transformedEdges, diagnostics);

        // 7. Emit RunspaceDefinition
        var definition = new RunspaceDefinition(
            DefinitionId: document.DocumentId,
            Name: document.Name,
            Nodes: transformedNodes,
            Edges: transformedEdges,
            Metadata: new Dictionary<string, object?>
            {
                ["schemaVersion"] = document.SchemaVersion,
                ["author"] = document.Author,
                ["description"] = document.Description,
                ["compiledAt"] = DateTimeOffset.UtcNow
            });

        return CompilationResult.Succeeded(definition, diagnostics);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validation methods
    // ─────────────────────────────────────────────────────────────────────────

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

        // Check for self-loops
        foreach (var edge in document.Edges)
        {
            if (edge.SourceNodeId == edge.TargetNodeId)
                diagnostics.Add(new CompilationDiagnostic(
                    DiagnosticSeverity.Warning,
                    $"Edge '{edge.EdgeId}': self-loop detected on node '{edge.SourceNodeId}'.",
                    edge.SourceNodeId,
                    "WSL013"));
        }

        // Warn about unreachable nodes (no incoming edges and not an entry point)
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
        }
    }

    private static void ValidateDeviceBindings(WslDocument document, DeviceRegistry deviceRegistry, List<CompilationDiagnostic> diagnostics)
    {
        foreach (var node in document.Nodes)
        {
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
                        var validScopes = new[] { "workflow", "global", "campaign", "device" };
                        if (!validScopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
                        {
                            diagnostics.Add(new CompilationDiagnostic(
                                DiagnosticSeverity.Error,
                                $"Node '{node.NodeId}', parameter '{param.Key}': unknown variable scope '{scope}'. Valid scopes: {string.Join(", ", validScopes)}",
                                node.NodeId,
                                "WSL040"));
                        }
                    }
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Graph transformation methods
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshots current interlock violations (read-only) and prepends a SafetyGuard node
    /// for each violation.  Never calls RegisterInterlock or TriggerEmergencyStopAsync.
    /// Threat T-06-08: read-only access verified by not calling any mutating ISafetyEngine methods.
    /// </summary>
    private static List<WslNode> InjectSafetyEnvelope(
        IReadOnlyList<WslNode> nodes,
        ISafetyEngine safetyEngine,
        List<CompilationDiagnostic> diagnostics)
    {
        var violations = safetyEngine.GetViolationsAsync().GetAwaiter().GetResult().ToList();

        var result = new List<WslNode>(nodes);

        foreach (var violation in violations)
        {
            var guardNode = new WslNode(
                NodeId: $"safety-guard-{violation.RuleId}",
                Label: $"Safety: {violation.Description}",
                NodeType: "SafetyGuard",
                Parameters: new Dictionary<string, object?>
                {
                    ["ruleId"] = violation.RuleId,
                    ["severity"] = violation.Severity.ToString(),
                    ["description"] = violation.Description
                });

            // Prepend guard node (insert at position 0 — before first non-entry node)
            result.Insert(0, guardNode);

            diagnostics.Add(new CompilationDiagnostic(
                DiagnosticSeverity.Warning,
                $"Safety guard injected for rule '{violation.RuleId}': {violation.Description}",
                null,
                "WSL050"));
        }

        return result;
    }

    /// <summary>
    /// Expands Loop nodes by unrolling the body N times.
    /// Threat T-06-07: iterations capped at <see cref="MaxLoopIterations"/> (1000).
    /// </summary>
    private static (List<WslNode> nodes, List<WslEdge> edges) ExpandLoopNodes(
        IReadOnlyList<WslNode> nodes,
        IReadOnlyList<WslEdge> edges,
        List<CompilationDiagnostic> diagnostics)
    {
        var resultNodes = new List<WslNode>(nodes);
        var resultEdges = new List<WslEdge>(edges);

        var loopNodes = resultNodes.Where(n => n.NodeType.Equals("Loop", StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var loopNode in loopNodes)
        {
            var iterations = 1;
            if (loopNode.Parameters.TryGetValue("iterations", out var iterObj))
            {
                iterations = Convert.ToInt32(iterObj);
            }

            if (iterations < 1)
            {
                diagnostics.Add(new CompilationDiagnostic(
                    DiagnosticSeverity.Warning,
                    $"Loop node '{loopNode.NodeId}' has iterations < 1; treating as 1.",
                    loopNode.NodeId,
                    "WSL051"));
                iterations = 1;
            }

            // Threat T-06-07: cap iterations
            if (iterations > MaxLoopIterations)
            {
                diagnostics.Add(new CompilationDiagnostic(
                    DiagnosticSeverity.Warning,
                    $"Loop node '{loopNode.NodeId}' iterations {iterations} exceeds maximum {MaxLoopIterations}; capped.",
                    loopNode.NodeId,
                    "WSL051"));
                iterations = MaxLoopIterations;
            }

            // Find body nodes: nodes that the loop node directly connects to
            var loopOutgoingEdges = resultEdges.Where(e => e.SourceNodeId == loopNode.NodeId).ToList();
            var bodyNodeIds = loopOutgoingEdges.Select(e => e.TargetNodeId).ToHashSet();

            if (!bodyNodeIds.Any())
            {
                diagnostics.Add(new CompilationDiagnostic(
                    DiagnosticSeverity.Warning,
                    $"Loop node '{loopNode.NodeId}' has no identifiable body (no outgoing edges); leaving as-is.",
                    loopNode.NodeId,
                    "WSL053"));
                continue;
            }

            // Get body nodes (in document order)
            var bodyNodes = resultNodes.Where(n => bodyNodeIds.Contains(n.NodeId)).ToList();

            // Find predecessor of loop node
            var incomingToLoop = resultEdges.Where(e => e.TargetNodeId == loopNode.NodeId).ToList();

            // Find what comes after the body (outgoing from body to non-body nodes)
            var bodyExitEdges = resultEdges
                .Where(e => bodyNodeIds.Contains(e.SourceNodeId) && !bodyNodeIds.Contains(e.TargetNodeId))
                .ToList();
            var afterBodyNodeIds = bodyExitEdges.Select(e => e.TargetNodeId).ToHashSet();

            // Build expanded copies: for each iteration i in [0, N-1], create copies of body nodes
            var allCopyNodes = new List<(int iteration, WslNode original, WslNode copy)>();
            for (int i = 0; i < iterations; i++)
            {
                foreach (var bodyNode in bodyNodes)
                {
                    var copy = new WslNode(
                        NodeId: $"{bodyNode.NodeId}-loop-{i}",
                        Label: bodyNode.Label,
                        NodeType: bodyNode.NodeType,
                        Parameters: bodyNode.Parameters);
                    allCopyNodes.Add((i, bodyNode, copy));
                }
            }

            // Remove loop node and body nodes from result
            resultNodes.RemoveAll(n => n.NodeId == loopNode.NodeId || bodyNodeIds.Contains(n.NodeId));

            // Add all copy nodes
            foreach (var (_, _, copy) in allCopyNodes)
                resultNodes.Add(copy);

            // Remove all edges involving the loop node and body nodes
            resultEdges.RemoveAll(e =>
                e.SourceNodeId == loopNode.NodeId ||
                e.TargetNodeId == loopNode.NodeId ||
                bodyNodeIds.Contains(e.SourceNodeId) ||
                bodyNodeIds.Contains(e.TargetNodeId));

            var edgeIdx = 0;

            // Wire: predecessor -> first copy of first body node (iteration 0)
            foreach (var inEdge in incomingToLoop)
            {
                var firstBodyCopy = allCopyNodes.First(x => x.iteration == 0 && x.original.NodeId == bodyNodes.First().NodeId).copy;
                resultEdges.Add(new WslEdge($"loop-wire-{loopNode.NodeId}-in-{edgeIdx++}", inEdge.SourceNodeId, firstBodyCopy.NodeId));
            }

            // Wire within each iteration: body[j] -> body[j+1] for each iteration
            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < bodyNodes.Count - 1; j++)
                {
                    var fromCopy = allCopyNodes.First(x => x.iteration == i && x.original.NodeId == bodyNodes[j].NodeId).copy;
                    var toCopy = allCopyNodes.First(x => x.iteration == i && x.original.NodeId == bodyNodes[j + 1].NodeId).copy;
                    resultEdges.Add(new WslEdge($"loop-wire-{loopNode.NodeId}-{i}-{j}", fromCopy.NodeId, toCopy.NodeId));
                }
            }

            // Wire: last body of iteration i -> first body of iteration i+1
            for (int i = 0; i < iterations - 1; i++)
            {
                var lastBodyCopy = allCopyNodes.First(x => x.iteration == i && x.original.NodeId == bodyNodes.Last().NodeId).copy;
                var firstNextBodyCopy = allCopyNodes.First(x => x.iteration == i + 1 && x.original.NodeId == bodyNodes.First().NodeId).copy;
                resultEdges.Add(new WslEdge($"loop-wire-{loopNode.NodeId}-iter-{i}-to-{i + 1}", lastBodyCopy.NodeId, firstNextBodyCopy.NodeId));
            }

            // Wire: last copy of last body node -> following nodes
            var lastIterLastBodyCopy = allCopyNodes.First(x => x.iteration == iterations - 1 && x.original.NodeId == bodyNodes.Last().NodeId).copy;
            foreach (var exitEdge in bodyExitEdges)
            {
                resultEdges.Add(new WslEdge($"loop-wire-{loopNode.NodeId}-out-{edgeIdx++}", lastIterLastBodyCopy.NodeId, exitEdge.TargetNodeId));
            }

            diagnostics.Add(new CompilationDiagnostic(
                DiagnosticSeverity.Info,
                $"Loop node '{loopNode.NodeId}' expanded to {iterations} iteration(s).",
                loopNode.NodeId,
                "WSL052"));
        }

        return (resultNodes, resultEdges);
    }

    /// <summary>
    /// Flattens Branch nodes by labeling outgoing edges with "true"/"false" conditions.
    /// If <see cref="WslEdge"/> already supports a <c>Condition</c> property, uses that.
    /// </summary>
    private static (List<WslNode> nodes, List<WslEdge> edges) FlattenBranchNodes(
        IReadOnlyList<WslNode> nodes,
        IReadOnlyList<WslEdge> edges,
        List<CompilationDiagnostic> diagnostics)
    {
        var resultNodes = new List<WslNode>(nodes);
        var resultEdges = new List<WslEdge>(edges);

        var branchNodes = resultNodes.Where(n => n.NodeType.Equals("Branch", StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var branchNode in branchNodes)
        {
            // Find outgoing edges from this branch node (in document order)
            var outgoingEdges = resultEdges
                .Where(e => e.SourceNodeId == branchNode.NodeId)
                .ToList();

            if (outgoingEdges.Count < 2)
            {
                // Not enough branches — leave as-is (validation may have already flagged this)
                diagnostics.Add(new CompilationDiagnostic(
                    DiagnosticSeverity.Warning,
                    $"Branch node '{branchNode.NodeId}' has fewer than 2 outgoing edges; conditional labelling skipped.",
                    branchNode.NodeId,
                    "WSL054"));
                continue;
            }

            // Label first outgoing edge as "true", second as "false"
            var trueEdge = outgoingEdges[0];
            var falseEdge = outgoingEdges[1];

            // Replace edges with condition-labelled versions
            resultEdges.Remove(trueEdge);
            resultEdges.Remove(falseEdge);
            resultEdges.Add(trueEdge with { Condition = "true" });
            resultEdges.Add(falseEdge with { Condition = "false" });

            diagnostics.Add(new CompilationDiagnostic(
                DiagnosticSeverity.Info,
                $"Branch node '{branchNode.NodeId}' flattened to conditional edges.",
                branchNode.NodeId,
                "WSL054"));
        }

        return (resultNodes, resultEdges);
    }

    /// <summary>
    /// Expands Parallel nodes into a fork-join structure:
    /// predecessor → ParallelFork → [branches] → ParallelJoin → successor.
    /// </summary>
    private static (List<WslNode> nodes, List<WslEdge> edges) FlattenParallelNodes(
        IReadOnlyList<WslNode> nodes,
        IReadOnlyList<WslEdge> edges,
        List<CompilationDiagnostic> diagnostics)
    {
        var resultNodes = new List<WslNode>(nodes);
        var resultEdges = new List<WslEdge>(edges);

        var parallelNodes = resultNodes.Where(n => n.NodeType.Equals("Parallel", StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var parallelNode in parallelNodes)
        {
            var forkId = $"{parallelNode.NodeId}-fork";
            var joinId = $"{parallelNode.NodeId}-join";

            var forkNode = new WslNode(forkId, $"Fork ({parallelNode.Label})", "ParallelFork", new Dictionary<string, object?>());
            var joinNode = new WslNode(joinId, $"Join ({parallelNode.Label})", "ParallelJoin", new Dictionary<string, object?>());

            // Edges coming into the parallel node
            var incomingEdges = resultEdges.Where(e => e.TargetNodeId == parallelNode.NodeId).ToList();
            // Edges going out of the parallel node (to branches)
            var outgoingEdges = resultEdges.Where(e => e.SourceNodeId == parallelNode.NodeId).ToList();
            // All branch node IDs (targets of parallel node)
            var branchNodeIds = outgoingEdges.Select(e => e.TargetNodeId).ToHashSet();
            // Edges from branches going to the "after" node (not to other branches)
            var branchExitEdges = resultEdges
                .Where(e => branchNodeIds.Contains(e.SourceNodeId) && !branchNodeIds.Contains(e.TargetNodeId))
                .ToList();
            var afterNodeIds = branchExitEdges.Select(e => e.TargetNodeId).Distinct().ToList();

            // Remove parallel node from result
            resultNodes.Remove(parallelNode);

            // Add fork and join nodes
            resultNodes.Add(forkNode);
            resultNodes.Add(joinNode);

            // Remove original edges
            foreach (var e in incomingEdges) resultEdges.Remove(e);
            foreach (var e in outgoingEdges) resultEdges.Remove(e);
            foreach (var e in branchExitEdges) resultEdges.Remove(e);

            var edgeIdx = 0;

            // Wire: predecessors -> fork
            foreach (var inEdge in incomingEdges)
                resultEdges.Add(new WslEdge($"par-wire-{parallelNode.NodeId}-in-{edgeIdx++}", inEdge.SourceNodeId, forkId));

            // Wire: fork -> branches
            foreach (var outEdge in outgoingEdges)
                resultEdges.Add(new WslEdge($"par-wire-{parallelNode.NodeId}-branch-{edgeIdx++}", forkId, outEdge.TargetNodeId));

            // Wire: branches -> join
            foreach (var branchId in branchNodeIds)
                resultEdges.Add(new WslEdge($"par-wire-{parallelNode.NodeId}-join-{edgeIdx++}", branchId, joinId));

            // Wire: join -> successors
            foreach (var afterNodeId in afterNodeIds)
                resultEdges.Add(new WslEdge($"par-wire-{parallelNode.NodeId}-out-{edgeIdx++}", joinId, afterNodeId));

            diagnostics.Add(new CompilationDiagnostic(
                DiagnosticSeverity.Info,
                $"Parallel node '{parallelNode.NodeId}' expanded to fork-join structure.",
                parallelNode.NodeId,
                "WSL055"));
        }

        return (resultNodes, resultEdges);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static bool IsEntryNode(WslNode node)
    {
        if (node.NodeType.Equals("Start", StringComparison.OrdinalIgnoreCase))
            return true;

        if (node.Parameters.TryGetValue("entryPoint", out var entryParam) && entryParam is bool isEntry && isEntry)
            return true;

        return false;
    }
}
