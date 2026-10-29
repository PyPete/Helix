using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Safety;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Safety;

public sealed class SafetyStateTests
{
    [Fact]
    public async Task EvaluateInterlocksAsync_WithoutRules_ReportsUnknownAndDoesNotPass()
    {
        using var engine = new SafetyEngine();

        var passed = await engine.EvaluateInterlocksAsync();

        Assert.False(passed);
        Assert.Equal(CommandCheckStatus.Unknown, engine.LastSafetyState.Status);
        Assert.Contains("No interlock rules", engine.LastSafetyState.Reason, StringComparison.Ordinal);
        Assert.NotEqual("0", engine.LastSafetyState.ContextRevision);
    }

    [Fact]
    public async Task EvaluateInterlocksAsync_CriticalFailure_ReportsUnsafeState()
    {
        using var engine = new SafetyEngine();
        engine.RegisterInterlock(new InterlockRule(() => Task.FromResult(false))
        {
            RuleId = "pressure",
            Description = "Pressure interlock failed",
            Severity = InterlockSeverity.Critical
        });

        var passed = await engine.EvaluateInterlocksAsync();

        Assert.False(passed);
        Assert.Equal(CommandCheckStatus.Failed, engine.LastSafetyState.Status);
        Assert.Equal("Pressure interlock failed", engine.LastSafetyState.Reason);
        Assert.NotEqual(default, engine.LastSafetyState.OccurredAtUtc);
    }

    [Fact]
    public async Task TriggerEmergencyStopAsync_CancelsImmediatelyAndPublishesTransition()
    {
        using var engine = new SafetyEngine();

        await engine.TriggerEmergencyStopAsync("manual stop");

        Assert.True(engine.IsEmergencyStopActive);
        Assert.True(engine.EmergencyStopToken.IsCancellationRequested);
        Assert.Equal(CommandCheckStatus.Failed, engine.LastSafetyState.Status);
        Assert.Equal("manual stop", engine.LastEmergencyStopTransition?.Reason);
        Assert.True(engine.LastEmergencyStopTransition?.IsActive);
    }
}
