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

    [Fact]
    public async Task Trusted_test_authority_accepts_only_bound_unexpired_provenance()
    {
        var revisionId = Guid.NewGuid();
        var content = new byte[] { 1, 2, 3 };
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)).ToLowerInvariant();
        var policy = new PolicyRevisionEnvelope(revisionId, Guid.NewGuid(), ScientificContentCanonicalizer.Schema, content, hash, "scientist", "test", DateTimeOffset.UtcNow);
        var provenance = new PolicyApprovalProvenance(Guid.NewGuid(), revisionId, hash, ScientificContentCanonicalizer.Schema, "approver", "trusted-test-authority", "scientific-setup", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(1));

        var accepted = await new TrustedTestAuthority().VerifyAsync(provenance, policy, "scientist", "scientific-setup", DateTimeOffset.UtcNow);
        var rejected = await new TrustedTestAuthority().VerifyAsync(provenance with { PolicyContentHash = "tampered" }, policy, "scientist", "scientific-setup", DateTimeOffset.UtcNow);

        Assert.True(accepted.IsApproved);
        Assert.False(rejected.IsApproved);
    }

    private sealed class TrustedTestAuthority : IPolicyApprovalAuthority
    {
        public Task<PolicyApprovalValidation> VerifyAsync(PolicyApprovalProvenance provenance, PolicyRevisionEnvelope policy, string actorSid, string scope, DateTimeOffset evaluatedAtUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(provenance.AuthorityId == "trusted-test-authority" && provenance.Scope == "scientific-setup" &&
                provenance.PolicyRevisionId == policy.RevisionId && provenance.PolicyContentHash == policy.ContentHash &&
                provenance.PolicySchema == policy.Schema && provenance.ExpiresAtUtc > evaluatedAtUtc
                ? PolicyApprovalValidation.Approved()
                : PolicyApprovalValidation.Rejected("Untrusted test provenance."));
    }
}
