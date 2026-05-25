using Helix.Orchestration.Devices.FaultTolerance;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.FaultTolerance;

public sealed class CircuitBreakerTests
{
    [Fact]
    public async Task ExecuteAsync_TransitionsClosedToOpen_AfterFailureThreshold()
    {
        var breaker = new CircuitBreaker
        {
            FailureThreshold = 2,
            ResetTimeoutMs = 10_000
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("first")));
        Assert.Equal(CircuitBreakerState.Closed, breaker.State);

        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("second")));
        Assert.Equal(CircuitBreakerState.Open, breaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOpenBeforeResetTimeout_RejectsCalls()
    {
        var breaker = new CircuitBreaker
        {
            FailureThreshold = 1,
            ResetTimeoutMs = 60_000
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("fail")));

        var exception = await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => breaker.ExecuteAsync(() => Task.FromResult(42)));

        Assert.Contains("Circuit breaker is open", exception.Message);
        Assert.Equal(CircuitBreakerState.Open, breaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_AfterResetTimeout_TransitionsToHalfOpen_AndSuccessCloses()
    {
        var breaker = new CircuitBreaker
        {
            FailureThreshold = 1,
            ResetTimeoutMs = 20
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("fail")));
        Assert.Equal(CircuitBreakerState.Open, breaker.State);

        await Task.Delay(50);

        var result = await breaker.ExecuteAsync(() => Task.FromResult(123));

        Assert.Equal(123, result);
        Assert.Equal(CircuitBreakerState.Closed, breaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_AfterResetTimeout_TransitionsToHalfOpen_AndFailureReopens()
    {
        var breaker = new CircuitBreaker
        {
            FailureThreshold = 1,
            ResetTimeoutMs = 20
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("fail")));
        Assert.Equal(CircuitBreakerState.Open, breaker.State);

        await Task.Delay(50);

        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("half-open fail")));

        Assert.Equal(CircuitBreakerState.Open, breaker.State);
    }
}
