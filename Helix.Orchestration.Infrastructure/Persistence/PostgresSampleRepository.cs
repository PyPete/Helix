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
        var events = await EventsAsync(sampleId, scope, null, 100, cancellationToken);
        if (events.Count == 0) return null;
        return SampleProjection.Reconstruct(new SampleIdentity(sampleId, SampleOrigin.Collection, []), events);
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
        var updated = await connection.ExecuteAsync(new CommandDefinition("UPDATE scientific_sample_heads h SET server_sequence=@next,event_id=@eventId FROM scientific_samples s WHERE h.sample_id=@sampleId AND s.sample_id=h.sample_id AND s.actor_sid=@actor AND h.server_sequence=@expected", new { sampleId, actor = scope.ActorSid, expected = expectedSequence, next = sampleEvent.ServerSequence, eventId = sampleEvent.EventId }, transaction, cancellationToken: cancellationToken));
        if (updated != 1) throw new InvalidOperationException("The sample history changed elsewhere.");
        await InsertEventAsync(connection, transaction, sampleEvent, cancellationToken);
        var receipt = await InsertReceiptAsync(connection, transaction, operationId, "sample.append", sampleId, scope.ActorSid, sampleEvent, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await ReadAsync(sampleId, scope, cancellationToken) ?? throw new InvalidOperationException("Sample append was not readable."), receipt);
    }

    public async Task<SampleOperationReceipt?> OperationAsync(Guid operationId, SampleScope scope, CancellationToken cancellationToken = default)
    {
        scope.Validate(); await using var connection = factory.Create();
        return await connection.QuerySingleOrDefaultAsync<SampleOperationReceipt>(new CommandDefinition("SELECT operation_id OperationId,operation_kind Kind,sample_id SampleId,actor_sid ActorSid,recorded_at RecordedAtUtc,payload::text Payload FROM scientific_sample_operations WHERE actor_sid=@actor AND operation_id=@operationId", new { actor = scope.ActorSid, operationId }, cancellationToken: cancellationToken));
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

    private static Task<int> InsertEventAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, SampleEvent value, CancellationToken ct) => connection.ExecuteAsync(new CommandDefinition("INSERT INTO scientific_sample_events(event_id,sample_id,event_kind,label,custodian,location,occurred_at,recorded_at,actor_sid,server_sequence,supersedes_event_id,correction_reason) VALUES (@EventId,@SampleId,@Kind,@Label,@Custodian,@Location,@OccurredAtUtc,@RecordedAtUtc,@ActorSid,@ServerSequence,@SupersedesEventId,@CorrectionReason)", value, transaction, cancellationToken: ct));
    private static async Task<SampleOperationReceipt> InsertReceiptAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid operationId, string kind, Guid sampleId, string actorSid, SampleEvent sampleEvent, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(sampleEvent); var now = DateTimeOffset.UtcNow; var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        await connection.ExecuteAsync(new CommandDefinition("INSERT INTO scientific_sample_operations(actor_sid,operation_id,operation_kind,sample_id,request_hash,payload,recorded_at) VALUES (@actorSid,@operationId,@kind,@sampleId,@hash,CAST(@payload AS jsonb),@now)", new { actorSid, operationId, kind, sampleId, hash, payload, now }, transaction, cancellationToken: ct));
        return new SampleOperationReceipt(operationId, kind, sampleId, actorSid, now, payload);
    }
    private sealed record Row(Guid EventId, Guid SampleId, string Kind, string? Label, string? Custodian, string Location, DateTimeOffset OccurredAtUtc, DateTimeOffset RecordedAtUtc, string ActorSid, long ServerSequence, Guid? SupersedesEventId, string? CorrectionReason)
    { public SampleEvent ToEvent() => new(EventId, SampleId, Enum.Parse<SampleEventKind>(Kind, true), Label, Custodian, Location, OccurredAtUtc, RecordedAtUtc, ActorSid, ServerSequence, SupersedesEventId, CorrectionReason); }
}
