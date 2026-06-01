namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// A directed edge in the WSL graph, connecting a source node to a target node.
/// An optional condition expression controls whether the edge is traversed.
/// </summary>
/// <param name="EdgeId">Unique edge identifier.</param>
/// <param name="SourceNodeId">Id of the upstream node.</param>
/// <param name="TargetNodeId">Id of the downstream node.</param>
/// <param name="Condition">
/// Optional boolean expression evaluated at runtime.
/// Null means the edge is always traversed.
/// </param>
public sealed record WslEdge(
    string EdgeId,
    string SourceNodeId,
    string TargetNodeId,
    string? Condition = null,
    string? EdgeKind = null);   // "exec" | "data" | null (null treated as exec)
