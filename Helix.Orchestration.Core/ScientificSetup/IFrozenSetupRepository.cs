namespace Helix.Orchestration.Core.ScientificSetup;

/// <summary>Atomic persistence for immutable manifest evidence. This port cannot start execution.</summary>
public interface IFrozenSetupRepository
{
    Task<FrozenSetupManifest> FreezeAsync(FrozenSetupManifest manifest, string actorSid, CancellationToken cancellationToken = default);
    Task<FrozenSetupManifest?> ReadAsync(Guid manifestId, string actorSid, CancellationToken cancellationToken = default);
}
