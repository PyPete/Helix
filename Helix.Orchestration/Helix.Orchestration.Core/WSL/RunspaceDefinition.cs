using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// Compiled, immutable representation of a WSL workflow ready for agent execution.
/// Produced by <see cref="WslDocument.Compile"/>.
/// </summary>
/// <param name="DefinitionId">Stable identifier matching the source document.</param>
/// <param name="Name">Human-readable workflow name.</param>
/// <param name="Nodes">Ordered list of compiled nodes.</param>
/// <param name="Edges">Directed edges in the execution graph.</param>
/// <param name="Metadata">Freeform metadata carried from the source document.</param>
public sealed record RunspaceDefinition(
    string DefinitionId,
    string Name,
    IReadOnlyList<WslNode> Nodes,
    IReadOnlyList<WslEdge> Edges,
    IReadOnlyDictionary<string, object?> Metadata);
