namespace Helix.Orchestration.Agent.Campaigns;

public interface IExternalEvidenceStore
{
    Task<PendingActivity> CreatePendingActivityAsync(
        PendingActivity activity,
        CancellationToken cancellationToken = default);

    Task<PendingActivity?> LoadActivityAsync(
        string campaignId,
        string activityId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingActivity>> LoadRecoveryProjectionAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityAuditFact>> LoadAuditAsync(
        string campaignId,
        string activityId,
        CancellationToken cancellationToken = default);

    Task<PendingActivity?> TryAdvanceAsync(
        PendingActivity expected,
        long expectedRevision,
        ActivityLifecycle nextLifecycle,
        CancellationToken cancellationToken = default);

    Task<EvidenceReceiptResult> RecordReceiptAsync(
        ExternalEvidenceReceipt receipt,
        long expectedRevision,
        CancellationToken cancellationToken = default);
}
