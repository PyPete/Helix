namespace Helix.Orchestration.Core.Execution;

/// <summary>
/// Represents a conditional expression that evaluates to true or false.
/// Used by Branch and Loop primitives.
/// </summary>
public sealed class Condition
{
    private readonly Func<WorkflowEngine.WorkflowContext, CancellationToken, Task<bool>> _predicate;

    /// <summary>
    /// Human-readable description of the condition (e.g., "Temperature > 80°C").
    /// </summary>
    public string? Description { get; init; }

    public Condition(Func<WorkflowEngine.WorkflowContext, CancellationToken, Task<bool>> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    /// <summary>
    /// Evaluates the condition.
    /// </summary>
    /// <param name="context">The workflow execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the condition is met, false otherwise.</returns>
    public Task<bool> EvaluateAsync(WorkflowEngine.WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return _predicate(context, cancellationToken);
    }
}
