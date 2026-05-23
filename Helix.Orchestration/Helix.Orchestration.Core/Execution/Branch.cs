namespace Helix.Orchestration.Core.Execution;

/// <summary>
/// Represents a conditional branch: if condition is true, execute the "then" step, otherwise execute the "else" step (if provided).
/// </summary>
public sealed class Branch : WorkflowStep
{
    /// <summary>
    /// The condition to evaluate.
    /// </summary>
    public required Condition Condition { get; init; }

    /// <summary>
    /// Step to execute if the condition is true.
    /// </summary>
    public required WorkflowStep ThenStep { get; init; }

    /// <summary>
    /// Optional step to execute if the condition is false.
    /// </summary>
    public WorkflowStep? ElseStep { get; init; }

    public override async Task ExecuteAsync(WorkflowEngine.WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var conditionMet = await Condition.EvaluateAsync(context, cancellationToken);

        if (conditionMet)
        {
            await ThenStep.ExecuteAsync(context, cancellationToken);
        }
        else if (ElseStep is not null)
        {
            await ElseStep.ExecuteAsync(context, cancellationToken);
        }
    }
}
