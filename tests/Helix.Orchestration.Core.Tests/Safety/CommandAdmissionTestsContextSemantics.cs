using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Safety;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Safety;

public sealed class CommandAdmissionTestsContextSemantics
{
    [Theory]
    [InlineData("safety")]
    [InlineData("lease")]
    [InlineData("health")]
    public void Evaluate_FailedPhysicalBoundary_HoldsInsteadOfRejecting(string check)
    {
        var statuses = PassingStatuses();
        statuses[check] = CommandCheckStatus.Failed;

        var result = new CommandAdmissionEvaluator().Evaluate(new CommandAdmissionContext(statuses, "revision-1"));

        Assert.Equal(CommandAdmissionState.Held, result.State);
        Assert.Equal($"{check}.failed", Assert.Single(result.Checks, evidence => evidence.Check == check).Code);
    }

    [Theory]
    [InlineData("capability")]
    [InlineData("resource")]
    [InlineData("budget")]
    [InlineData("termination")]
    public void Evaluate_KnownOrdinaryFailure_Rejects(string check)
    {
        var statuses = PassingStatuses();
        statuses[check] = CommandCheckStatus.Failed;

        var result = new CommandAdmissionEvaluator().Evaluate(new CommandAdmissionContext(statuses, "revision-2"));

        Assert.Equal(CommandAdmissionState.Rejected, result.State);
        Assert.Equal(7, result.Checks.Count);
    }

    [Fact]
    public void Evaluate_UnknownPhysicalBoundaryAndInvalidCapability_RetainsAllEvidenceAndHolds()
    {
        var statuses = PassingStatuses();
        statuses["capability"] = CommandCheckStatus.Failed;
        statuses["health"] = CommandCheckStatus.Unknown;

        var result = new CommandAdmissionEvaluator().Evaluate(new CommandAdmissionContext(statuses, "revision-3"));

        Assert.Equal(CommandAdmissionState.Held, result.State);
        Assert.Equal(7, result.Checks.Count);
        Assert.Contains(result.Checks, evidence => evidence.Code == "capability.failed");
        Assert.Contains(result.Checks, evidence => evidence.Code == "health.unknown");
        Assert.All(result.Checks, evidence => Assert.Equal("revision-3", evidence.ContextRevision));
    }

    private static Dictionary<string, CommandCheckStatus> PassingStatuses() => new(StringComparer.Ordinal)
    {
        ["capability"] = CommandCheckStatus.Passed,
        ["safety"] = CommandCheckStatus.Passed,
        ["lease"] = CommandCheckStatus.Passed,
        ["resource"] = CommandCheckStatus.Passed,
        ["budget"] = CommandCheckStatus.Passed,
        ["termination"] = CommandCheckStatus.Passed,
        ["health"] = CommandCheckStatus.Passed
    };
}
