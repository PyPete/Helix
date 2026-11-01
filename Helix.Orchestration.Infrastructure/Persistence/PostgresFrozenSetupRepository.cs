using System.Text.Json;
using Dapper;
using Helix.Orchestration.Core.ScientificSetup;

namespace Helix.Orchestration.Infrastructure.Persistence;

public sealed class PostgresFrozenSetupRepository(DbConnectionFactory factory) : IFrozenSetupRepository
{
    public async Task<FrozenSetupManifest> FreezeAsync(FrozenSetupManifest manifest, string actorSid, CancellationToken cancellationToken = default)
    {
        manifest.Validate();
        if (!string.Equals(manifest.FrozenBySid, actorSid, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Frozen manifests must be written by their authenticated actor.");
        var payload = JsonSerializer.Serialize(manifest);
        await using var connection = factory.Create();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO scientific_frozen_setup_manifests(manifest_id,apparatus_revision_id,procedure_revision_id,plan_revision_id,policy_revision_id,actor_sid,manifest,frozen_at) VALUES (@ManifestId,@ApparatusRevisionId,@ProcedureRevisionId,@PlanRevisionId,@PolicyRevisionId,@ActorSid,CAST(@Payload AS jsonb),@FrozenAtUtc);",
            new { manifest.ManifestId, ApparatusRevisionId = manifest.Apparatus.RevisionId, ProcedureRevisionId = manifest.Procedure.RevisionId, PlanRevisionId = manifest.Plan.RevisionId, PolicyRevisionId = manifest.Policy.RevisionId, ActorSid = actorSid, Payload = payload, manifest.FrozenAtUtc }, cancellationToken: cancellationToken));
        return manifest;
    }

    public async Task<FrozenSetupManifest?> ReadAsync(Guid manifestId, string actorSid, CancellationToken cancellationToken = default)
    {
        await using var connection = factory.Create();
        var payload = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT manifest::text FROM scientific_frozen_setup_manifests WHERE manifest_id=@manifestId AND actor_sid=@actorSid;",
            new { manifestId, actorSid }, cancellationToken: cancellationToken));
        if (payload is null) return null;
        var manifest = JsonSerializer.Deserialize<FrozenSetupManifest>(payload) ?? throw new InvalidOperationException("Stored frozen manifest is invalid.");
        manifest.Validate();
        return manifest;
    }
}
