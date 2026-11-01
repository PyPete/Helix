using System.Security.Cryptography;
using System.Text;
using Helix.Orchestration.Core.ScientificSetup;
using Helix.Orchestration.Infrastructure.Persistence;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

 [Collection(nameof(PostgresRepositoriesTestsCollection))]
public sealed class PolicyReferenceTests(PostgresFixture fixture)
{
    [Fact]
    public void Immutable_policy_and_frozen_manifest_persistence_are_available()
    {
        var assembly = typeof(DbConnectionFactory).Assembly;

        Assert.NotNull(assembly.GetType("Helix.Orchestration.Infrastructure.Persistence.PostgresPolicyRevisionProvider"));
        Assert.NotNull(assembly.GetType("Helix.Orchestration.Infrastructure.Persistence.PostgresFrozenSetupRepository"));
    }

    [Fact]
    public void Frozen_repository_implements_typed_repository_port()
    {
        Assert.True(typeof(IFrozenSetupRepository).IsAssignableFrom(typeof(PostgresFrozenSetupRepository)));
        Assert.True(typeof(IPolicyRevisionProvider).IsAssignableFrom(typeof(PostgresPolicyRevisionProvider)));
    }

    [Fact]
    public async Task Policy_content_round_trips_without_manufacturing_approval()
    {
        Assert.True(fixture.IsAvailable, "PostgreSQL is required for policy persistence tests.");
        var content = Encoding.UTF8.GetBytes("{\"content\":{\"stopRules\":[]},\"schema\":\"scientific-setup/1\"}");
        var policy = new PolicyRevisionEnvelope(Guid.NewGuid(), Guid.NewGuid(), ScientificContentCanonicalizer.Schema, content, Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant(), "policy-scientist", "test-fixture", DateTimeOffset.UtcNow);
        var provider = new PostgresPolicyRevisionProvider(fixture.ConnectionFactory!, new RejectingAuthority());

        await provider.RegisterAsync(policy);
        var reconstructed = await provider.ResolveAsync(policy.RevisionId, "policy-scientist");
        var provenance = new PolicyApprovalProvenance(Guid.NewGuid(), policy.RevisionId, policy.ContentHash, policy.Schema, "approver", "test-authority", "scientist", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));

        Assert.NotNull(reconstructed);
        Assert.Equal(policy.CanonicalContent, reconstructed!.CanonicalContent);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => provider.ResolveApprovedAsync(policy.RevisionId, provenance, "policy-scientist", "scientist"));
    }

    private sealed class RejectingAuthority : IPolicyApprovalAuthority
    {
        public Task<PolicyApprovalValidation> VerifyAsync(PolicyApprovalProvenance provenance, PolicyRevisionEnvelope policy, string actorSid, string scope, DateTimeOffset evaluatedAtUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(PolicyApprovalValidation.Rejected("No approval was issued."));
    }
}
