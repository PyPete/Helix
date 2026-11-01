using Helix.Orchestration.Core.ScientificSetup;

namespace Helix.Orchestration.Agent.ScientificSetup;

/// <summary>Reconstructs exact stored compilation evidence after a fresh authority check; it never recompiles or dispatches.</summary>
public sealed class FrozenSetupResolver(IFrozenSetupRepository manifests, IPolicyRevisionProvider policies)
{
    public async Task<FrozenExecutionBinding?> ResolveAsync(Guid manifestId, string actorSid, CancellationToken cancellationToken = default)
    {
        var manifest = await manifests.ReadAsync(manifestId, actorSid, cancellationToken);
        if (manifest is null)
            return null;

        manifest.Validate();
        await policies.ResolveApprovedAsync(manifest.Policy.RevisionId, manifest.Approval, actorSid, "scientific-setup", cancellationToken);
        var binding = new FrozenExecutionBinding(manifest, manifest.CompiledDefinition.ToArray());
        binding.Validate();
        return binding;
    }
}
