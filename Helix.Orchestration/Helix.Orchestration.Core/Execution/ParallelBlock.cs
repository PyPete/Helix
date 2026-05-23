namespace Helix.Orchestration.Core.Execution;

/// <summary>
/// Represents a parallel execution block: executes multiple steps concurrently and waits for all to complete.
/// </summary>
public sealed class ParallelBlock : WorkflowStep
{
    /// <summary>
    /// The steps to execute in parallel.
    /// </summary>
    public required IReadOnlyList<WorkflowStep> Steps { get; init; }

    /// <summary>
    /// If true, waits for all steps to complete even if some fail.
    /// If false, stops on first failure and cancels remaining tasks.
    /// </summary>
    public bool WaitForAll { get; init; } = true;

    public override async Task ExecuteAsync(WorkflowEngine.WorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (WaitForAll)
        {
            await Task.WhenAll(Steps.Select(step => step.ExecuteAsync(context, cancellationToken)));
        }
        else
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var tasks = Steps.Select(step => ExecuteWithCancellationAsync(step, context, cts)).ToArray();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch
            {
                await cts.CancelAsync();
                throw;
            }
        }
    }

    private static async Task ExecuteWithCancellationAsync(WorkflowStep step, WorkflowEngine.WorkflowContext context, CancellationTokenSource cts)
    {
        try
        {
            await step.ExecuteAsync(context, cts.Token);
        }
        catch
        {
            await cts.CancelAsync();
            throw;
        }
    }
}
