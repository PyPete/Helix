namespace Helix.Orchestration.Core.Samples;

public sealed record SampleProjection(
    SampleIdentity Identity,
    string? Label,
    string? Custodian,
    string Location,
    long ServerSequence,
    Guid LastEventId)
{
    public static SampleProjection Reconstruct(SampleIdentity identity, IEnumerable<SampleEvent> history)
    {
        SampleFactValidation.ValidateIdentity(identity);
        // Occurrence time is reported evidence and can be backdated.  The server
        // sequence is the authoritative ordering for custody state.
        var events = history.OrderBy(x => x.ServerSequence).ToArray();
        if (events.Length == 0) throw new InvalidOperationException("A confirmed sample requires immutable event history.");

        var current = events[0];
        SampleFactValidation.ValidateEvent(current, DateTimeOffset.UtcNow);
        if (current.Kind != SampleEventKind.Confirmed || current.SampleId != identity.SampleId)
            throw new InvalidOperationException("The first event must confirm this sample.");

        foreach (var next in events.Skip(1))
        {
            SampleFactValidation.ValidateEvent(next, DateTimeOffset.UtcNow);
            if (next.SampleId != identity.SampleId || next.ServerSequence <= current.ServerSequence)
                throw new InvalidOperationException("Sample history must be a single ordered stream.");
            if (next.Kind == SampleEventKind.Correction && !events.TakeWhile(x => x.EventId != next.EventId).Any(x => x.EventId == next.SupersedesEventId))
                throw new InvalidOperationException("Corrections must supersede a prior event in the same stream.");
            current = next;
        }

        return new SampleProjection(identity, current.Label, current.Custodian, current.Location, current.ServerSequence, current.EventId);
    }
}
