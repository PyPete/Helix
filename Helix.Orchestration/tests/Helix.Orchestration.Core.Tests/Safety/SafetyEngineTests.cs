using Helix.Orchestration.Core.Safety;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Safety;

public class SafetyEngineTests
{
    [Fact]
    public void RegisterInterlock_AddsRule_Successfully()
    {
        // Arrange
        var engine = new SafetyEngine();
        var rule = new InterlockRule(() => Task.FromResult(true))
        {
            RuleId = "TestRule",
            Description = "Test interlock"
        };

        // Act
        engine.RegisterInterlock(rule);

        // Assert - no exception means success
    }

    [Fact]
    public void RegisterInterlock_ThrowsException_WhenRuleIdAlreadyRegistered()
    {
        // Arrange
        var engine = new SafetyEngine();
        var rule = new InterlockRule(() => Task.FromResult(true))
        {
            RuleId = "TestRule",
            Description = "Test interlock"
        };
        engine.RegisterInterlock(rule);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => engine.RegisterInterlock(rule));
    }

    [Fact]
    public async Task EvaluateInterlocksAsync_ReturnsTrue_WhenAllRulesPass()
    {
        // Arrange
        var engine = new SafetyEngine();
        var rule1 = new InterlockRule(() => Task.FromResult(true))
        {
            RuleId = "Rule1",
            Description = "Always passes"
        };
        var rule2 = new InterlockRule(() => Task.FromResult(true))
        {
            RuleId = "Rule2",
            Description = "Also passes"
        };

        engine.RegisterInterlock(rule1);
        engine.RegisterInterlock(rule2);

        // Act
        var result = await engine.EvaluateInterlocksAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateInterlocksAsync_ReturnsFalse_WhenCriticalRuleFails()
    {
        // Arrange
        var engine = new SafetyEngine();
        var passingRule = new InterlockRule(() => Task.FromResult(true))
        {
            RuleId = "PassingRule",
            Description = "This passes"
        };
        var failingRule = new InterlockRule(() => Task.FromResult(false))
        {
            RuleId = "FailingRule",
            Description = "Critical failure",
            Severity = InterlockSeverity.Critical
        };

        engine.RegisterInterlock(passingRule);
        engine.RegisterInterlock(failingRule);

        // Act
        var result = await engine.EvaluateInterlocksAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetViolationsAsync_ReturnsFailedRules()
    {
        // Arrange
        var engine = new SafetyEngine();
        var passingRule = new InterlockRule(() => Task.FromResult(true))
        {
            RuleId = "PassingRule",
            Description = "This passes"
        };
        var failingRule = new InterlockRule(() => Task.FromResult(false))
        {
            RuleId = "FailingRule",
            Description = "This fails",
            Severity = InterlockSeverity.Critical
        };

        engine.RegisterInterlock(passingRule);
        engine.RegisterInterlock(failingRule);

        // Evaluate first to populate LastResult
        await engine.EvaluateInterlocksAsync();

        // Act
        var violations = await engine.GetViolationsAsync();

        // Assert
        var violationList = violations.ToList();
        Assert.Single(violationList);
        Assert.Equal("FailingRule", violationList[0].RuleId);
    }

    [Fact]
    public async Task TriggerEmergencyStopAsync_SetsActiveFlag_AndCancelsToken()
    {
        // Arrange
        var engine = new SafetyEngine();
        var token = engine.EmergencyStopToken;

        // Act
        await engine.TriggerEmergencyStopAsync("Test emergency stop");

        // Assert
        Assert.True(engine.IsEmergencyStopActive);
        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public async Task ResetEmergencyStopAsync_ClearsActiveFlag_AndCreatesNewToken()
    {
        // Arrange
        var engine = new SafetyEngine();
        await engine.TriggerEmergencyStopAsync("Test emergency stop");
        var oldToken = engine.EmergencyStopToken;

        // Act
        await engine.ResetEmergencyStopAsync();

        // Assert
        Assert.False(engine.IsEmergencyStopActive);
        Assert.NotSame(oldToken, engine.EmergencyStopToken);
        Assert.False(engine.EmergencyStopToken.IsCancellationRequested);
    }

    [Fact]
    public async Task StartInterlockMonitoringAsync_TriggerEmergencyStop_OnCriticalViolation()
    {
        // Arrange
        var engine = new SafetyEngine();
        bool violationCondition = false;

        var rule = new InterlockRule(() => Task.FromResult(!violationCondition))
        {
            RuleId = "MonitoredRule",
            Description = "Temperature safety limit",
            Severity = InterlockSeverity.Critical
        };

        engine.RegisterInterlock(rule);

        using var cts = new CancellationTokenSource();

        // Start monitoring in background
        var monitoringTask = Task.Run(async () =>
        {
            await engine.StartInterlockMonitoringAsync(TimeSpan.FromMilliseconds(50), cts.Token);
        }, cts.Token);

        // Act - simulate violation after a short delay
        await Task.Delay(100);
        violationCondition = true; // Trigger violation

        // Wait for monitoring to detect the violation
        await Task.Delay(150);

        // Assert
        Assert.True(engine.IsEmergencyStopActive);

        // Cleanup
        await cts.CancelAsync();
        try
        {
            await monitoringTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when canceling the monitoring task
        }
    }

    [Fact]
    public void UnregisterInterlock_RemovesRule_Successfully()
    {
        // Arrange
        var engine = new SafetyEngine();
        var rule = new InterlockRule(() => Task.FromResult(true))
        {
            RuleId = "TestRule",
            Description = "Test interlock"
        };
        engine.RegisterInterlock(rule);

        // Act
        engine.UnregisterInterlock("TestRule");

        // Assert - attempting to register the same rule again should succeed
        engine.RegisterInterlock(rule);
    }
}
