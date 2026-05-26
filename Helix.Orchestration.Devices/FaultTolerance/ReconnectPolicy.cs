namespace Helix.Orchestration.Devices.FaultTolerance;

/// <summary>
/// Defines the retry policy for transport reconnection after a fault.
/// </summary>
public sealed class ReconnectPolicy
{
    /// <summary>
    /// Maximum number of reconnection attempts (-1 for infinite).
    /// </summary>
    public int MaxAttempts { get; init; } = 10;

    /// <summary>
    /// Initial delay in milliseconds before the first reconnection attempt.
    /// </summary>
    public int InitialDelayMs { get; init; } = 500;

    /// <summary>
    /// Maximum delay in milliseconds between reconnection attempts.
    /// </summary>
    public int MaxDelayMs { get; init; } = 30_000;

    /// <summary>
    /// Multiplier for exponential backoff (e.g., 2.0 doubles delay each attempt).
    /// </summary>
    public double BackoffMultiplier { get; init; } = 2.0;

    /// <summary>
    /// Default reconnect policy with 10 attempts, 500ms initial delay, 30s max delay, and 2x backoff.
    /// </summary>
    public static ReconnectPolicy Default { get; } = new();

    /// <summary>
    /// Default reconnect policy with infinite attempts.
    /// </summary>
    public static ReconnectPolicy Infinite { get; } = new() { MaxAttempts = -1 };

    /// <summary>
    /// Computes the delay for a given reconnection attempt using exponential backoff.
    /// </summary>
    /// <param name="attempt">The current attempt number (1-based).</param>
    /// <returns>The delay in milliseconds for this attempt.</returns>
    public int ComputeDelay(int attempt)
    {
        if (attempt <= 0)
            throw new ArgumentOutOfRangeException(nameof(attempt), "Attempt must be greater than zero");

        var delay = InitialDelayMs * Math.Pow(BackoffMultiplier, attempt - 1);
        return (int)Math.Min(delay, MaxDelayMs);
    }
}
