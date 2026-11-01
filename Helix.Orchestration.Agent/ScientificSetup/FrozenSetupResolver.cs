using Helix.Orchestration.Core.ScientificSetup;
using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.WSL;
using System.Text.Json;

namespace Helix.Orchestration.Agent.ScientificSetup;

/// <summary>Reconstructs exact stored compilation evidence after a fresh authority check; it never recompiles or dispatches.</summary>
public sealed class FrozenSetupResolver(
    IFrozenSetupRepository manifests,
    IPolicyRevisionProvider policies,
    IScientificSetupRepository scientificSetup)
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

    /// <summary>
    /// Reconstructs the durable compiled definition only when the submitted workflow
    /// is the exact plan instantiation of the frozen source revisions.
    /// </summary>
    public async Task<(FrozenExecutionBinding Binding, RunspaceDefinition Definition)?> ResolveDefinitionAsync(
        Guid manifestId,
        string actorSid,
        WslDocument submittedWorkflow,
        IWslCompiler compiler,
        DeviceRegistry devices,
        CancellationToken cancellationToken = default)
    {
        var binding = await ResolveAsync(manifestId, actorSid, cancellationToken);
        if (binding is null)
            return null;

        var manifest = binding.Manifest;
        var procedureRevision = await scientificSetup.ReadProcedureAsync(
            manifest.Procedure.ResourceId, manifest.Procedure.RevisionId, actorSid, cancellationToken);
        var planRevision = await scientificSetup.ReadPlanAsync(
            manifest.Plan.ResourceId, manifest.Plan.RevisionId, actorSid, cancellationToken);
        if (procedureRevision is null || planRevision is null ||
            procedureRevision.ContentHash != manifest.Procedure.ContentHash ||
            planRevision.ContentHash != manifest.Plan.ContentHash)
            throw new InvalidOperationException("Frozen scientific source revisions are unavailable or do not match the manifest.");

        var procedure = JsonSerializer.Deserialize<ProcedureDefinition>(procedureRevision.CanonicalContent)
            ?? throw new InvalidOperationException("Frozen procedure revision is invalid.");
        var plan = JsonSerializer.Deserialize<ExperimentPlanDefinition>(planRevision.CanonicalContent)
            ?? throw new InvalidOperationException("Frozen plan revision is invalid.");
        if (plan.ProcedureRevisionId != manifest.Procedure.RevisionId)
            throw new InvalidOperationException("Frozen plan does not pin the frozen procedure revision.");

        var expectedWorkflow = plan.Instantiate(procedure);
        if (!JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expectedWorkflow), JsonSerializer.SerializeToElement(submittedWorkflow)))
            throw new InvalidOperationException("Submitted workflow does not match the exact frozen procedure and plan.");

        var compiled = JsonSerializer.Deserialize<RunspaceDefinition>(binding.CompiledDefinition)
            ?? throw new InvalidOperationException("Frozen compiled definition is not a durable runspace definition.");
        var freshCompilation = compiler.Compile(expectedWorkflow, devices);
        if (!freshCompilation.Success || freshCompilation.Definition is null)
            throw new InvalidOperationException("Frozen scientific source no longer passes current admission compilation.");

        var storedHash = compiled.Metadata.TryGetValue("definitionHash", out var storedValue) ? storedValue?.ToString() : null;
        var freshHash = freshCompilation.Definition.Metadata.TryGetValue("definitionHash", out var freshValue) ? freshValue?.ToString() : null;
        if (string.IsNullOrWhiteSpace(storedHash) || !string.Equals(storedHash, freshHash, StringComparison.Ordinal))
            throw new InvalidOperationException("Frozen durable definition does not match the exact scientific source compilation.");

        return (binding, compiled);
    }
}
