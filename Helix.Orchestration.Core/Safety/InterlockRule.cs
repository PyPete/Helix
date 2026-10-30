namespace Helix.Orchestration.Core.Safety;

/// <summary>
/// Represents an interlock rule that must be satisfied for safe operation.
/// </summary>
public sealed class InterlockRule
{
    private readonly Func<Task<bool>> _condition;

    /// <summary>
    /// Unique identifier for the interlock rule.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Human-readable description of the interlock condition.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Severity level of the interlock violation.
    /// </summary>
    public InterlockSeverity Severity { get; init; } = InterlockSeverity.Critical;

    /// <summary>
    /// Timestamp of the last evaluation.
    /// </summary>
    public DateTimeOffset LastEvaluatedAt { get; internal set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Result of the last evaluation (null if not yet evaluated).
    /// </summary>
    public bool? LastResult { get; internal set; }

    /// <summary>
    /// Creates a new interlock rule.
    /// </summary>
    /// <param name="condition">Async predicate that returns true if the condition is met (safe), false if violated.</param>
    public InterlockRule(Func<Task<bool>> condition)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
    }

    /// <summary>
    /// Evaluates the interlock condition.
    /// </summary>
    /// <returns>True if the condition is satisfied (safe), false if violated.</returns>
    public async Task<bool> EvaluateAsync()
    {
        LastEvaluatedAt = DateTimeOffset.UtcNow;
        LastResult = await _condition();
        return LastResult.Value;
    }
}

/// <summary>
/// Severity levels for interlock violations.
/// </summary>
public enum InterlockSeverity
{
    /// <summary>
    /// Informational warning, does not stop execution.
    /// </summary>
    Info,

    /// <summary>
    /// Warning, requires acknowledgment but allows continuation.
    /// </summary>
    Warning,

    /// <summary>
    /// Critical violation, triggers emergency stop.
    /// </summary>
    Critical
}
