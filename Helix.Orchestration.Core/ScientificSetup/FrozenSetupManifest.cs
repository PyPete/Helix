namespace Helix.Orchestration.Core.ScientificSetup;

/// <summary>An immutable, exact content reference. It never means "latest".</summary>
public sealed record FrozenContentReference(Guid RevisionId, Guid ResourceId, string Schema, string ContentHash)
{
    public void Validate()
    {
        if (RevisionId == Guid.Empty || ResourceId == Guid.Empty || string.IsNullOrWhiteSpace(Schema) || string.IsNullOrWhiteSpace(ContentHash))
            throw new ArgumentException("Frozen content references require identity, schema, and hash.");
    }
}

/// <summary>Durable evidence captured at freeze time; it does not authorize dispatch.</summary>
public sealed record FrozenSetupManifest(
    Guid ManifestId,
    FrozenContentReference Apparatus,
    FrozenContentReference Procedure,
    FrozenContentReference Plan,
    FrozenContentReference Policy,
    PolicyApprovalProvenance Approval,
    string CompiledDefinitionSchema,
    string CompilerIdentity,
    byte[] CompiledDefinition,
    string CompiledDefinitionHash,
    DateTimeOffset CompiledAtUtc,
    string FrozenBySid,
    DateTimeOffset FrozenAtUtc)
{
    public void Validate()
    {
        if (ManifestId == Guid.Empty || string.IsNullOrWhiteSpace(FrozenBySid) || CompiledDefinition.Length == 0 ||
            string.IsNullOrWhiteSpace(CompiledDefinitionSchema) || string.IsNullOrWhiteSpace(CompilerIdentity) || string.IsNullOrWhiteSpace(CompiledDefinitionHash))
            throw new ArgumentException("Frozen manifests require immutable compilation evidence and an actor.");
        Apparatus.Validate(); Procedure.Validate(); Plan.Validate(); Policy.Validate();
        if (Approval.PolicyRevisionId != Policy.RevisionId || !string.Equals(Approval.PolicyContentHash, Policy.ContentHash, StringComparison.Ordinal) ||
            !string.Equals(Approval.PolicySchema, Policy.Schema, StringComparison.Ordinal))
            throw new ArgumentException("Approval provenance must bind the exact frozen policy.");
        var actualHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(CompiledDefinition)).ToLowerInvariant();
        if (!string.Equals(actualHash, CompiledDefinitionHash, StringComparison.Ordinal))
            throw new ArgumentException("Frozen compiled-definition hash does not match its original bytes.");
    }
}

/// <summary>Typed reconstruction supplied to a later execution boundary after fresh approval checks.</summary>
public sealed record FrozenExecutionBinding(FrozenSetupManifest Manifest, byte[] CompiledDefinition)
{
    public void Validate() => Manifest.Validate();
}
