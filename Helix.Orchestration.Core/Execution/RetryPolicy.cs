namespace Helix.Orchestration.Core.Execution;

/// <summary>
/// Defines a retry policy for step execution failures.
/// </summary>
public sealed class RetryPolicy
{
    /// <summary>
    /// Maximum number of retry attempts (excluding the initial attempt).
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Delay between retries. For exponential backoff, this is the base delay.
    /// </summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// If true, uses exponential backoff: delay doubles on each retry.
    /// If false, uses fixed delay.
    /// </summary>
    public bool UseExponentialBackoff { get; init; } = false;

    /// <summary>
    /// Optional predicate to determine if a specific exception should trigger a retry.
    /// If null, all exceptions trigger retries.
    /// </summary>
    public Func<Exception, bool>? ShouldRetry { get; init; }

    /// <summary>
    /// Executes an action with retry logic according to this policy.
    /// </summary>
    public async Task ExecuteWithRetryAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= MaxRetries)
        {
            try
            {
                await action();
                return; // Success
            }
            catch (Exception ex) when (attempt < MaxRetries && (ShouldRetry?.Invoke(ex) ?? true))
            {
                lastException = ex;
                attempt++;

                var delay = UseExponentialBackoff
                    ? TimeSpan.FromMilliseconds(RetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1))
                    : RetryDelay;

                await Task.Delay(delay, cancellationToken);
            }
        }

        // All retries exhausted
        throw lastException ?? new InvalidOperationException("Retry logic failed without capturing an exception.");
    }
}
