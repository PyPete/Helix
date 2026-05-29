namespace Helix.Orchestration.Agent.Events;

/// <summary>
/// Abstract base record for all runspace and step lifecycle events emitted by the Agent.
/// </summary>
/// <param name="Type">Machine-readable event type discriminator (e.g. "runspace-started").</param>
/// <param name="RunspaceId">The runspace this event belongs to. Empty string for non-runspace events.</param>
/// <param name="Timestamp">UTC timestamp when the event occurred.</param>
public abstract record RunspaceEvent(string Type, string RunspaceId, DateTimeOffset Timestamp);

// ─── Runspace lifecycle ────────────────────────────────────────────────────────

/// <summary>Emitted when a runspace transitions to the running state.</summary>
/// <param name="RunspaceId">Identifier of the started runspace.</param>
public sealed record RunspaceStartedEvent(string RunspaceId)
    : RunspaceEvent("runspace-started", RunspaceId, DateTimeOffset.UtcNow);

/// <summary>Emitted when a runspace completes all steps successfully.</summary>
/// <param name="RunspaceId">Identifier of the completed runspace.</param>
public sealed record RunspaceCompletedEvent(string RunspaceId)
    : RunspaceEvent("runspace-completed", RunspaceId, DateTimeOffset.UtcNow);

/// <summary>Emitted when a runspace is aborted by operator request.</summary>
/// <param name="RunspaceId">Identifier of the aborted runspace.</param>
public sealed record RunspaceAbortedEvent(string RunspaceId)
    : RunspaceEvent("runspace-aborted", RunspaceId, DateTimeOffset.UtcNow);

/// <summary>Emitted when a runspace terminates due to an unhandled fault.</summary>
/// <param name="RunspaceId">Identifier of the faulted runspace.</param>
/// <param name="FaultMessage">Human-readable description of the fault.</param>
public sealed record RunspaceFaultedEvent(string RunspaceId, string FaultMessage)
    : RunspaceEvent("runspace-faulted", RunspaceId, DateTimeOffset.UtcNow);

// ─── Step lifecycle ────────────────────────────────────────────────────────────

/// <summary>Emitted when a workflow step begins execution.</summary>
/// <param name="RunspaceId">Identifier of the owning runspace.</param>
/// <param name="StepId">Unique identifier of the step.</param>
/// <param name="StepLabel">Human-readable label for the step.</param>
public sealed record StepStartedEvent(string RunspaceId, string StepId, string StepLabel)
    : RunspaceEvent("step-started", RunspaceId, DateTimeOffset.UtcNow);

/// <summary>Emitted when a workflow step finishes without error.</summary>
/// <param name="RunspaceId">Identifier of the owning runspace.</param>
/// <param name="StepId">Unique identifier of the completed step.</param>
public sealed record StepCompletedEvent(string RunspaceId, string StepId)
    : RunspaceEvent("step-completed", RunspaceId, DateTimeOffset.UtcNow);

/// <summary>Emitted when a workflow step terminates due to a fault.</summary>
/// <param name="RunspaceId">Identifier of the owning runspace.</param>
/// <param name="StepId">Unique identifier of the faulted step.</param>
/// <param name="FaultMessage">Human-readable description of the step fault.</param>
public sealed record StepFaultedEvent(string RunspaceId, string StepId, string FaultMessage)
    : RunspaceEvent("step-faulted", RunspaceId, DateTimeOffset.UtcNow);

// ─── Live data events ──────────────────────────────────────────────────────────

/// <summary>
/// Emitted when a scalar trend value is produced during runspace execution.
/// </summary>
/// <param name="TrendName">Name of the trend (e.g. "reactor.temperature").</param>
/// <param name="Value">Current scalar value.</param>
/// <param name="Unit">Engineering unit of the value (e.g. "°C", "bar", "rpm"). May be empty.</param>
/// <param name="Timestamp">UTC timestamp of the measurement.</param>
public sealed record TrendValueEvent(string TrendName, double Value, string Unit, DateTimeOffset Timestamp)
    : RunspaceEvent("trend-value", "", Timestamp);

/// <summary>
/// Emitted when the set of active interlock violations changes.
/// One event is published per violation rule change.
/// </summary>
/// <param name="RuleId">Unique identifier of the interlock rule.</param>
/// <param name="Description">Human-readable description of the interlock condition.</param>
/// <param name="Severity">Severity level of the violation (e.g. "Critical", "Warning").</param>
/// <param name="Triggered">True if the rule is now violated; false if it has cleared.</param>
/// <param name="Timestamp">UTC timestamp when the change was detected.</param>
public sealed record SafetyEvent(string RuleId, string Description, string Severity, bool Triggered, DateTimeOffset Timestamp)
    : RunspaceEvent("safety-event", "", Timestamp);
