using System.Text.Json;
using Dapper;
using Helix.Orchestration.Core.ScientificSetup;
using Npgsql;

namespace Helix.Orchestration.Infrastructure.Persistence;

public sealed class PostgresScientificSetupRepository(DbConnectionFactory factory) : IScientificSetupRepository
{
    private const string CreateKind = "apparatus.create";
    private const string PublishKind = "apparatus.publish";
    private const string SaveDraftKind = "apparatus.save-draft";
    private const string AdoptRevisionKind = "apparatus.adopt-revision";
    private const string OperationConflictMessage = "Operation ID was previously used with a different request.";

    public async Task<(ApparatusDefinition Apparatus, long Token, ScientificOperationReceipt Receipt)> CreateAsync(ApparatusDefinition apparatus, string actorSid, Guid operationId, CancellationToken ct = default)
    {
        apparatus.Validate();
        var requestHash = CanonicalRequestHash(new { apparatus.Name, apparatus.Components });
        var existing = await GetOperationAsync(operationId, CreateKind, actorSid, ct);
        if (existing is not null)
            return await ReplayCreateAsync(existing, requestHash, ct);

        var payload = JsonSerializer.Serialize(apparatus);
        var receipt = new ScientificOperationReceipt(operationId, CreateKind, apparatus.ApparatusId, actorSid, DateTimeOffset.UtcNow, payload);
        try
        {
            await using var connection = factory.Create();
            await connection.OpenAsync(ct);
            await using var transaction = await connection.BeginTransactionAsync(ct);
            await connection.ExecuteAsync(new CommandDefinition("INSERT INTO scientific_apparatus (apparatus_id,actor_sid,name,draft_content) VALUES (@id,@actor,@name,CAST(@payload AS jsonb)); INSERT INTO scientific_operations(actor_sid,operation_kind,operation_id,resource_id,payload,request_hash,recorded_at) VALUES (@actor,@kind,@operationId,@id,CAST(@payload AS jsonb),@requestHash,@at);", new { operationId, id = apparatus.ApparatusId, actor = actorSid, name = apparatus.Name, payload, kind = receipt.Kind, requestHash, at = receipt.RecordedAtUtc }, transaction, cancellationToken: ct));
            await transaction.CommitAsync(ct);
            var persisted = await GetOperationAsync(operationId, CreateKind, actorSid, ct)
                ?? throw new InvalidOperationException("Created operation receipt was not persisted.");
            return (apparatus, 1, persisted.Receipt);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            var raced = await GetOperationAsync(operationId, CreateKind, actorSid, ct);
            if (raced is null) throw;
            return await ReplayCreateAsync(raced, requestHash, ct);
        }
    }

