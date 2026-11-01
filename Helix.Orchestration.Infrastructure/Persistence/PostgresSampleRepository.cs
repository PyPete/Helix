using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Helix.Orchestration.Core.Samples;
using Npgsql;

namespace Helix.Orchestration.Infrastructure.Persistence;

public sealed class PostgresSampleRepository(DbConnectionFactory factory) : ISampleRepository
{
    public async Task<(SampleProjection Projection, SampleOperationReceipt Receipt)> ConfirmAsync(SampleConfirmation confirmation, SampleScope scope, Guid operationId, CancellationToken cancellationToken = default)
    {
        scope.Validate();
        SampleFactValidation.ValidateConfirmation(confirmation, DateTimeOffset.UtcNow);
        var sampleId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var sampleEvent = new SampleEvent(Guid.NewGuid(), sampleId, SampleEventKind.Confirmed, confirmation.Label, confirmation.Custodian, confirmation.Location, confirmation.OccurredAtUtc, now, scope.ActorSid, 1);
        return await WriteConfirmationAsync(new SampleIdentity(sampleId, SampleOrigin.Collection, []), sampleEvent, scope, operationId, cancellationToken);
    }

    public async Task<SampleProjection?> ReadAsync(Guid sampleId, SampleScope scope, CancellationToken cancellationToken = default)
    {
        scope.Validate();
        await using var connection = factory.Create();
        var identityRow = await connection.QuerySingleOrDefaultAsync<IdentityRow>(new CommandDefinition(
            "SELECT sample_id SampleId,origin Origin,parent_sample_ids ParentSampleIds FROM scientific_samples WHERE sample_id=@sampleId AND actor_sid=@actor",
            new { sampleId, actor = scope.ActorSid }, cancellationToken: cancellationToken));
        if (identityRow is null) return null;
        var events = await EventsAsync(sampleId, scope, null, 100, cancellationToken);
        if (events.Count == 0) throw new InvalidOperationException("Sample identity has no immutable event history.");
        return SampleProjection.Reconstruct(identityRow.ToIdentity(), events);
    }

    public async Task<IReadOnlyList<SampleProjection>> ListAsync(SampleScope scope, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        scope.Validate();
        await using var connection = factory.Create();
        var sampleIds = await connection.QueryAsync<Guid>(new CommandDefinition("SELECT sample_id FROM scientific_samples WHERE actor_sid=@actor ORDER BY created_at, sample_id LIMIT @limit", new { actor = scope.ActorSid, limit = Math.Clamp(pageSize, 1, 100) }, cancellationToken: cancellationToken));
        var samples = new List<SampleProjection>();
        foreach (var sampleId in sampleIds)
        {
            var sample = await ReadAsync(sampleId, scope, cancellationToken);
            if (sample is not null) samples.Add(sample);
        }
        return samples;
    }

    public async Task<IReadOnlyList<SampleEvent>> EventsAsync(Guid sampleId, SampleScope scope, long? afterSequence = null, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        scope.Validate();
        await using var connection = factory.Create();
        var rows = await connection.QueryAsync<Row>(new CommandDefinition("SELECT e.event_id EventId,e.sample_id SampleId,e.event_kind Kind,e.label Label,e.custodian Custodian,e.location Location,e.occurred_at OccurredAtUtc,e.recorded_at RecordedAtUtc,e.actor_sid ActorSid,e.server_sequence ServerSequence,e.supersedes_event_id SupersedesEventId,e.correction_reason CorrectionReason FROM scientific_sample_events e JOIN scientific_samples s ON s.sample_id=e.sample_id WHERE e.sample_id=@sampleId AND s.actor_sid=@actor AND (@after IS NULL OR e.server_sequence>@after) ORDER BY e.server_sequence LIMIT @limit", new { sampleId, actor = scope.ActorSid, after = afterSequence, limit = Math.Clamp(pageSize, 1, 100) }, cancellationToken: cancellationToken));
        return rows.Select(x => x.ToEvent()).ToArray();
    }

