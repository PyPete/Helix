using Helix.Orchestration.Agent.ScientificSetup;
using Helix.Orchestration.Agent.Experiments;
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

    [Fact]
    public void Canonical_definition_exposes_optional_typed_frozen_binding()
    {
        var binding = typeof(CanonicalExperimentDefinition).GetProperty("FrozenExecutionBinding");

        Assert.NotNull(binding);
        Assert.Equal(typeof(FrozenExecutionBinding), binding.PropertyType);
    }

    [Fact]
    public async Task Resolver_rejects_expired_authority_before_returning_a_binding()
    {
        var manifest = CreateManifest();
        var resolver = new FrozenSetupResolver(
            new TestManifestRepository(manifest),
            new RejectingPolicyProvider(),
            null!);

        await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveAsync(manifest.ManifestId, "scientist"));
    }

    private static FrozenSetupManifest CreateManifest()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
        var policy = new FrozenContentReference(Guid.NewGuid(), Guid.NewGuid(), ScientificContentCanonicalizer.Schema, "policy-hash");
        return new FrozenSetupManifest(
            Guid.NewGuid(),
            new FrozenContentReference(Guid.NewGuid(), Guid.NewGuid(), ScientificContentCanonicalizer.Schema, "apparatus-hash"),
            new FrozenContentReference(Guid.NewGuid(), Guid.NewGuid(), ScientificContentCanonicalizer.Schema, "procedure-hash"),
            new FrozenContentReference(Guid.NewGuid(), Guid.NewGuid(), ScientificContentCanonicalizer.Schema, "plan-hash"),
            policy,
            new PolicyApprovalProvenance(Guid.NewGuid(), policy.RevisionId, policy.ContentHash, policy.Schema, "approver", "trusted-test-authority", "scientific-setup", DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(-1)),
            "runspace-definition/1", "test-compiler", bytes, hash, DateTimeOffset.UtcNow.AddHours(-2), "scientist", DateTimeOffset.UtcNow.AddHours(-2));
    }

    private sealed class TestManifestRepository(FrozenSetupManifest manifest) : IFrozenSetupRepository
    {
        public Task<FrozenSetupManifest> FreezeAsync(FrozenSetupManifest manifest, string actorSid, CancellationToken cancellationToken = default) => Task.FromResult(manifest);
        public Task<FrozenSetupManifest?> ReadAsync(Guid manifestId, string actorSid, CancellationToken cancellationToken = default) => Task.FromResult<FrozenSetupManifest?>(manifestId == manifest.ManifestId ? manifest : null);
    }

    private sealed class RejectingPolicyProvider : IPolicyRevisionProvider
    {
        public Task<PolicyRevisionEnvelope> RegisterAsync(PolicyRevisionEnvelope policy, CancellationToken cancellationToken = default) => Task.FromResult(policy);
        public Task<PolicyRevisionEnvelope?> ResolveAsync(Guid revisionId, string actorSid, CancellationToken cancellationToken = default) => Task.FromResult<PolicyRevisionEnvelope?>(null);
        public Task<PolicyRevisionEnvelope> ResolveApprovedAsync(Guid revisionId, PolicyApprovalProvenance provenance, string actorSid, string scope, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Approval evidence is expired.");
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
