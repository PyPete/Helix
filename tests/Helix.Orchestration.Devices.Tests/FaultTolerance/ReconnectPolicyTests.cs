using Helix.Orchestration.Devices.FaultTolerance;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.FaultTolerance;

public sealed class ReconnectPolicyTests
{
    [Fact]
    public void ComputeDelay_UsesExponentialBackoff()
    {
        var policy = new ReconnectPolicy
        {
            InitialDelayMs = 100,
            BackoffMultiplier = 2.0,
            MaxDelayMs = 10_000
        };

        Assert.Equal(100, policy.ComputeDelay(1));
        Assert.Equal(200, policy.ComputeDelay(2));
        Assert.Equal(400, policy.ComputeDelay(3));
        Assert.Equal(800, policy.ComputeDelay(4));
    }

    [Fact]
    public void ComputeDelay_CapsAtMaxDelay()
    {
        var policy = new ReconnectPolicy
        {
            InitialDelayMs = 500,
            BackoffMultiplier = 3.0,
            MaxDelayMs = 2_000
        };

        Assert.Equal(500, policy.ComputeDelay(1));
        Assert.Equal(1500, policy.ComputeDelay(2));
        Assert.Equal(2000, policy.ComputeDelay(3));
        Assert.Equal(2000, policy.ComputeDelay(4));
    }

    [Fact]
    public void InfinitePolicy_UsesMinusOneForMaxAttempts()
    {
        Assert.Equal(-1, ReconnectPolicy.Infinite.MaxAttempts);
    }

    [Fact]
    public void DefaultPolicy_UsesExpectedDefaults()
    {
        Assert.Equal(10, ReconnectPolicy.Default.MaxAttempts);
        Assert.Equal(500, ReconnectPolicy.Default.InitialDelayMs);
        Assert.Equal(30_000, ReconnectPolicy.Default.MaxDelayMs);
        Assert.Equal(2.0, ReconnectPolicy.Default.BackoffMultiplier);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ComputeDelay_WithNonPositiveAttempt_Throws(int attempt)
    {
        var policy = new ReconnectPolicy();

        Assert.Throws<ArgumentOutOfRangeException>(() => policy.ComputeDelay(attempt));
    }
}
