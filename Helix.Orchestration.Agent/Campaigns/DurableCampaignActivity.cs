namespace Helix.Orchestration.Agent.Campaigns;

public enum ActivityLifecycle
{
    PendingExternalEvidence,
    Completed
}

public enum PhysicalOutcome
{
    unknown,
    uncertain
}

public enum EvidenceDisposition
{
    Accepted,
    Duplicate,
    Rejected,
    Stale
}

public sealed record PendingActivity(
    string CampaignId,
    string ActivityId,
    string FlowId,
    string AwaitingStepId,
    string CommandId,
    long Revision,
    ActivityLifecycle Lifecycle,
    PhysicalOutcome PhysicalOutcome);

public sealed record ExternalEvidenceReceipt(
    string Source,
    string EventId,
    string PayloadHash,
    string CampaignId,
    string ActivityId,
    string FlowId,
    string AwaitingStepId,
    EvidenceDisposition Disposition);

public sealed record ActivityAuditFact(
    long SequenceId,
    string CampaignId,
    string ActivityId,
    long Revision,
    string Disposition,
    string? Source,
    string? EventId,
    string? PayloadHash,
    DateTimeOffset OccurredAt);

public sealed record EvidenceReceiptResult(
    ExternalEvidenceReceipt Receipt,
    PendingActivity? Activity,
    bool IsReplay);