    public async Task<(ApparatusDefinition Apparatus, long Token, ScientificOperationReceipt Receipt)> SaveDraftAsync(ApparatusDefinition apparatus, long expectedToken, string actorSid, Guid operationId, CancellationToken ct = default)
    {
        apparatus.Validate();
        var requestHash = CanonicalRequestHash(new { apparatus, expectedToken });
        var existing = await GetOperationAsync(operationId, SaveDraftKind, actorSid, ct);
        if (existing is not null)
            return await ReplayCreateAsync(existing, requestHash, ct);

        var payload = JsonSerializer.Serialize(apparatus);
        var receipt = new ScientificOperationReceipt(operationId, SaveDraftKind, apparatus.ApparatusId, actorSid, DateTimeOffset.UtcNow, payload);
        await using var connection = factory.Create();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            var updated = await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE scientific_apparatus SET name=@name,draft_content=CAST(@payload AS jsonb),draft_token=draft_token+1 WHERE apparatus_id=@id AND actor_sid=@actor AND draft_token=@expectedToken;",
                new { id = apparatus.ApparatusId, actor = actorSid, name = apparatus.Name, payload, expectedToken }, transaction, cancellationToken: ct));
            if (updated != 1)
                throw new InvalidOperationException("The apparatus draft changed elsewhere.");
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO scientific_operations(actor_sid,operation_kind,operation_id,resource_id,payload,request_hash,recorded_at) VALUES (@actor,@kind,@operationId,@id,CAST(@payload AS jsonb),@requestHash,@at);",
                new { operationId, id = apparatus.ApparatusId, actor = actorSid, kind = receipt.Kind, payload = receipt.Payload, requestHash, at = receipt.RecordedAtUtc }, transaction, cancellationToken: ct));
            await transaction.CommitAsync(ct);
            return (apparatus, expectedToken + 1, receipt);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(ct);
            var raced = await GetOperationAsync(operationId, SaveDraftKind, actorSid, ct);
            if (raced is null) throw;
            return await ReplayCreateAsync(raced, requestHash, ct);
        }
    }

    public async Task<(ApparatusDefinition Apparatus, long Token)> ReadDraftAsync(Guid apparatusId, string actorSid, CancellationToken ct = default)
    {
        await using var connection = factory.Create();
        var draft = await connection.QuerySingleOrDefaultAsync<Draft>(new CommandDefinition(
            "SELECT draft_content::text AS Content,draft_token AS Token,latest_revision_id AS LatestRevisionId FROM scientific_apparatus WHERE apparatus_id=@apparatusId AND actor_sid=@actorSid;",
            new { apparatusId, actorSid }, cancellationToken: ct));
        if (draft is null) throw new KeyNotFoundException();
        return (JsonSerializer.Deserialize<ApparatusDefinition>(draft.Content) ?? throw new InvalidOperationException("Stored apparatus draft is invalid."), draft.Token);
    }

    public async Task<(ApparatusDefinition Apparatus, long Token, ScientificOperationReceipt Receipt)> AdoptRevisionAsync(Guid apparatusId, Guid revisionId, long expectedToken, string actorSid, Guid operationId, CancellationToken ct = default)
    {
        var existing = await GetOperationAsync(operationId, AdoptRevisionKind, actorSid, ct);
        if (existing is not null)
            return await ReplayCreateAsync(existing, CanonicalRequestHash(new { apparatusId, revisionId, expectedToken }), ct);

        var revision = await ReadAsync(apparatusId, revisionId, actorSid, ct) ?? throw new KeyNotFoundException();
        using var document = JsonDocument.Parse(revision.CanonicalContent);
        var content = document.RootElement.TryGetProperty("content", out var envelope) ? envelope : document.RootElement;
        var source = JsonSerializer.Deserialize<ApparatusDefinition>(content.GetRawText()) ?? throw new InvalidOperationException("Stored apparatus revision is invalid.");
        var adopted = source with { ApparatusId = apparatusId, SourceRevisionId = revisionId };
        adopted.Validate();
        var requestHash = CanonicalRequestHash(new { apparatusId, revisionId, expectedToken });
        var payload = JsonSerializer.Serialize(adopted);
        var receipt = new ScientificOperationReceipt(operationId, AdoptRevisionKind, apparatusId, actorSid, DateTimeOffset.UtcNow, payload);
        await using var connection = factory.Create();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var updated = await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE scientific_apparatus SET name=@name,draft_content=CAST(@payload AS jsonb),draft_token=draft_token+1 WHERE apparatus_id=@id AND actor_sid=@actor AND draft_token=@expectedToken;",
            new { id = apparatusId, actor = actorSid, name = adopted.Name, payload, expectedToken }, transaction, cancellationToken: ct));
        if (updated != 1) throw new InvalidOperationException("The apparatus draft changed elsewhere.");
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO scientific_operations(actor_sid,operation_kind,operation_id,resource_id,payload,request_hash,recorded_at) VALUES (@actor,@kind,@operationId,@id,CAST(@payload AS jsonb),@requestHash,@at);",
            new { operationId, id = apparatusId, actor = actorSid, kind = receipt.Kind, payload = receipt.Payload, requestHash, at = receipt.RecordedAtUtc }, transaction, cancellationToken: ct));
        await transaction.CommitAsync(ct);
        return (adopted, expectedToken + 1, receipt);
    }

    public async Task<(ScientificRevision Revision, ScientificOperationReceipt Receipt)> PublishAsync(Guid apparatusId, Guid? expectedRevisionId, string name, string actorSid, Guid operationId, CancellationToken ct = default)
    {
        var requestHash = CanonicalRequestHash(new { apparatusId, expectedRevisionId, name });
        var existing = await GetOperationAsync(operationId, PublishKind, actorSid, ct);
        if (existing is not null)
            return ReplayPublish(existing, requestHash);

        await using var connection = factory.Create();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            var draft = await connection.QuerySingleOrDefaultAsync<Draft>(new CommandDefinition("SELECT draft_content::text AS Content,latest_revision_id AS LatestRevisionId FROM scientific_apparatus WHERE apparatus_id=@apparatusId AND actor_sid=@actorSid FOR UPDATE;", new { apparatusId, actorSid }, transaction, cancellationToken: ct));
            if (draft is null) throw new KeyNotFoundException();
            if (draft.LatestRevisionId != expectedRevisionId) throw new InvalidOperationException("The apparatus revision changed elsewhere.");

            var canonical = ScientificContentCanonicalizer.Canonicalize(JsonDocument.Parse(draft.Content).RootElement);
            var revision = new ScientificRevision(Guid.NewGuid(), apparatusId, name, canonical.Hash, actorSid, DateTimeOffset.UtcNow, canonical.Content);
            var receipt = new ScientificOperationReceipt(operationId, PublishKind, apparatusId, actorSid, revision.RecordedAtUtc, JsonSerializer.Serialize(revision));
            await connection.ExecuteAsync(new CommandDefinition("INSERT INTO scientific_apparatus_revisions(revision_id,apparatus_id,revision_name,content_hash,canonical_content,actor_sid,recorded_at) VALUES (@rid,@aid,@name,@hash,CAST(@content AS jsonb),@actor,@at); UPDATE scientific_apparatus SET latest_revision_id=@rid,draft_token=draft_token+1 WHERE apparatus_id=@aid; INSERT INTO scientific_operations(actor_sid,operation_kind,operation_id,resource_id,payload,request_hash,recorded_at) VALUES (@actor,@kind,@operationId,@aid,CAST(@payload AS jsonb),@requestHash,@at);", new { rid = revision.RevisionId, aid = apparatusId, name, hash = canonical.Hash, content = canonical.Content, actor = actorSid, at = revision.RecordedAtUtc, kind = receipt.Kind, operationId, payload = receipt.Payload, requestHash }, transaction, cancellationToken: ct));
            await transaction.CommitAsync(ct);
            var persisted = await GetOperationAsync(operationId, PublishKind, actorSid, ct)
                ?? throw new InvalidOperationException("Published operation receipt was not persisted.");
            return (revision, persisted.Receipt);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(ct);
            var raced = await GetOperationAsync(operationId, PublishKind, actorSid, ct);
            if (raced is null) throw;
            return ReplayPublish(raced, requestHash);
        }
    }

    public async Task<ScientificRevision?> ReadAsync(Guid apparatusId, Guid revisionId, string actorSid, CancellationToken ct = default)
    {
        await using var connection = factory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<RevisionRow>(new CommandDefinition("SELECT revision_id AS RevisionId,r.apparatus_id AS ApparatusId,revision_name AS Name,content_hash AS ContentHash,r.actor_sid AS ActorSid,recorded_at AS RecordedAtUtc,canonical_content::text AS CanonicalContent FROM scientific_apparatus_revisions r JOIN scientific_apparatus a ON a.apparatus_id=r.apparatus_id WHERE r.apparatus_id=@apparatusId AND revision_id=@revisionId AND a.actor_sid=@actorSid;", new { apparatusId, revisionId, actorSid }, cancellationToken: ct));
        return row is null ? null : new ScientificRevision(row.RevisionId, row.ApparatusId, row.Name, row.ContentHash, row.ActorSid, new DateTimeOffset(DateTime.SpecifyKind(row.RecordedAtUtc, DateTimeKind.Utc)), row.CanonicalContent);
    }

    public async Task<IReadOnlyList<ScientificApparatusSummary>> ListAsync(string actorSid, CancellationToken ct = default)
    {
        await using var connection = factory.Create();
        var values = await connection.QueryAsync<ScientificApparatusSummary>(new CommandDefinition("SELECT apparatus_id AS ApparatusId,name AS Name,latest_revision_id AS RevisionId FROM scientific_apparatus WHERE actor_sid=@actorSid ORDER BY name, apparatus_id;", new { actorSid }, cancellationToken: ct));
        return values.AsList();
    }

    public async Task<ScientificApparatusSummary?> ReadLatestAsync(Guid apparatusId, string actorSid, CancellationToken ct = default)
    {
        await using var connection = factory.Create();
        return await connection.QuerySingleOrDefaultAsync<ScientificApparatusSummary>(new CommandDefinition("SELECT apparatus_id AS ApparatusId,name AS Name,latest_revision_id AS RevisionId FROM scientific_apparatus WHERE apparatus_id=@apparatusId AND actor_sid=@actorSid;", new { apparatusId, actorSid }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ScientificOperationReceipt>> HistoryAsync(Guid apparatusId, string actorSid, CancellationToken ct = default)
    {
        await using var connection = factory.Create();
        var values = await connection.QueryAsync<StoredOperation>(new CommandDefinition("SELECT operation_id AS OperationId,operation_kind AS Kind,resource_id AS ResourceId,actor_sid AS ActorSid,recorded_at AS RecordedAtUtc,payload::text AS Payload,request_hash AS RequestHash FROM scientific_operations WHERE resource_id=@apparatusId AND actor_sid=@actorSid ORDER BY recorded_at, operation_id;", new { apparatusId, actorSid }, cancellationToken: ct));
        return values.Select(value => value.Receipt).ToList();
    }

    public async Task<ScientificOperationReceipt?> OperationAsync(Guid operationId, string actorSid, CancellationToken ct = default)
    {
        await using var connection = factory.Create();
        var operation = await connection.QuerySingleOrDefaultAsync<StoredOperation>(new CommandDefinition("SELECT operation_id AS OperationId,operation_kind AS Kind,resource_id AS ResourceId,actor_sid AS ActorSid,recorded_at AS RecordedAtUtc,payload::text AS Payload,request_hash AS RequestHash FROM scientific_operations WHERE operation_id=@operationId AND actor_sid=@actorSid;", new { operationId, actorSid }, cancellationToken: ct));
        return operation?.Receipt;
    }

    public async Task<(ProcedureDefinition Procedure, long Token, ScientificOperationReceipt Receipt)> CreateProcedureAsync(ProcedureDefinition procedure, string actorSid, Guid operationId, CancellationToken ct = default)
    {
        procedure.Validate();
        const string kind = "procedure.create";
        var requestHash = CanonicalRequestHash(procedure);
        var existing = await GetOperationAsync(operationId, kind, actorSid, ct);
        if (existing is not null)
        {
            EnsureSameRequest(existing, requestHash);
            return (JsonSerializer.Deserialize<ProcedureDefinition>(existing.Receipt.Payload) ?? throw new InvalidOperationException("Stored procedure receipt is invalid."), 1, existing.Receipt);
        }

        var payload = JsonSerializer.Serialize(procedure);
        var receipt = new ScientificOperationReceipt(operationId, kind, procedure.ProcedureId, actorSid, DateTimeOffset.UtcNow, payload);
        await using var connection = factory.Create();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            await connection.ExecuteAsync(new CommandDefinition("INSERT INTO scientific_procedures(procedure_id,actor_sid,name,draft_content) VALUES (@id,@actor,@name,CAST(@payload AS jsonb)); INSERT INTO scientific_operations(actor_sid,operation_kind,operation_id,resource_id,payload,request_hash,recorded_at) VALUES (@actor,@kind,@operationId,@id,CAST(@payload AS jsonb),@requestHash,@at);", new { id = procedure.ProcedureId, actor = actorSid, name = procedure.Name, payload, kind, operationId, requestHash, at = receipt.RecordedAtUtc }, transaction, cancellationToken: ct));
            await transaction.CommitAsync(ct);
            return (procedure, 1, receipt);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(ct);
            var raced = await GetOperationAsync(operationId, kind, actorSid, ct) ?? throw new InvalidOperationException("Procedure operation race did not persist a receipt.");
            EnsureSameRequest(raced, requestHash);
            return (JsonSerializer.Deserialize<ProcedureDefinition>(raced.Receipt.Payload) ?? throw new InvalidOperationException("Stored procedure receipt is invalid."), 1, raced.Receipt);
        }
    }

    public async Task<(ExperimentPlanDefinition Plan, long Token, ScientificOperationReceipt Receipt)> CreatePlanAsync(ExperimentPlanDefinition plan, ProcedureDefinition procedure, string actorSid, Guid operationId, CancellationToken ct = default)
    {
        plan.ValidateAgainst(procedure);
        const string kind = "plan.create";
        var requestHash = CanonicalRequestHash(plan);
        var existing = await GetOperationAsync(operationId, kind, actorSid, ct);
        if (existing is not null)
        {
            EnsureSameRequest(existing, requestHash);
            return (JsonSerializer.Deserialize<ExperimentPlanDefinition>(existing.Receipt.Payload) ?? throw new InvalidOperationException("Stored plan receipt is invalid."), 1, existing.Receipt);
        }

        var payload = JsonSerializer.Serialize(plan);
        var receipt = new ScientificOperationReceipt(operationId, kind, plan.PlanId, actorSid, DateTimeOffset.UtcNow, payload);
        await using var connection = factory.Create();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            await connection.ExecuteAsync(new CommandDefinition("INSERT INTO scientific_experiment_plans(plan_id,actor_sid,name,draft_content) VALUES (@id,@actor,@name,CAST(@payload AS jsonb)); INSERT INTO scientific_operations(actor_sid,operation_kind,operation_id,resource_id,payload,request_hash,recorded_at) VALUES (@actor,@kind,@operationId,@id,CAST(@payload AS jsonb),@requestHash,@at);", new { id = plan.PlanId, actor = actorSid, name = plan.Name, payload, kind, operationId, requestHash, at = receipt.RecordedAtUtc }, transaction, cancellationToken: ct));
            await transaction.CommitAsync(ct);
            return (plan, 1, receipt);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(ct);
            var raced = await GetOperationAsync(operationId, kind, actorSid, ct) ?? throw new InvalidOperationException("Plan operation race did not persist a receipt.");
            EnsureSameRequest(raced, requestHash);
            return (JsonSerializer.Deserialize<ExperimentPlanDefinition>(raced.Receipt.Payload) ?? throw new InvalidOperationException("Stored plan receipt is invalid."), 1, raced.Receipt);
        }
    }

    public async Task<(ExperimentPlanDefinition Plan, long Token)> ReadPlanDraftAsync(Guid planId, string actorSid, CancellationToken ct = default)
    {
        await using var connection = factory.Create();
        var draft = await connection.QuerySingleOrDefaultAsync<Draft>(new CommandDefinition("SELECT draft_content::text AS Content,draft_token AS Token FROM scientific_experiment_plans WHERE plan_id=@planId AND actor_sid=@actorSid;", new { planId, actorSid }, cancellationToken: ct));
        if (draft is null) throw new KeyNotFoundException();
        return (JsonSerializer.Deserialize<ExperimentPlanDefinition>(draft.Content) ?? throw new InvalidOperationException("Stored plan draft is invalid."), draft.Token);
    }

    private async Task<(ApparatusDefinition Apparatus, long Token, ScientificOperationReceipt Receipt)> ReplayCreateAsync(StoredOperation operation, string requestHash, CancellationToken ct)
    {
        EnsureSameRequest(operation, requestHash);
        var apparatus = JsonSerializer.Deserialize<ApparatusDefinition>(operation.Receipt.Payload) ?? throw new InvalidOperationException("Stored create receipt is invalid.");
        await using var connection = factory.Create();
        var token = await connection.QuerySingleAsync<long>(new CommandDefinition("SELECT draft_token FROM scientific_apparatus WHERE apparatus_id=@apparatusId;", new { apparatusId = apparatus.ApparatusId }, cancellationToken: ct));
        return (apparatus, token, operation.Receipt);
    }

    private static (ScientificRevision Revision, ScientificOperationReceipt Receipt) ReplayPublish(StoredOperation operation, string requestHash)
    {
        EnsureSameRequest(operation, requestHash);
        var revision = JsonSerializer.Deserialize<ScientificRevision>(operation.Receipt.Payload) ?? throw new InvalidOperationException("Stored publish receipt is invalid.");
        return (revision, operation.Receipt);
    }

    private async Task<StoredOperation?> GetOperationAsync(Guid operationId, string kind, string actorSid, CancellationToken ct)
    {
        await using var connection = factory.Create();
        return await connection.QuerySingleOrDefaultAsync<StoredOperation>(new CommandDefinition("SELECT operation_id AS OperationId,operation_kind AS Kind,resource_id AS ResourceId,actor_sid AS ActorSid,recorded_at AS RecordedAtUtc,payload::text AS Payload,request_hash AS RequestHash FROM scientific_operations WHERE operation_id=@operationId AND operation_kind=@kind AND actor_sid=@actorSid;", new { operationId, kind, actorSid }, cancellationToken: ct));
    }

    private static string CanonicalRequestHash<T>(T request) => ScientificContentCanonicalizer.Canonicalize(JsonSerializer.SerializeToElement(request)).Hash;

    private static void EnsureSameRequest(StoredOperation operation, string requestHash)
    {
        if (!string.Equals(operation.RequestHash, requestHash, StringComparison.Ordinal))
            throw new InvalidOperationException(OperationConflictMessage);
    }

    private sealed class Draft
    {
        public string Content { get; init; } = "{}";
        public Guid? LatestRevisionId { get; init; }
        public long Token { get; init; }
    }

    private sealed class StoredOperation
    {
        public Guid OperationId { get; init; }
        public string Kind { get; init; } = string.Empty;
        public Guid ResourceId { get; init; }
        public string ActorSid { get; init; } = string.Empty;
        public DateTimeOffset RecordedAtUtc { get; init; }
        public string Payload { get; init; } = string.Empty;
        public string RequestHash { get; init; } = string.Empty;
        public ScientificOperationReceipt Receipt => new(OperationId, Kind, ResourceId, ActorSid, RecordedAtUtc, Payload);
    }

    private sealed class RevisionRow
    {
        public Guid RevisionId { get; init; }
        public Guid ApparatusId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string ContentHash { get; init; } = string.Empty;
        public string ActorSid { get; init; } = string.Empty;
        public DateTime RecordedAtUtc { get; init; }
        public string CanonicalContent { get; init; } = string.Empty;
    }
}
