namespace Helix.Orchestration.Core.Capabilities;

/// <summary>
/// Result of a capability invocation.
/// </summary>
public sealed record CapabilityInvocationResult
{
    /// <summary>
    /// Indicates whether the invocation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Optional return value from the capability (e.g., measurement data, status code).
    /// </summary>
    public object? ReturnValue { get; init; }

    /// <summary>
    /// Error message if the invocation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Exception details if the invocation threw an exception.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Duration of the capability invocation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Timestamp when the invocation started.
    /// </summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static CapabilityInvocationResult Succeeded(object? returnValue = null, TimeSpan? duration = null)
    {
        return new CapabilityInvocationResult
        {
            Success = true,
            ReturnValue = returnValue,
            Duration = duration ?? TimeSpan.Zero
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static CapabilityInvocationResult Failed(string errorMessage, Exception? exception = null, TimeSpan? duration = null)
    {
        return new CapabilityInvocationResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Duration = duration ?? TimeSpan.Zero
        };
    }
}
