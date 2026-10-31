namespace Helix.Orchestration.Core.ScientificSetup;

public sealed record ScientificContentSummary(Guid ResourceId, string Name, Guid? RevisionId);
public interface IScientificSetupRepository
{
    Task<(ApparatusDefinition Apparatus, long Token, ScientificOperationReceipt Receipt)> CreateAsync(ApparatusDefinition apparatus, string actorSid, Guid operationId, CancellationToken ct = default);
    Task<(ApparatusDefinition Apparatus, long Token, ScientificOperationReceipt Receipt)> SaveDraftAsync(ApparatusDefinition apparatus, long expectedToken, string actorSid, Guid operationId, CancellationToken ct = default);
    Task<(ApparatusDefinition Apparatus, long Token)> ReadDraftAsync(Guid apparatusId, string actorSid, CancellationToken ct = default);
    Task<(ApparatusDefinition Apparatus, long Token, ScientificOperationReceipt Receipt)> AdoptRevisionAsync(Guid apparatusId, Guid revisionId, long expectedToken, string actorSid, Guid operationId, CancellationToken ct = default);
    Task<(ScientificRevision Revision, ScientificOperationReceipt Receipt)> PublishAsync(Guid apparatusId, Guid? expectedRevisionId, string name, string actorSid, Guid operationId, CancellationToken ct = default);
    Task<IReadOnlyList<ScientificApparatusSummary>> ListAsync(string actorSid, CancellationToken ct = default);
    Task<ScientificApparatusSummary?> ReadLatestAsync(Guid apparatusId, string actorSid, CancellationToken ct = default);
    Task<IReadOnlyList<ScientificOperationReceipt>> HistoryAsync(Guid apparatusId, string actorSid, CancellationToken ct = default);
    Task<ScientificRevision?> ReadAsync(Guid apparatusId, Guid revisionId, string actorSid, CancellationToken ct = default);
    Task<ScientificOperationReceipt?> OperationAsync(Guid operationId, string actorSid, CancellationToken ct = default);

    Task<(ProcedureDefinition Procedure, long Token, ScientificOperationReceipt Receipt)> CreateProcedureAsync(ProcedureDefinition procedure, string actorSid, Guid operationId, CancellationToken ct = default);
    Task<(ExperimentPlanDefinition Plan, long Token, ScientificOperationReceipt Receipt)> CreatePlanAsync(ExperimentPlanDefinition plan, ProcedureDefinition procedure, string actorSid, Guid operationId, CancellationToken ct = default);
    Task<(ProcedureDefinition Procedure, long Token, ScientificOperationReceipt Receipt)> DeriveProcedureAsync(Guid sourceRevisionId, string name, string actorSid, Guid operationId, CancellationToken ct = default) => throw new NotSupportedException();
    Task<(ExperimentPlanDefinition Plan, long Token, ScientificOperationReceipt Receipt)> AdoptProcedureAsync(Guid planId, Guid procedureRevisionId, long expectedToken, string actorSid, Guid operationId, CancellationToken ct = default) => throw new NotSupportedException();
    Task<(ExperimentPlanDefinition Plan, long Token)> ReadPlanDraftAsync(Guid planId, string actorSid, CancellationToken ct = default);
    Task<(ScientificRevision Revision, ScientificOperationReceipt Receipt)> PublishProcedureAsync(Guid procedureId, Guid? expectedRevisionId, string name, string actorSid, Guid operationId, CancellationToken ct = default);
    Task<(ScientificRevision Revision, ScientificOperationReceipt Receipt)> PublishPlanAsync(Guid planId, Guid? expectedRevisionId, string name, string actorSid, Guid operationId, CancellationToken ct = default);

    Task<(ProcedureDefinition Procedure, long Token)> ReadProcedureDraftAsync(Guid procedureId, string actorSid, CancellationToken ct = default) => throw new NotSupportedException();
    Task<(ProcedureDefinition Procedure, long Token, ScientificOperationReceipt Receipt)> SaveProcedureDraftAsync(ProcedureDefinition procedure, long expectedToken, string actorSid, Guid operationId, CancellationToken ct = default) => throw new NotSupportedException();
    Task<IReadOnlyList<ScientificContentSummary>> ListProceduresAsync(string actorSid, CancellationToken ct = default) => throw new NotSupportedException();
    Task<ScientificContentSummary?> ReadProcedureLatestAsync(Guid procedureId, string actorSid, CancellationToken ct = default) => throw new NotSupportedException();
    Task<IReadOnlyList<ScientificOperationReceipt>> ProcedureHistoryAsync(Guid procedureId, string actorSid, CancellationToken ct = default) => throw new NotSupportedException();
    Task<ScientificRevision?> ReadProcedureAsync(Guid procedureId, Guid revisionId, string actorSid, CancellationToken ct = default) => throw new NotSupportedException();
    Task<ScientificRevision?> ReadProcedureByRevisionAsync(Guid revisionId, string actorSid, CancellationToken ct = default) => throw new NotSupportedException();
    Task<(ExperimentPlanDefinition Plan, long Token, ScientificOperationReceipt Receipt)> SavePlanDraftAsync(ExperimentPlanDefinition plan, ProcedureDefinition procedure, long expectedToken, string actorSid, Guid operationId, CancellationToken ct = default) => throw new NotSupportedException();
    Task<IReadOnlyList<ScientificContentSummary>> ListPlansAsync(string actorSid, CancellationToken ct = default) => throw new NotSupportedException();
    Task<ScientificContentSummary?> ReadPlanLatestAsync(Guid planId, string actorSid, CancellationToken ct = default) => throw new NotSupportedException();
    Task<IReadOnlyList<ScientificOperationReceipt>> PlanHistoryAsync(Guid planId, string actorSid, CancellationToken ct = default) => throw new NotSupportedException();
    Task<ScientificRevision?> ReadPlanAsync(Guid planId, Guid revisionId, string actorSid, CancellationToken ct = default) => throw new NotSupportedException();
}
