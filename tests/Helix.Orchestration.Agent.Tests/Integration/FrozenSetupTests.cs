using Helix.Orchestration.Agent.ScientificSetup;
using Helix.Orchestration.Core.ScientificSetup;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Integration;

public sealed class FrozenSetupTests
{
    [Fact]
    public async Task Production_authority_is_unavailable_and_fail_closed()
    {
        var authority = new UnavailablePolicyApprovalAuthority();
        var result = await authority.VerifyAsync(
            new PolicyApprovalProvenance(Guid.NewGuid(), Guid.NewGuid(), "hash", ScientificContentCanonicalizer.Schema, "scientist", "authority", "scientific-setup", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(1)),
            new PolicyRevisionEnvelope(Guid.NewGuid(), Guid.NewGuid(), ScientificContentCanonicalizer.Schema, [1], "00", "scientist", "test", DateTimeOffset.UtcNow),
            "scientist", "scientific-setup", DateTimeOffset.UtcNow);

        Assert.True(result.IsUnavailable);
        Assert.False(result.IsApproved);
    }
}
