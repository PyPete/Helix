namespace Helix.Orchestration.Core.ScientificSetup;
public sealed record ScientificRevision(Guid RevisionId, Guid ApparatusId, string Name, string ContentHash, string ActorSid, DateTimeOffset RecordedAtUtc, string CanonicalContent);
public sealed record ScientificOperationReceipt(Guid OperationId, string Kind, Guid ResourceId, string ActorSid, DateTimeOffset RecordedAtUtc, string Payload);
