using Helix.Orchestration.Core.ScientificSetup;

namespace Helix.Orchestration.Agent.ScientificSetup;

/// <summary>Server-authoritative scientific lifecycle facade. Caller identity always comes from authenticated middleware.</summary>
public sealed class ScientificSetupService(IScientificSetupRepository repository)
{
    public Task<IReadOnlyList<ScientificContentSummary>> ListProceduresAsync(string actorSid, CancellationToken ct) => repository.ListProceduresAsync(actorSid, ct);
    public Task<IReadOnlyList<ScientificContentSummary>> ListPlansAsync(string actorSid, CancellationToken ct) => repository.ListPlansAsync(actorSid, ct);
    public Task<(ProcedureDefinition Procedure, long Token, ScientificOperationReceipt Receipt)> CreateProcedureAsync(ProcedureDefinition procedure, string actorSid, Guid operationId, CancellationToken ct) => repository.CreateProcedureAsync(procedure with { ProcedureId = Guid.NewGuid() }, actorSid, operationId, ct);
    public Task<(ExperimentPlanDefinition Plan, long Token, ScientificOperationReceipt Receipt)> CreatePlanAsync(ExperimentPlanDefinition plan, ProcedureDefinition procedure, string actorSid, Guid operationId, CancellationToken ct) => repository.CreatePlanAsync(plan with { PlanId = Guid.NewGuid() }, procedure, actorSid, operationId, ct);
    public Task<(ProcedureDefinition Procedure, long Token)> ReadProcedureDraftAsync(Guid id, string actorSid, CancellationToken ct) => repository.ReadProcedureDraftAsync(id, actorSid, ct);
    public Task<(ExperimentPlanDefinition Plan, long Token)> ReadPlanDraftAsync(Guid id, string actorSid, CancellationToken ct) => repository.ReadPlanDraftAsync(id, actorSid, ct);
    public Task<(ProcedureDefinition Procedure, long Token, ScientificOperationReceipt Receipt)> SaveProcedureDraftAsync(ProcedureDefinition procedure, long token, string actorSid, Guid operationId, CancellationToken ct) => repository.SaveProcedureDraftAsync(procedure, token, actorSid, operationId, ct);
    public Task<(ExperimentPlanDefinition Plan, long Token, ScientificOperationReceipt Receipt)> SavePlanDraftAsync(ExperimentPlanDefinition plan, ProcedureDefinition procedure, long token, string actorSid, Guid operationId, CancellationToken ct) => repository.SavePlanDraftAsync(plan, procedure, token, actorSid, operationId, ct);
    public Task<(ScientificRevision Revision, ScientificOperationReceipt Receipt)> PublishProcedureAsync(Guid id, Guid? expectedRevision, string name, string actorSid, Guid operationId, CancellationToken ct) => repository.PublishProcedureAsync(id, expectedRevision, name, actorSid, operationId, ct);
    public Task<(ScientificRevision Revision, ScientificOperationReceipt Receipt)> PublishPlanAsync(Guid id, Guid? expectedRevision, string name, string actorSid, Guid operationId, CancellationToken ct) => repository.PublishPlanAsync(id, expectedRevision, name, actorSid, operationId, ct);
}
