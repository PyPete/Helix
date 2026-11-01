using System.Security.Cryptography;
using System.Text;
using Helix.Orchestration.Core.ScientificSetup;
using Helix.Orchestration.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

/// <summary>Exercises a real isolated PostgreSQL round trip for immutable frozen evidence.</summary>
[Collection(nameof(PostgresRepositoriesTestsCollection))]
public sealed class FrozenReferenceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Trusted_policy_freeze_survives_later_publication_and_repository_reconstruction()
    {
        Assert.True(fixture.IsAvailable, "PostgreSQL is required for immutable frozen-reference tests.");

        const string actor = "frozen-reference-scientist";
        var policyProvider = new PostgresPolicyRevisionProvider(fixture.ConnectionFactory!, new TrustedTestAuthority());
        var originalPolicy = NewPolicy(actor, "{\"content\":{\"maxIterations\":1},\"schema\":\"scientific-setup/1\"}");
        await policyProvider.RegisterAsync(originalPolicy);

        var approval = new PolicyApprovalProvenance(
            Guid.NewGuid(), originalPolicy.RevisionId, originalPolicy.ContentHash, originalPolicy.Schema,
            "test-approver", "trusted-test-authority", "scientific-setup", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        await policyProvider.ResolveApprovedAsync(originalPolicy.RevisionId, approval, actor, "scientific-setup");

        var compiled = Encoding.UTF8.GetBytes("{\"compiled\":\"original\"}");
        var (apparatus, procedure, plan) = await SeedPublishedReferencesAsync(actor);
        var manifest = new FrozenSetupManifest(
            Guid.NewGuid(), apparatus, procedure, plan,
            new FrozenContentReference(originalPolicy.RevisionId, originalPolicy.PolicyId, originalPolicy.Schema, originalPolicy.ContentHash),
            approval, "compiled/1", "test-compiler", compiled, Hash(compiled), DateTimeOffset.UtcNow, actor, DateTimeOffset.UtcNow);
        var repository = new PostgresFrozenSetupRepository(fixture.ConnectionFactory!);
        await repository.FreezeAsync(manifest, actor);

        // A later immutable policy publication cannot replace the recorded policy reference.
        var laterPolicy = NewPolicy(actor, "{\"content\":{\"maxIterations\":2},\"schema\":\"scientific-setup/1\"}");
        await policyProvider.RegisterAsync(laterPolicy);

        // Reconstructing the repository represents a process/service restart.
        var reconstructed = new PostgresFrozenSetupRepository(fixture.ConnectionFactory!);
        var stored = await reconstructed.ReadAsync(manifest.ManifestId, actor);

        Assert.NotNull(stored);
        Assert.Equal(manifest.Apparatus, stored!.Apparatus);
        Assert.Equal(manifest.Procedure, stored.Procedure);
        Assert.Equal(manifest.Plan, stored.Plan);
        Assert.Equal(originalPolicy.RevisionId, stored.Policy.RevisionId);
        Assert.Equal(originalPolicy.ContentHash, stored.Policy.ContentHash);
        Assert.NotEqual(laterPolicy.RevisionId, stored.Policy.RevisionId);
        Assert.Equal(compiled, stored.CompiledDefinition);
        Assert.Equal(Hash(compiled), stored.CompiledDefinitionHash);
        Assert.Equal(manifest.CompiledAtUtc, stored.CompiledAtUtc);
        Assert.Equal(approval, stored.Approval);
    }

    [Fact]
    public async Task Frozen_manifest_is_scoped_to_its_original_actor()
    {
        Assert.True(fixture.IsAvailable, "PostgreSQL is required for immutable frozen-reference tests.");
        var actor = $"frozen-actor-{Guid.NewGuid():N}";
        var policy = NewPolicy(actor, "{\"content\":{\"stopRules\":[]},\"schema\":\"scientific-setup/1\"}");
        await new PostgresPolicyRevisionProvider(fixture.ConnectionFactory!, new TrustedTestAuthority()).RegisterAsync(policy);
        var approval = new PolicyApprovalProvenance(Guid.NewGuid(), policy.RevisionId, policy.ContentHash, policy.Schema, "test", "trusted-test-authority", "scientific-setup", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var bytes = Encoding.UTF8.GetBytes("immutable artifact");
        var (apparatus, procedure, plan) = await SeedPublishedReferencesAsync(actor);
        var manifest = new FrozenSetupManifest(Guid.NewGuid(), apparatus, procedure, plan, new FrozenContentReference(policy.RevisionId, policy.PolicyId, policy.Schema, policy.ContentHash), approval, "compiled/1", "test", bytes, Hash(bytes), DateTimeOffset.UtcNow, actor, DateTimeOffset.UtcNow);
        var repository = new PostgresFrozenSetupRepository(fixture.ConnectionFactory!);

        await repository.FreezeAsync(manifest, actor);

        Assert.Null(await repository.ReadAsync(manifest.ManifestId, "different-actor"));
    }

    private static PolicyRevisionEnvelope NewPolicy(string actor, string json)
    {
        var content = Encoding.UTF8.GetBytes(json);
        return new PolicyRevisionEnvelope(Guid.NewGuid(), Guid.NewGuid(), ScientificContentCanonicalizer.Schema, content, Hash(content), actor, "trusted-test-fixture", DateTimeOffset.UtcNow);
    }

    private async Task<(FrozenContentReference Apparatus, FrozenContentReference Procedure, FrozenContentReference Plan)> SeedPublishedReferencesAsync(string actor)
    {
        var apparatusId = Guid.NewGuid();
        var apparatusRevisionId = Guid.NewGuid();
        var procedureId = Guid.NewGuid();
        var procedureRevisionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var planRevisionId = Guid.NewGuid();
        var recordedAt = DateTimeOffset.UtcNow;
        await using var connection = fixture.ConnectionFactory!.Create();
        await connection.OpenAsync();
        const string sql = """
            INSERT INTO scientific_apparatus(apparatus_id, actor_sid, name, draft_content) VALUES (@apparatusId, @actor, 'fixture apparatus', '{}'::jsonb);
            INSERT INTO scientific_apparatus_revisions(revision_id, apparatus_id, revision_name, content_hash, canonical_content, actor_sid, recorded_at) VALUES (@apparatusRevisionId, @apparatusId, 'fixture', @apparatusHash, '{}'::jsonb, @actor, @recordedAt);
            INSERT INTO scientific_procedures(procedure_id, actor_sid, name, draft_content) VALUES (@procedureId, @actor, 'fixture procedure', '{}'::jsonb);
            INSERT INTO scientific_procedure_revisions(revision_id, procedure_id, revision_name, content_hash, canonical_content, actor_sid, recorded_at) VALUES (@procedureRevisionId, @procedureId, 'fixture', @procedureHash, '{}'::jsonb, @actor, @recordedAt);
            INSERT INTO scientific_experiment_plans(plan_id, actor_sid, name, draft_content) VALUES (@planId, @actor, 'fixture plan', '{}'::jsonb);
            INSERT INTO scientific_experiment_plan_revisions(revision_id, plan_id, procedure_revision_id, revision_name, content_hash, canonical_content, actor_sid, recorded_at) VALUES (@planRevisionId, @planId, @procedureRevisionId, 'fixture', @planHash, '{}'::jsonb, @actor, @recordedAt);
            """;
        var apparatusHash = Hash(Encoding.UTF8.GetBytes(apparatusRevisionId.ToString()));
        var procedureHash = Hash(Encoding.UTF8.GetBytes(procedureRevisionId.ToString()));
        var planHash = Hash(Encoding.UTF8.GetBytes(planRevisionId.ToString()));
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("apparatusId", apparatusId);
        command.Parameters.AddWithValue("apparatusRevisionId", apparatusRevisionId);
        command.Parameters.AddWithValue("procedureId", procedureId);
        command.Parameters.AddWithValue("procedureRevisionId", procedureRevisionId);
        command.Parameters.AddWithValue("planId", planId);
        command.Parameters.AddWithValue("planRevisionId", planRevisionId);
        command.Parameters.AddWithValue("actor", actor);
        command.Parameters.AddWithValue("recordedAt", recordedAt);
        command.Parameters.AddWithValue("apparatusHash", apparatusHash);
        command.Parameters.AddWithValue("procedureHash", procedureHash);
        command.Parameters.AddWithValue("planHash", planHash);
        await command.ExecuteNonQueryAsync();
        return (
            new FrozenContentReference(apparatusRevisionId, apparatusId, "apparatus/1", apparatusHash),
            new FrozenContentReference(procedureRevisionId, procedureId, "procedure/1", procedureHash),
            new FrozenContentReference(planRevisionId, planId, "plan/1", planHash));
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed class TrustedTestAuthority : IPolicyApprovalAuthority
    {
        public Task<PolicyApprovalValidation> VerifyAsync(PolicyApprovalProvenance provenance, PolicyRevisionEnvelope policy, string actorSid, string scope, DateTimeOffset evaluatedAtUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(provenance.AuthorityId == "trusted-test-authority" && provenance.Scope == scope
                ? PolicyApprovalValidation.Approved()
                : PolicyApprovalValidation.Rejected("Trusted test provenance did not match."));
    }
}
