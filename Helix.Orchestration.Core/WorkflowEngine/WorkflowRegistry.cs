using System.Collections.Concurrent;

namespace Helix.Orchestration.Core.WorkflowEngine;

/// <summary>
/// Registry for workflow definitions.
/// Maps workflow names to their implementations for lookup and invocation by the executor.
/// </summary>
public sealed class WorkflowRegistry
{
    private readonly ConcurrentDictionary<string, IWorkflow> _workflows = new();

    /// <summary>
    /// Registers a workflow implementation with a unique name.
    /// </summary>
    /// <param name="workflowName">Unique identifier for the workflow (e.g., "SetupReactor", "RunAnalysis").</param>
    /// <param name="workflow">The workflow implementation.</param>
    /// <exception cref="ArgumentException">Thrown if a workflow with the same name is already registered.</exception>
    public void Register(string workflowName, IWorkflow workflow)
    {
        if (!_workflows.TryAdd(workflowName, workflow))
        {
            throw new ArgumentException($"Workflow '{workflowName}' is already registered.", nameof(workflowName));
        }
    }

    /// <summary>
    /// Retrieves a registered workflow by name.
    /// </summary>
    /// <param name="workflowName">The workflow name to look up.</param>
    /// <returns>The workflow implementation if found.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the workflow is not registered.</exception>
    public IWorkflow Get(string workflowName)
    {
        if (!_workflows.TryGetValue(workflowName, out var workflow))
        {
            throw new KeyNotFoundException($"Workflow '{workflowName}' is not registered.");
        }
        return workflow;
    }

    /// <summary>
    /// Attempts to retrieve a registered workflow by name.
    /// </summary>
    /// <param name="workflowName">The workflow name to look up.</param>
    /// <param name="workflow">The workflow implementation if found, otherwise null.</param>
    /// <returns>True if the workflow was found, false otherwise.</returns>
    public bool TryGet(string workflowName, out IWorkflow? workflow)
    {
        return _workflows.TryGetValue(workflowName, out workflow);
    }

    /// <summary>
    /// Gets all registered workflow names.
    /// </summary>
    public IEnumerable<string> GetAllWorkflowNames() => _workflows.Keys;
}
