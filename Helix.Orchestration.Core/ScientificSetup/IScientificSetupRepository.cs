namespace Helix.Orchestration.Core.ScientificSetup;
public interface IScientificSetupRepository
{
    Task<(ApparatusDefinition Apparatus, long Token, ScientificOperationReceipt Receipt)> CreateAsync(ApparatusDefinition apparatus, string actorSid, Guid operationId, CancellationToken ct = default);
    Task<(ScientificRevision Revision, ScientificOperationReceipt Receipt)> PublishAsync(Guid apparatusId, long expectedToken, string name, string actorSid, Guid operationId, CancellationToken ct = default);
    Task<ScientificRevision?> ReadAsync(Guid apparatusId, Guid revisionId, string actorSid, CancellationToken ct = default);
    Task<ScientificOperationReceipt?> OperationAsync(Guid operationId, string kind, string actorSid, CancellationToken ct = default);
}
