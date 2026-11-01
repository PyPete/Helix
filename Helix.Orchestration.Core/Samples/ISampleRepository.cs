namespace Helix.Orchestration.Core.Samples;

public sealed record SampleScope(string ActorSid)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ActorSid)) throw new ArgumentException("An authenticated actor is required.", nameof(ActorSid));
    }
}

public sealed record SampleOperationReceipt(Guid OperationId, string Kind, Guid SampleId, string ActorSid, DateTimeOffset RecordedAtUtc, string Payload);
public sealed record SamplePage(IReadOnlyList<SampleProjection> Items, long? NextSequence);

public interface ISampleRepository
{
    Task<(SampleProjection Projection, SampleOperationReceipt Receipt)> ConfirmAsync(SampleConfirmation confirmation, SampleScope scope, Guid operationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SampleProjection>> ListAsync(SampleScope scope, int pageSize = 25, CancellationToken cancellationToken = default);
    Task<SampleProjection?> ReadAsync(Guid sampleId, SampleScope scope, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SampleEvent>> EventsAsync(Guid sampleId, SampleScope scope, long? afterSequence = null, int pageSize = 25, CancellationToken cancellationToken = default);
    Task<(SampleProjection Projection, SampleOperationReceipt Receipt)> AppendAsync(Guid sampleId, SampleEvent sampleEvent, long expectedSequence, SampleScope scope, Guid operationId, CancellationToken cancellationToken = default);
    Task<SampleOperationReceipt?> OperationAsync(Guid operationId, SampleScope scope, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SampleProjection> Children, SampleOperationReceipt Receipt)> TransformAsync(SampleTransformation transformation, SampleScope scope, Guid operationId, CancellationToken cancellationToken = default)
        => Task.FromException<(IReadOnlyList<SampleProjection>, SampleOperationReceipt)>(new NotSupportedException("Sample transformations are not implemented by this repository."));
    Task<SampleLineage> LineageAsync(Guid sampleId, SampleLineageDirection direction, SampleScope scope, Guid? continuation = null, int pageSize = 25, CancellationToken cancellationToken = default)
        => Task.FromException<SampleLineage>(new NotSupportedException("Sample lineage traversal is not implemented by this repository."));
}
