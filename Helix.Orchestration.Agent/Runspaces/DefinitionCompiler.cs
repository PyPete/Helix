using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Experiment;
using Helix.Orchestration.Core.WSL;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Runspaces;

/// <summary>
/// Compiles a runspace definition into an executable delegate for sequential node execution.
/// </summary>
public sealed class DefinitionCompiler(
    ICapabilityInvoker capabilityInvoker,
    IExperimentContext experimentContext,
    ILogger<DefinitionCompiler> logger)
{
    private static class LogEvents
    {
        public static readonly EventId NodeExecuting = new(2400, nameof(NodeExecuting));
        public static readonly EventId NodeExecuted = new(2401, nameof(NodeExecuted));
    }

    /// <summary>
    /// Produces a runnable delegate from a runspace definition.
    /// </summary>
    public Func<CancellationToken, Task> Compile(RunspaceDefinition definition)
    {
        return async cancellationToken =>
        {
            foreach (var node in definition.Nodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                logger.LogDebug(LogEvents.NodeExecuting, "Executing node {NodeId} ({NodeType})", node.NodeId, node.NodeType);
                await capabilityInvoker.InvokeAsync(node.NodeType, node.Parameters, cancellationToken);
                await experimentContext.RecordDataAsync(node.NodeId, node.Parameters);
                logger.LogDebug(LogEvents.NodeExecuted, "Executed node {NodeId} ({NodeType})", node.NodeId, node.NodeType);
            }
        };
    }
}
