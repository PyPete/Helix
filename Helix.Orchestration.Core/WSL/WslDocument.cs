namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// Root WSL authoring document — a directed graph of nodes and edges
/// that can be compiled into a <see cref="RunspaceDefinition"/>.
/// </summary>
public sealed record WslDocument
{
    /// <summary>Unique document identifier.</summary>
    public string DocumentId { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Human-readable name for this workflow definition.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Schema version for forward/backward compatibility.</summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>Author or team that created the document.</summary>
    public string? Author { get; init; }

    /// <summary>Free-text description of the workflow intent.</summary>
    public string? Description { get; init; }

    /// <summary>All nodes in the graph.</summary>
    public IReadOnlyList<WslNode> Nodes { get; init; } = [];

    /// <summary>All directed edges in the graph.</summary>
    public IReadOnlyList<WslEdge> Edges { get; init; } = [];

    /// <summary>
    /// Compiles the document into a <see cref="RunspaceDefinition"/> suitable for agent execution.
    /// </summary>
    /// <exception cref="WslValidationException">Thrown when the document is structurally invalid.</exception>
    public RunspaceDefinition Compile()
    {
        Validate();

        return new RunspaceDefinition(
            DefinitionId: DocumentId,
            Name: Name,
            Nodes: Nodes,
            Edges: Edges,
            Metadata: new Dictionary<string, object?>
            {
                ["schemaVersion"] = SchemaVersion,
                ["author"] = Author,
                ["description"] = Description
            });
    }

    private void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Document must have a non-empty Name.");

        var nodeIds = Nodes.Select(n => n.NodeId).ToHashSet();

        foreach (var edge in Edges)
        {
            if (!nodeIds.Contains(edge.SourceNodeId))
                errors.Add($"Edge '{edge.EdgeId}': source node '{edge.SourceNodeId}' not found.");
            if (!nodeIds.Contains(edge.TargetNodeId))
                errors.Add($"Edge '{edge.EdgeId}': target node '{edge.TargetNodeId}' not found.");
        }

        if (errors.Count > 0)
            throw new WslValidationException(errors);
    }
}