    public async Task<(SampleProjection Projection, SampleOperationReceipt Receipt)> AppendAsync(Guid sampleId, SampleEvent sampleEvent, long expectedSequence, SampleScope scope, Guid operationId, CancellationToken cancellationToken = default)
    {
        scope.Validate();
        if (sampleEvent.SampleId != sampleId) throw new ArgumentException("Event does not belong to the requested sample.", nameof(sampleEvent));
        if (sampleEvent.ServerSequence != expectedSequence + 1) throw new InvalidOperationException("The append sequence must advance exactly once.");
        SampleFactValidation.ValidateEvent(sampleEvent, DateTimeOffset.UtcNow);
        await using var connection = factory.Create(); await connection.OpenAsync(cancellationToken); await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var updated = await connection.ExecuteAsync(new CommandDefinition("UPDATE scientific_sample_heads h SET server_sequence=@next FROM scientific_samples s WHERE h.sample_id=@sampleId AND s.sample_id=h.sample_id AND s.actor_sid=@actor AND h.server_sequence=@expected", new { sampleId, actor = scope.ActorSid, expected = expectedSequence, next = sampleEvent.ServerSequence }, transaction, cancellationToken: cancellationToken));
        if (updated != 1) throw new InvalidOperationException("The sample history changed elsewhere.");
        await InsertEventAsync(connection, transaction, sampleEvent, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("UPDATE scientific_sample_heads SET event_id=@eventId WHERE sample_id=@sampleId", new { sampleId, eventId = sampleEvent.EventId }, transaction, cancellationToken: cancellationToken));
        var receipt = await InsertReceiptAsync(connection, transaction, operationId, "sample.append", sampleId, scope.ActorSid, sampleEvent, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await ReadAsync(sampleId, scope, cancellationToken) ?? throw new InvalidOperationException("Sample append was not readable."), receipt);
    }

    public async Task<SampleOperationReceipt?> OperationAsync(Guid operationId, SampleScope scope, CancellationToken cancellationToken = default)
    {
        scope.Validate(); await using var connection = factory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<ReceiptRow>(new CommandDefinition("SELECT operation_id OperationId,operation_kind Kind,sample_id SampleId,actor_sid ActorSid,recorded_at RecordedAtUtc,payload::text Payload FROM scientific_sample_operations WHERE actor_sid=@actor AND operation_id=@operationId", new { actor = scope.ActorSid, operationId }, cancellationToken: cancellationToken));
        return row?.ToReceipt();
    }

    public async Task<(IReadOnlyList<SampleProjection> Children, SampleOperationReceipt Receipt)> TransformAsync(SampleTransformation transformation, SampleScope scope, Guid operationId, CancellationToken cancellationToken = default)
    {
        scope.Validate();
        var validation = SampleLineageValidator.Validate(transformation);
        if (!validation.IsValid) throw new ArgumentException(string.Join(" ", validation.Diagnostics), nameof(transformation));

        transformation = transformation with
        {
            ParentSampleIds = transformation.ParentSampleIds.OrderBy(id => id).ToArray(),
            ChildSampleIds = transformation.ChildSampleIds.OrderBy(id => id).ToArray()
        };
        var payload = JsonSerializer.Serialize(transformation);
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        await using var connection = factory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Transaction-scoped advisory locks preserve least privilege: the runtime never needs
        // UPDATE permission merely to serialize idempotency or overlapping parent validation.
        await AcquireAdvisoryLockAsync(connection, transaction, AdvisoryLockKey("sample.transform.operation", scope.ActorSid, operationId), cancellationToken);

        var receiptRow = await connection.QuerySingleOrDefaultAsync<ReceiptHashRow>(new CommandDefinition(
            "SELECT request_hash RequestHash,operation_kind Kind,sample_id SampleId,actor_sid ActorSid,recorded_at RecordedAtUtc,payload::text Payload FROM scientific_sample_operations WHERE actor_sid=@actor AND operation_id=@operationId",
            new { actor = scope.ActorSid, operationId }, transaction, cancellationToken: cancellationToken));
        if (receiptRow is not null)
        {
            if (!StringComparer.Ordinal.Equals(receiptRow.RequestHash, requestHash))
                throw new InvalidOperationException("The operation identity was already used for different transformation content.");
            var existingChildren = await ReadTransformationChildrenAsync(connection, transaction, transformation.TransformationId, scope, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (existingChildren, receiptRow.ToReceipt());
        }

        foreach (var parentId in transformation.ParentSampleIds)
            await AcquireAdvisoryLockAsync(connection, transaction, AdvisoryLockKey("sample.transform.parent", scope.ActorSid, parentId), cancellationToken);

        var lockedParents = (await connection.QueryAsync<Guid>(new CommandDefinition(
            "SELECT sample_id FROM scientific_samples WHERE actor_sid=@actor AND sample_id = ANY(@parentIds) ORDER BY sample_id",
            new { actor = scope.ActorSid, parentIds = transformation.ParentSampleIds.ToArray() }, transaction, cancellationToken: cancellationToken))).ToArray();
        if (lockedParents.Length != transformation.ParentSampleIds.Count)
            throw new InvalidOperationException("Every transformation parent must exist in the authenticated scope.");

        var heads = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT count(*) FROM scientific_sample_heads WHERE sample_id = ANY(@parentIds)",
            new { parentIds = transformation.ParentSampleIds.ToArray() }, transaction, cancellationToken: cancellationToken));
        if (heads != transformation.ParentSampleIds.Count)
            throw new InvalidOperationException("Every transformation parent requires an authorized current head.");

