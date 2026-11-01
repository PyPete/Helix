using Helix.Orchestration.Core.ScientificSetup;

namespace Helix.Orchestration.Agent.ScientificSetup;

/// <summary>Scoped facade for immutable frozen setup evidence. It never starts or dispatches an experiment.</summary>
public sealed class FrozenSetupService(IFrozenSetupRepository manifests, IPolicyRevisionProvider policies)
{
    public async Task<FrozenSetupManifest> FreezeAsync(FrozenSetupManifest manifest, string actorSid, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!string.Equals(manifest.FrozenBySid, actorSid, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Frozen setup actor must be authenticated.");

        // This independently rechecks authority against the exact frozen revision and does not resolve latest policy content.
        await policies.ResolveApprovedAsync(manifest.Policy.RevisionId, manifest.Approval, actorSid, "scientific-setup", cancellationToken);
        return await manifests.FreezeAsync(manifest, actorSid, cancellationToken);
    }

    public Task<FrozenSetupManifest?> ReadAsync(Guid manifestId, string actorSid, CancellationToken cancellationToken = default) =>
        manifests.ReadAsync(manifestId, actorSid, cancellationToken);
}
