using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Safety;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Safety;

public sealed class CommandAdmissionEvaluatorTruthTableTests
{
    private static readonly string[] RequiredChecks =
    [
        "capability", "safety", "lease", "resource", "budget", "termination", "health"
    ];

    [Fact]
    public void Evaluate_AllChecksPassed_AllowsWithStableTimestampAndRevision()
    {
        var occurredAtUtc = new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
        var evaluator = new CommandAdmissionEvaluator(new FixedTimeProvider(occurredAtUtc));
        var context = CreateContext(CommandCheckStatus.Passed);

        var result = evaluator.Evaluate(context);

        Assert.Equal(CommandAdmissionState.Allowed, result.State);
        Assert.Equal(RequiredChecks, result.Checks.Select(check => check.Check));
        Assert.All(result.Checks, check =>
        {
            Assert.Equal(CommandCheckStatus.Passed, check.Status);
            Assert.Equal(occurredAtUtc, check.OccurredAtUtc);
            Assert.Equal("revision-7", check.ContextRevision);
            Assert.Equal($"{check.Check}.passed", check.Code);
        });
    }

    [Fact]
    public void Evaluate_KnownInvalidCapability_RejectsWhileKeepingEveryDiagnostic()
    {
        var statuses = CreateStatuses(CommandCheckStatus.Passed);
        statuses["capability"] = CommandCheckStatus.Failed;

        var result = new CommandAdmissionEvaluator().Evaluate(new CommandAdmissionContext(statuses, "revision-8"));

        Assert.Equal(CommandAdmissionState.Rejected, result.State);
        Assert.Equal(7, result.Checks.Count);
        Assert.Contains(result.Checks, check => check is
        {
            Check: "capability",
            Status: CommandCheckStatus.Failed,
            Code: "capability.failed"
        });
    }

    [Theory]
    [InlineData("safety")]
    [InlineData("lease")]
    [InlineData("resource")]
    [InlineData("budget")]
    [InlineData("termination")]
    [InlineData("health")]
    public void Evaluate_UnknownRequiredState_Holds(string unknownCheck)
    {
        var statuses = CreateStatuses(CommandCheckStatus.Passed);
        statuses[unknownCheck] = CommandCheckStatus.Unknown;

        var result = new CommandAdmissionEvaluator().Evaluate(new CommandAdmissionContext(statuses, "revision-9"));

        Assert.Equal(CommandAdmissionState.Held, result.State);
        Assert.Contains(result.Checks, check => check is
        {
            Check: var check,
            Status: CommandCheckStatus.Unknown,
            Code: var code
        } && check == unknownCheck && code == $"{unknownCheck}.unknown");
    }

    [Fact]
    public void Evaluate_UnsafeStateAndOrdinaryInvalidInput_HoldsForReviewAndRetainsFailure()
    {
        var statuses = CreateStatuses(CommandCheckStatus.Passed);
        statuses["capability"] = CommandCheckStatus.Failed;
        statuses["safety"] = CommandCheckStatus.Unknown;

        var result = new CommandAdmissionEvaluator().Evaluate(new CommandAdmissionContext(statuses, "revision-10"));

        Assert.Equal(CommandAdmissionState.Held, result.State);
        Assert.Contains(result.Checks, check => check is { Check: "capability", Status: CommandCheckStatus.Failed });
        Assert.Contains(result.Checks, check => check is { Check: "safety", Status: CommandCheckStatus.Unknown });
    }

    [Fact]
    public void Evaluate_MissingCheckStates_HoldsInsteadOfGrantingPermission()
    {
        var result = new CommandAdmissionEvaluator().Evaluate(new CommandAdmissionContext(
            new Dictionary<string, CommandCheckStatus>(),
            "revision-11"));

        Assert.Equal(CommandAdmissionState.Held, result.State);
        Assert.All(result.Checks, check => Assert.Equal(CommandCheckStatus.Unknown, check.Status));
    }

    private static CommandAdmissionContext CreateContext(CommandCheckStatus status) =>
        new(CreateStatuses(status), "revision-7");

    private static Dictionary<string, CommandCheckStatus> CreateStatuses(CommandCheckStatus status) =>
        RequiredChecks.ToDictionary(check => check, _ => status, StringComparer.Ordinal);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
