namespace Helix.Orchestration.Devices.FaultTolerance;

/// <summary>
/// Implements a circuit breaker pattern to prevent cascading failures.
/// States: Closed (normal), Open (blocking calls after threshold), HalfOpen (test recovery).
/// </summary>
public sealed class CircuitBreaker
{
    private readonly object _lock = new();
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _consecutiveFailures;
    private DateTimeOffset _lastFailureTime;

    /// <summary>
    /// Number of consecutive failures before opening the circuit.
    /// </summary>
    public int FailureThreshold { get; init; } = 5;

    /// <summary>
    /// Time in milliseconds before attempting to move from Open to HalfOpen.
    /// </summary>
    public int ResetTimeoutMs { get; init; } = 60_000;

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Event raised when the circuit breaker state changes.
    /// </summary>
    public event EventHandler<CircuitBreakerStateChangedEventArgs>? OnStateChanged;

    /// <summary>
    /// Executes an asynchronous operation protected by the circuit breaker.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit is open.</exception>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        CheckState();

        try
        {
            var result = await operation();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
    }

    private void CheckState()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.Open)
            {
                var elapsed = DateTimeOffset.UtcNow - _lastFailureTime;
                if (elapsed.TotalMilliseconds >= ResetTimeoutMs)
                {
                    TransitionTo(CircuitBreakerState.HalfOpen);
                }
                else
                {
                    throw new CircuitBreakerOpenException($"Circuit breaker is open. Reset timeout: {ResetTimeoutMs}ms");
                }
            }
        }
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            _consecutiveFailures = 0;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                TransitionTo(CircuitBreakerState.Closed);
            }
        }
    }

    private void OnFailure(Exception ex)
    {
        lock (_lock)
        {
            _consecutiveFailures++;
            _lastFailureTime = DateTimeOffset.UtcNow;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                TransitionTo(CircuitBreakerState.Open);
            }
            else if (_consecutiveFailures >= FailureThreshold)
            {
                TransitionTo(CircuitBreakerState.Open);
            }
        }
    }

    private void TransitionTo(CircuitBreakerState newState)
    {
        var oldState = _state;
        _state = newState;
        OnStateChanged?.Invoke(this, new CircuitBreakerStateChangedEventArgs(oldState, newState));
    }

    /// <summary>
    /// Manually resets the circuit breaker to Closed state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _consecutiveFailures = 0;
            TransitionTo(CircuitBreakerState.Closed);
        }
    }
}

/// <summary>
/// Represents the state of a circuit breaker.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Circuit is closed and operations are allowed.
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open and operations are blocked.
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is testing recovery (single operation allowed).
    /// </summary>
    HalfOpen
}

/// <summary>
/// Event args for circuit breaker state changes.
/// </summary>
public sealed class CircuitBreakerStateChangedEventArgs : EventArgs
{
    public CircuitBreakerState OldState { get; }
    public CircuitBreakerState NewState { get; }

    public CircuitBreakerStateChangedEventArgs(CircuitBreakerState oldState, CircuitBreakerState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

/// <summary>
/// Exception thrown when the circuit breaker is open.
/// </summary>
public sealed class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
