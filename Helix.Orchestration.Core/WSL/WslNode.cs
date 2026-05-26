namespace Helix.Orchestration.Core.WSL;

/// <summary>
/// A WSL node represents a single step or device action in the workflow authoring graph.
/// </summary>
/// <param name="NodeId">Unique node identifier within the document.</param>
/// <param name="Label">Human-readable label shown in Node-Flow UI.</param>
/// <param name="NodeType">Discriminator, e.g. "Ramp", "Hold", "Sample", "Decision".</param>
/// <param name="Parameters">Freeform parameters passed to the runtime handler.</param>
public sealed record WslNode(
    string NodeId,
    string Label,
    string NodeType,
    IReadOnlyDictionary<string, object?> Parameters);
