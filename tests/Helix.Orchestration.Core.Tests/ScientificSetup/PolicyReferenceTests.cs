using System.Text;
using Helix.Orchestration.Core.ScientificSetup;
using Xunit;

namespace Helix.Orchestration.Core.Tests.ScientificSetup;

public sealed class PolicyReferenceTests
{
    [Fact]
    public void Policy_contract_exposes_independent_content_and_approval_authority()
    {
        var assembly = typeof(ApparatusDefinition).Assembly;

        Assert.NotNull(assembly.GetType("Helix.Orchestration.Core.ScientificSetup.PolicyRevisionEnvelope"));
        Assert.NotNull(assembly.GetType("Helix.Orchestration.Core.ScientificSetup.PolicyApprovalProvenance"));
        Assert.NotNull(assembly.GetType("Helix.Orchestration.Core.ScientificSetup.IPolicyApprovalAuthority"));
        Assert.NotNull(assembly.GetType("Helix.Orchestration.Core.ScientificSetup.IPolicyRevisionProvider"));
        Assert.NotNull(assembly.GetType("Helix.Orchestration.Core.ScientificSetup.FrozenSetupManifest"));
        Assert.NotNull(assembly.GetType("Helix.Orchestration.Core.ScientificSetup.IFrozenSetupRepository"));
    }

    [Fact]
    public void Policy_content_hash_and_approval_snapshot_must_bind_exact_bytes()
    {
        var content = Encoding.UTF8.GetBytes("{\"policy\":\"bounded\"}");
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)).ToLowerInvariant();
        var policy = new PolicyRevisionEnvelope(Guid.NewGuid(), Guid.NewGuid(), ScientificContentCanonicalizer.Schema, content, hash, "creator", "trusted-source", DateTimeOffset.UtcNow);
        policy.Validate();
        var provenance = new PolicyApprovalProvenance(Guid.NewGuid(), policy.RevisionId, policy.ContentHash, policy.Schema, "approver", "authority", "scientist", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var reference = new FrozenContentReference(policy.RevisionId, policy.PolicyId, policy.Schema, policy.ContentHash);

        var artifact = new byte[] { 1 };
        var artifactHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(artifact)).ToLowerInvariant();
        var manifest = new FrozenSetupManifest(Guid.NewGuid(), reference with { RevisionId = Guid.NewGuid() }, reference with { RevisionId = Guid.NewGuid() }, reference with { RevisionId = Guid.NewGuid() }, reference, provenance, "compiled/1", "compiler", artifact, artifactHash, DateTimeOffset.UtcNow, "scientist", DateTimeOffset.UtcNow);
        manifest.Validate();

        Assert.Throws<ArgumentException>(() => (policy with { ContentHash = "not-a-hash" }).Validate());
        Assert.Throws<ArgumentException>(() => (manifest with { Approval = provenance with { PolicyContentHash = "different" } }).Validate());
    }
}
