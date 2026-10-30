using Helix.Orchestration.Agent.Execution;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Safety;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Execution;

public sealed class AdmissionContextProviderTests
{
    [Fact]
    public async Task CreateAsync_MissingDurableEvidence_IsUnknownAndCannotAuthorize()
    {
        using var safety = new SafetyEngine();
        var provider = new AdmissionContextProvider(safety);

        var context = await provider.CreateAsync(CreateIntent());

        Assert.Equal(CommandCheckStatus.Unknown, context.GetStatus("capability"));
        Assert.Equal(CommandCheckStatus.Unknown, context.GetStatus("safety"));
        Assert.Equal(CommandCheckStatus.Unknown, context.GetStatus("lease"));
        Assert.StartsWith("safety:", context.ContextRevision, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateAsync_StaleState_IsUnknownEvenWhenItPreviouslyPassed()
    {
        using var safety = CreateSafeEngine();
        var source = new FixedStateSource(new AdmissionStateSnapshot(
            CommandCheckStatus.Passed,
            "simulation-policy-v1",
            CommandCheckStatus.Passed,
            CommandCheckStatus.Passed,
            CommandCheckStatus.Passed,
            CommandCheckStatus.Passed,
            CommandCheckStatus.Passed,
            DateTimeOffset.UtcNow - TimeSpan.FromSeconds(3),
            "old"));
        var provider = new AdmissionContextProvider(safety, source);

        var context = await provider.CreateAsync(CreateIntent());

        Assert.Equal(CommandCheckStatus.Unknown, context.GetStatus("capability"));
        Assert.Equal(CommandCheckStatus.Unknown, context.GetStatus("health"));
        Assert.Contains("admission:stale:old", context.ContextRevision, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateAsync_LegacyCapability_IsRejectedWithoutTrustingOtherPassedEvidence()
    {
        using var safety = CreateSafeEngine();
        var source = new FixedStateSource(new AdmissionStateSnapshot(
            CommandCheckStatus.Passed,
            "simulation-policy-v1",
            CommandCheckStatus.Passed,
            CommandCheckStatus.Passed,
            CommandCheckStatus.Passed,
            CommandCheckStatus.Passed,
            CommandCheckStatus.Passed,
            DateTimeOffset.UtcNow,
            "current"));
        var provider = new AdmissionContextProvider(safety, source);

        var context = await provider.CreateAsync(CreateIntent(capability: "SetPumpMode"));

        Assert.Equal(CommandCheckStatus.Failed, context.GetStatus("capability"));
        Assert.Equal(CommandCheckStatus.Passed, context.GetStatus("safety"));
    }

    private static SafetyEngine CreateSafeEngine()
    {
        var safety = new SafetyEngine();
        safety.RegisterInterlock(new InterlockRule(() => Task.FromResult(true))
        {
            RuleId = "safe",
            Description = "Safe"
        });
        return safety;
    }

    private static CommandIntent CreateIntent(string capability = "SetFlow") => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "simulator",
        capability,
        "{\"flowRate\":1.0}",
        "S-1-5-21",
        "Scientist",
        "session",
        "simulation-policy-v1",
        DateTimeOffset.UtcNow);

    private sealed class FixedStateSource(AdmissionStateSnapshot snapshot) : IAdmissionStateSource
    {
        public Task<AdmissionStateSnapshot?> GetAsync(CommandIntent intent, CancellationToken cancellationToken = default) =>
            Task.FromResult<AdmissionStateSnapshot?>(snapshot);
    }
}
