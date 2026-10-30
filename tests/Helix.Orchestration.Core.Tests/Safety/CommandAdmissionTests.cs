using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Safety;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Safety;

public sealed class CommandAdmissionTests
{
    private readonly CommandAdmissionEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_SetFlowWithAllChecksPassed_AllowsAndRecordsSevenChecks()
    {
        var result = _evaluator.Evaluate(CreateContext(CommandCheckStatus.Passed));

        Assert.Equal(CommandAdmissionState.Allowed, result.State);
        Assert.Equal(7, result.Checks.Count);
        Assert.All(result.Checks, check => Assert.Equal(CommandCheckStatus.Passed, check.Status));
    }

    [Fact]
    public void Evaluate_SetFlowWithRejectedCapability_Rejects()
    {
        var statuses = CreateStatuses(CommandCheckStatus.Passed);
        statuses["capability"] = CommandCheckStatus.Failed;

        var result = _evaluator.Evaluate(new CommandAdmissionContext(statuses, "revision-1"));

        Assert.Equal(CommandAdmissionState.Rejected, result.State);
        Assert.Contains(result.Checks, check => check.Check == "capability" && check.Code == "capability.failed");
    }

    [Fact]
    public void Evaluate_UnknownHealth_Holds()
    {
        var statuses = CreateStatuses(CommandCheckStatus.Passed);
        statuses["health"] = CommandCheckStatus.Unknown;

        var result = _evaluator.Evaluate(new CommandAdmissionContext(statuses, "revision-1"));

        Assert.Equal(CommandAdmissionState.Held, result.State);
        Assert.Contains(result.Checks, check => check.Check == "health" && check.Code == "health.unknown");
    }

    [Fact]
    public void Evaluate_MissingState_HoldsAndStillRecordsEveryRequiredCheck()
    {
        var statuses = CreateStatuses(CommandCheckStatus.Passed);
        statuses.Remove("lease");

        var result = _evaluator.Evaluate(new CommandAdmissionContext(statuses, "revision-1"));

        Assert.Equal(CommandAdmissionState.Held, result.State);
        Assert.Equal(7, result.Checks.Count);
        Assert.Contains(result.Checks, check => check.Check == "lease" && check.Status == CommandCheckStatus.Unknown);
    }

    private static CommandAdmissionContext CreateContext(CommandCheckStatus status) =>
        new(CreateStatuses(status), "revision-1");

    private static Dictionary<string, CommandCheckStatus> CreateStatuses(CommandCheckStatus status) =>
        new(StringComparer.Ordinal)
        {
            ["capability"] = status,
            ["safety"] = status,
            ["lease"] = status,
            ["resource"] = status,
            ["budget"] = status,
            ["termination"] = status,
            ["health"] = status
        };
}
