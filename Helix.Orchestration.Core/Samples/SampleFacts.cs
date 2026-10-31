namespace Helix.Orchestration.Core.Samples;

public enum SampleOrigin
{
    Collection,
    Transformation
}

public enum SampleEventKind
{
    Confirmed,
    Relabeled,
    CustodyMoved,
    Correction
}

public sealed record SampleIdentity(Guid SampleId, SampleOrigin Origin, IReadOnlyList<Guid> ParentSampleIds)
{
    public void Validate()
    {
        if (SampleId == Guid.Empty) throw new ArgumentException("A sample requires a stable identifier.", nameof(SampleId));
        if (Origin == SampleOrigin.Collection && ParentSampleIds.Count != 0)
            throw new ArgumentException("Collected samples cannot have parent samples.", nameof(ParentSampleIds));
    }
}

public sealed record SampleEvent(
    Guid EventId,
    Guid SampleId,
    SampleEventKind Kind,
    string? Label,
    string? Custodian,
    string Location,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset RecordedAtUtc,
    string ActorSid,
    long ServerSequence,
    Guid? SupersedesEventId = null,
    string? CorrectionReason = null);

public sealed record SampleConfirmation(
    string Location,
    DateTimeOffset OccurredAtUtc,
    string? Label = null,
    string? Custodian = null,
    Guid? PlannedReferenceId = null);

public static class SampleFactValidation
{
    public static void ValidateIdentity(SampleIdentity identity) => identity.Validate();

    public static void ValidateConfirmation(SampleConfirmation confirmation, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(confirmation.Location)) throw new ArgumentException("A collection location is required.", nameof(confirmation));
        if (confirmation.OccurredAtUtc > now) throw new ArgumentOutOfRangeException(nameof(confirmation), "Future occurrence times are not evidence.");
    }

    public static void ValidateEvent(SampleEvent sampleEvent, DateTimeOffset now)
    {
        if (sampleEvent.EventId == Guid.Empty || sampleEvent.SampleId == Guid.Empty) throw new ArgumentException("Sample events require stable identifiers.", nameof(sampleEvent));
        if (sampleEvent.ServerSequence <= 0) throw new ArgumentOutOfRangeException(nameof(sampleEvent), "Server sequence must be positive.");
        if (sampleEvent.OccurredAtUtc > now) throw new ArgumentOutOfRangeException(nameof(sampleEvent), "Future occurrence times are not evidence.");
        if (string.IsNullOrWhiteSpace(sampleEvent.Location) || string.IsNullOrWhiteSpace(sampleEvent.ActorSid)) throw new ArgumentException("Sample event location and actor are required.", nameof(sampleEvent));
        if (sampleEvent.Kind == SampleEventKind.Correction && (sampleEvent.SupersedesEventId is null || string.IsNullOrWhiteSpace(sampleEvent.CorrectionReason)))
            throw new ArgumentException("Corrections require a prior event and reason.", nameof(sampleEvent));
    }
}
