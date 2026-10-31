using Helix.Orchestration.Core.Samples;

namespace Helix.Orchestration.Agent.Samples;

/// <summary>Admits only explicit, authenticated sample facts; it has no execution or dispatch dependency.</summary>
public sealed class SampleService(ISampleRepository repository)
{
    public Task<IReadOnlyList<SampleProjection>> ListAsync(string actorSid, int pageSize, CancellationToken ct) =>
        repository.ListAsync(new SampleScope(actorSid), pageSize, ct);

    public Task<SampleProjection?> ReadAsync(Guid sampleId, string actorSid, CancellationToken ct) =>
        repository.ReadAsync(sampleId, new SampleScope(actorSid), ct);

    public Task<IReadOnlyList<SampleEvent>> EventsAsync(Guid sampleId, string actorSid, long? afterSequence, int pageSize, CancellationToken ct) =>
        repository.EventsAsync(sampleId, new SampleScope(actorSid), afterSequence, pageSize, ct);

    public async Task<(SampleProjection Projection, SampleOperationReceipt Receipt, bool Replayed)> ConfirmAsync(SampleConfirmation confirmation, string actorSid, Guid operationId, CancellationToken ct)
    {
        var scope = new SampleScope(actorSid);
        scope.Validate();
        if (operationId == Guid.Empty) throw new ArgumentException("An operation identifier is required.", nameof(operationId));
        SampleFactValidation.ValidateConfirmation(confirmation, DateTimeOffset.UtcNow);
        var prior = await repository.OperationAsync(operationId, scope, ct);
        if (prior is not null)
        {
            if (prior.Kind != "sample.confirm") throw new InvalidOperationException("Operation identity was reused for a different sample operation.");
            var projection = await repository.ReadAsync(prior.SampleId, scope, ct) ?? throw new InvalidOperationException("Committed sample receipt has no readable sample.");
            return (projection, prior, true);
        }

        var committed = await repository.ConfirmAsync(confirmation, scope, operationId, ct);
        return (committed.Projection, committed.Receipt, false);
    }

    public async Task<(SampleProjection Projection, SampleOperationReceipt Receipt)> AppendAsync(Guid sampleId, SampleEventKind kind, string? label, string? custodian, string location, DateTimeOffset occurredAtUtc, long expectedSequence, Guid? supersedesEventId, string? correctionReason, string actorSid, Guid operationId, CancellationToken ct)
    {
        var scope = new SampleScope(actorSid);
        if (sampleId == Guid.Empty || operationId == Guid.Empty) throw new ArgumentException("Sample and operation identifiers are required.");
        if (expectedSequence < 1) throw new ArgumentOutOfRangeException(nameof(expectedSequence));
        var @event = new SampleEvent(Guid.NewGuid(), sampleId, kind, label, custodian, location, occurredAtUtc, DateTimeOffset.UtcNow, actorSid, expectedSequence + 1, supersedesEventId, correctionReason);
        return await repository.AppendAsync(sampleId, @event, expectedSequence, scope, operationId, ct);
    }

    public Task<SampleOperationReceipt?> OperationAsync(Guid operationId, string actorSid, CancellationToken ct) =>
        repository.OperationAsync(operationId, new SampleScope(actorSid), ct);
}
