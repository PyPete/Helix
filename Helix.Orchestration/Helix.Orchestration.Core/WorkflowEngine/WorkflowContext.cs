using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Experiment;
using Helix.Orchestration.Core.Safety;
using Helix.Orchestration.Core.State;
using Helix.Orchestration.Core.Trends;

namespace Helix.Orchestration.Core.WorkflowEngine;

/// <summary>
/// Provides execution context for a running workflow instance.
/// This is the single dependency-injection surface passed to every workflow step.
/// </summary>
public sealed class WorkflowContext
{
    /// <summary>
    /// Unique identifier for this workflow execution instance.
    /// </summary>
    public required string WorkflowId { get; init; }

    /// <summary>
    /// Capability invocation layer for device-agnostic operations.
    /// </summary>
    public required ICapabilityInvoker Capabilities { get; init; }

    /// <summary>
    /// Trend data provider for real-time streaming analytics.
    /// </summary>
    public required ITrendProvider Trends { get; init; }

    /// <summary>
    /// Safety engine for interlocks and emergency stop handling.
    /// </summary>
    public required ISafetyEngine Safety { get; init; }

    /// <summary>
    /// Durable workflow state store for persistence across crashes and resumption.
    /// </summary>
    public required IWorkflowStateStore State { get; init; }

    /// <summary>
    /// Optional experiment context for experiment-centric workflows.
    /// Null for non-experiment automation tasks.
    /// </summary>
    public IExperimentContext? Experiment { get; init; }
}
