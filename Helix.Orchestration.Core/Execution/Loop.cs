namespace Helix.Orchestration.Core.Execution;

/// <summary>
/// Represents a loop: repeatedly executes a step while a condition is true.
/// </summary>
public sealed class Loop : WorkflowStep
{
    /// <summary>
    /// The condition to evaluate before each iteration.
    /// </summary>
    public required Condition Condition { get; init; }

    /// <summary>
    /// The step to execute on each iteration.
    /// </summary>
    public required WorkflowStep BodyStep { get; init; }

    /// <summary>
    /// Optional maximum iteration count to prevent infinite loops.
    /// </summary>
    public int? MaxIterations { get; init; }

    public override async Task ExecuteAsync(WorkflowEngine.WorkflowContext context, CancellationToken cancellationToken = default)
    {
        int iteration = 0;

        while (await Condition.EvaluateAsync(context, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await BodyStep.ExecuteAsync(context, cancellationToken);

            iteration++;
            if (MaxIterations.HasValue && iteration >= MaxIterations.Value)
            {
                break;
            }
        }
    }
}