        var existingChildCount = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT count(*) FROM scientific_samples WHERE sample_id = ANY(@childIds)",
            new { childIds = transformation.ChildSampleIds.ToArray() }, transaction, cancellationToken: cancellationToken));
        if (existingChildCount != 0)
            throw new InvalidOperationException("Transformation children must be newly assigned sample identifiers.");

        var now = DateTimeOffset.UtcNow;
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO scientific_sample_transformations(transformation_id,actor_sid,transformation_kind,operation_description,quantity_value,quantity_unit_code,quantity_dimension,quantity_supplied,occurred_at,recorded_at) VALUES (@TransformationId,@Actor,@Kind,@Operation,@QuantityValue,@QuantityUnitCode,@QuantityDimension,@QuantitySupplied,@OccurredAt,@RecordedAt)",
            new
            {
                transformation.TransformationId,
                Actor = scope.ActorSid,
                Kind = transformation.Kind.ToString(),
                transformation.Operation,
                QuantityValue = transformation.Quantity?.Value,
                QuantityUnitCode = transformation.Quantity?.UnitCode,
                QuantityDimension = transformation.Quantity?.Dimension,
                transformation.QuantitySupplied,
                OccurredAt = now,
                RecordedAt = now
            }, transaction, cancellationToken: cancellationToken));

        var children = new List<SampleProjection>(transformation.ChildSampleIds.Count);
        foreach (var childId in transformation.ChildSampleIds)
        {
            var identity = new SampleIdentity(childId, SampleOrigin.Transformation, transformation.ParentSampleIds.ToArray());
            var childEvent = new SampleEvent(Guid.NewGuid(), childId, SampleEventKind.Transformed, null, null, "transformation", now, now, scope.ActorSid, 1);
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO scientific_samples(sample_id,actor_sid,origin,parent_sample_ids,created_at) VALUES (@SampleId,@Actor,'transformation',@ParentIds,@RecordedAt)",
                new { SampleId = childId, Actor = scope.ActorSid, ParentIds = transformation.ParentSampleIds.ToArray(), RecordedAt = now }, transaction, cancellationToken: cancellationToken));
            await InsertEventAsync(connection, transaction, childEvent, cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO scientific_sample_heads(sample_id,server_sequence,event_id) VALUES (@SampleId,1,@EventId)",
                new { SampleId = childId, EventId = childEvent.EventId }, transaction, cancellationToken: cancellationToken));
            children.Add(SampleProjection.Reconstruct(identity, [childEvent]));
        }

        foreach (var parentId in transformation.ParentSampleIds)
        foreach (var childId in transformation.ChildSampleIds)
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO scientific_sample_lineage_edges(transformation_id,parent_sample_id,child_sample_id) VALUES (@transformationId,@parentId,@childId)",
                new { transformationId = transformation.TransformationId, parentId, childId }, transaction, cancellationToken: cancellationToken));

        var receipt = await InsertTransformationReceiptAsync(connection, transaction, operationId, children[0].Identity.SampleId, scope.ActorSid, payload, requestHash, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (children, receipt);
    }

    private static Task AcquireAdvisoryLockAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long lockKey, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_xact_lock(@lockKey)", new { lockKey }, transaction, cancellationToken: cancellationToken));

    private static long AdvisoryLockKey(string domain, string actorSid, Guid identity)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{domain}\n{actorSid}\n{identity:D}"));
        return BinaryPrimitives.ReadInt64BigEndian(bytes);
    }

    public async Task<SampleLineage> LineageAsync(Guid sampleId, SampleLineageDirection direction, SampleScope scope, Guid? continuation = null, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        scope.Validate();
        if (sampleId == Guid.Empty) throw new ArgumentException("A sample identifier is required.", nameof(sampleId));
        var limit = Math.Clamp(pageSize, 1, 100);
        await using var connection = factory.Create();
        var exists = await connection.ExecuteScalarAsync<long>(new CommandDefinition("SELECT count(*) FROM scientific_samples WHERE sample_id=@sampleId AND actor_sid=@actor", new { sampleId, actor = scope.ActorSid }, commandTimeout: 5, cancellationToken: cancellationToken));
        if (exists != 1) throw new InvalidOperationException("The requested sample is not available in the authenticated scope.");

        var edgeJoin = direction == SampleLineageDirection.Descendants
            ? "e.parent_sample_id = walk.sample_id"
            : "e.child_sample_id = walk.sample_id";
        var nextId = direction == SampleLineageDirection.Descendants ? "e.child_sample_id" : "e.parent_sample_id";
        var sql = $"""
            WITH RECURSIVE walk(sample_id, transformation_id, depth, path) AS (
                SELECT @sampleId::uuid, NULL::uuid, 0, ARRAY[@sampleId::uuid]
                UNION ALL
                SELECT {nextId}, e.transformation_id, walk.depth + 1, walk.path || {nextId}
                FROM walk JOIN scientific_sample_lineage_edges e ON {edgeJoin}
                JOIN scientific_samples s ON s.sample_id = {nextId} AND s.actor_sid = @actor
                WHERE walk.depth < 32 AND NOT ({nextId} = ANY(walk.path))
            )
            SELECT walk.sample_id SampleId,s.origin Origin,walk.transformation_id TransformationId,walk.depth Depth
            FROM walk JOIN scientific_samples s ON s.sample_id = walk.sample_id
            WHERE walk.depth > 0 AND (@continuation IS NULL OR walk.sample_id > @continuation)
            ORDER BY walk.depth,walk.sample_id LIMIT @take
            """;
        var rows = (await connection.QueryAsync<LineageRow>(new CommandDefinition(sql, new { sampleId, actor = scope.ActorSid, continuation, take = Math.Min(limit + 1, 1001) }, commandTimeout: 5, cancellationToken: cancellationToken))).ToArray();
        var truncated = rows.Length > limit;
        var entries = rows.Take(limit).Select(row => row.ToEntry()).ToArray();
        return new SampleLineage(entries, truncated && entries.Length > 0 ? entries[^1].SampleId : null, truncated);
    }

    private async Task<(SampleProjection Projection, SampleOperationReceipt Receipt)> WriteConfirmationAsync(SampleIdentity identity, SampleEvent sampleEvent, SampleScope scope, Guid operationId, CancellationToken cancellationToken)
    {
        await using var connection = factory.Create(); await connection.OpenAsync(cancellationToken); await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("INSERT INTO scientific_samples(sample_id,actor_sid,origin,parent_sample_ids,created_at) VALUES (@sampleId,@actor,'collection','{}',@now);", new { sampleId = identity.SampleId, actor = scope.ActorSid, now = sampleEvent.RecordedAtUtc }, transaction, cancellationToken: cancellationToken));
        await InsertEventAsync(connection, transaction, sampleEvent, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("INSERT INTO scientific_sample_heads(sample_id,server_sequence,event_id) VALUES (@sampleId,1,@eventId);", new { sampleId = identity.SampleId, eventId = sampleEvent.EventId }, transaction, cancellationToken: cancellationToken));
        var receipt = await InsertReceiptAsync(connection, transaction, operationId, "sample.confirm", identity.SampleId, scope.ActorSid, sampleEvent, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (SampleProjection.Reconstruct(identity, [sampleEvent]), receipt);
    }

    private static async Task<IReadOnlyList<SampleProjection>> ReadTransformationChildrenAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid transformationId, SampleScope scope, CancellationToken cancellationToken)
    {
        var childIds = (await connection.QueryAsync<Guid>(new CommandDefinition("SELECT DISTINCT e.child_sample_id FROM scientific_sample_lineage_edges e JOIN scientific_samples s ON s.sample_id=e.child_sample_id WHERE e.transformation_id=@transformationId AND s.actor_sid=@actor ORDER BY e.child_sample_id", new { transformationId, actor = scope.ActorSid }, transaction, cancellationToken: cancellationToken))).ToArray();
        var children = new List<SampleProjection>(childIds.Length);
        foreach (var childId in childIds)
        {
            var rows = await connection.QueryAsync<Row>(new CommandDefinition("SELECT event_id EventId,sample_id SampleId,event_kind Kind,label Label,custodian Custodian,location Location,occurred_at OccurredAtUtc,recorded_at RecordedAtUtc,actor_sid ActorSid,server_sequence ServerSequence,supersedes_event_id SupersedesEventId,correction_reason CorrectionReason FROM scientific_sample_events WHERE sample_id=@childId ORDER BY server_sequence", new { childId }, transaction, cancellationToken: cancellationToken));
            var parents = await connection.QueryAsync<Guid>(new CommandDefinition("SELECT DISTINCT parent_sample_id FROM scientific_sample_lineage_edges WHERE transformation_id=@transformationId AND child_sample_id=@childId ORDER BY parent_sample_id", new { transformationId, childId }, transaction, cancellationToken: cancellationToken));
            children.Add(SampleProjection.Reconstruct(new SampleIdentity(childId, SampleOrigin.Transformation, parents.ToArray()), rows.Select(row => row.ToEvent())));
        }
        return children;
    }

    private static async Task<SampleOperationReceipt> InsertTransformationReceiptAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid operationId, Guid sampleId, string actorSid, string payload, string hash, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await connection.ExecuteAsync(new CommandDefinition("INSERT INTO scientific_sample_operations(actor_sid,operation_id,operation_kind,sample_id,request_hash,payload,recorded_at) VALUES (@actorSid,@operationId,'sample.transform',@sampleId,@hash,CAST(@payload AS jsonb),@now)", new { actorSid, operationId, sampleId, hash, payload, now }, transaction, cancellationToken: cancellationToken));
        return new SampleOperationReceipt(operationId, "sample.transform", sampleId, actorSid, now, payload);
    }

    private static Task<int> InsertEventAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, SampleEvent value, CancellationToken ct) => connection.ExecuteAsync(new CommandDefinition("INSERT INTO scientific_sample_events(event_id,sample_id,event_kind,label,custodian,location,occurred_at,recorded_at,actor_sid,server_sequence,supersedes_event_id,correction_reason) VALUES (@EventId,@SampleId,@Kind,@Label,@Custodian,@Location,@OccurredAtUtc,@RecordedAtUtc,@ActorSid,@ServerSequence,@SupersedesEventId,@CorrectionReason)", value, transaction, cancellationToken: ct));
    private static async Task<SampleOperationReceipt> InsertReceiptAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid operationId, string kind, Guid sampleId, string actorSid, SampleEvent sampleEvent, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(sampleEvent); var now = DateTimeOffset.UtcNow; var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        await connection.ExecuteAsync(new CommandDefinition("INSERT INTO scientific_sample_operations(actor_sid,operation_id,operation_kind,sample_id,request_hash,payload,recorded_at) VALUES (@actorSid,@operationId,@kind,@sampleId,@hash,CAST(@payload AS jsonb),@now)", new { actorSid, operationId, kind, sampleId, hash, payload, now }, transaction, cancellationToken: ct));
        return new SampleOperationReceipt(operationId, kind, sampleId, actorSid, now, payload);
    }
    private sealed class Row
    {
        public Guid EventId { get; init; }
        public Guid SampleId { get; init; }
        public string Kind { get; init; } = "";
        public string? Label { get; init; }
        public string? Custodian { get; init; }
        public string Location { get; init; } = "";
        public DateTime OccurredAtUtc { get; init; }
        public DateTime RecordedAtUtc { get; init; }
        public string ActorSid { get; init; } = "";
        public long ServerSequence { get; init; }
        public Guid? SupersedesEventId { get; init; }
        public string? CorrectionReason { get; init; }
        public SampleEvent ToEvent() => new(EventId, SampleId, Enum.Parse<SampleEventKind>(Kind, true), Label, Custodian, Location, new DateTimeOffset(OccurredAtUtc, TimeSpan.Zero), new DateTimeOffset(RecordedAtUtc, TimeSpan.Zero), ActorSid, ServerSequence, SupersedesEventId, CorrectionReason);
    }

    private class ReceiptRow
    {
        public Guid OperationId { get; init; }
        public string Kind { get; init; } = "";
        public Guid SampleId { get; init; }
        public string ActorSid { get; init; } = "";
        public DateTime RecordedAtUtc { get; init; }
        public string Payload { get; init; } = "";
        public SampleOperationReceipt ToReceipt() => new(OperationId, Kind, SampleId, ActorSid, new DateTimeOffset(RecordedAtUtc, TimeSpan.Zero), Payload);
    }

    private sealed class ReceiptHashRow : ReceiptRow
    {
        public string RequestHash { get; init; } = "";
    }

    private sealed class LineageRow
    {
        public Guid SampleId { get; init; }
        public string Origin { get; init; } = "";
        public Guid? TransformationId { get; init; }
        public int Depth { get; init; }
        public SampleLineageEntry ToEntry() => new(SampleId, Enum.Parse<SampleOrigin>(Origin, true), TransformationId, Depth);
    }

    private sealed class IdentityRow
    {
        public Guid SampleId { get; init; }
        public string Origin { get; init; } = "";
        public Guid[] ParentSampleIds { get; init; } = [];
        public SampleIdentity ToIdentity() => new(SampleId, Enum.Parse<SampleOrigin>(Origin, true), ParentSampleIds);
    }
}
