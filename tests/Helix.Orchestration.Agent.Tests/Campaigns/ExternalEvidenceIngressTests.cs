using System.Text.Json;
using Helix.Orchestration.Agent.Api;
using Helix.Orchestration.Agent.Campaigns;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Campaigns;

public sealed class ExternalEvidenceIngressTests
{
    [Fact]
    public async Task Rejects_one_component_subject_mismatch()
    {
        var state = new EvidenceState();
        var store = new FakeEvidenceStore(state);
        var activity = NewActivity();
        await store.CreatePendingActivityAsync(activity);

        var response = await ExternalEvidenceEndpoints.ProcessAsync(
            Request(activity) with { FlowId = activity.FlowId + "-other" }, store);

        Assert.Equal(nameof(EvidenceDisposition.Rejected), response.Disposition);
        Assert.Null(response.Projection);
        var unchanged = await store.LoadActivityAsync(activity.CampaignId, activity.ActivityId);
        Assert.Equal(activity, unchanged);
        Assert.Contains(await store.LoadAuditAsync(activity.CampaignId, activity.ActivityId), fact => fact.Disposition == "Rejected");
    }

    [Fact]
    public async Task Reconciles_recovery_time_signal_once()
    {
        var state = new EvidenceState();
        var beforeRecovery = new FakeEvidenceStore(state);
        var activity = NewActivity();
        await beforeRecovery.CreatePendingActivityAsync(activity);

        // A new store instance represents the Agent's durable-state recovery boundary.
        var recoveredStore = new FakeEvidenceStore(state);
        var request = Request(activity);
        var submissions = await Task.WhenAll(
            ExternalEvidenceEndpoints.ProcessAsync(request, recoveredStore),
            ExternalEvidenceEndpoints.ProcessAsync(request, recoveredStore));

        Assert.Single(submissions, result => !result.Replayed && result.Disposition == nameof(EvidenceDisposition.Accepted));
        Assert.Single(submissions, result => result.Replayed && result.Disposition == nameof(EvidenceDisposition.Accepted));
        var replay = await ExternalEvidenceEndpoints.ProcessAsync(request, new FakeEvidenceStore(state));
        Assert.True(replay.Replayed);
        Assert.Equal(nameof(EvidenceDisposition.Accepted), replay.Disposition);
        Assert.Contains("does not assert offline instrument control or physical completion", replay.Notice);

        var completed = await recoveredStore.LoadActivityAsync(activity.CampaignId, activity.ActivityId);
        Assert.NotNull(completed);
        Assert.Equal(ActivityLifecycle.Completed, completed!.Lifecycle);
        Assert.Equal(activity.Revision + 1, completed.Revision);
        Assert.Equal(PhysicalOutcome.unknown, completed.PhysicalOutcome);
        var audit = await recoveredStore.LoadAuditAsync(activity.CampaignId, activity.ActivityId);
        Assert.Single(audit, fact => fact.Disposition == nameof(EvidenceDisposition.Accepted));
    }

    [Fact]
    public async Task Reused_delivery_with_altered_payload_is_rejected()
    {
        var store = new FakeEvidenceStore(new EvidenceState());
        var activity = NewActivity();
        await store.CreatePendingActivityAsync(activity);
        var request = Request(activity);

        var accepted = await ExternalEvidenceEndpoints.ProcessAsync(request, store);
        var altered = await ExternalEvidenceEndpoints.ProcessAsync(request with { Payload = Payload("{\"result\":\"altered\"}") }, store);

        Assert.Equal(nameof(EvidenceDisposition.Accepted), accepted.Disposition);
        Assert.Equal(nameof(EvidenceDisposition.Rejected), altered.Disposition);
        Assert.False(altered.Replayed);
        var current = await store.LoadActivityAsync(activity.CampaignId, activity.ActivityId);
        Assert.Equal(activity.Revision + 1, current!.Revision);
        Assert.Equal(ActivityLifecycle.Completed, current.Lifecycle);
    }

    [Fact]
    public async Task Stale_pending_revision_is_audited_without_advancement()
    {
        var store = new FakeEvidenceStore(new EvidenceState());
        var activity = NewActivity();
        await store.CreatePendingActivityAsync(activity);

        var response = await ExternalEvidenceEndpoints.ProcessAsync(
            Request(activity) with { ExpectedRevision = activity.Revision + 1 }, store);

        Assert.Equal(nameof(EvidenceDisposition.Stale), response.Disposition);
        var unchanged = await store.LoadActivityAsync(activity.CampaignId, activity.ActivityId);
        Assert.Equal(activity, unchanged);
        Assert.Contains(await store.LoadAuditAsync(activity.CampaignId, activity.ActivityId), fact => fact.Disposition == "Stale");
    }

    private static PendingActivity NewActivity()
    {
        var id = Guid.NewGuid().ToString("N");
        return new PendingActivity($"campaign-{id}", $"activity-{id}", $"flow-{id}", $"step-{id}",
            $"command-{id}", 1, ActivityLifecycle.PendingExternalEvidence, PhysicalOutcome.unknown);
    }

    private static ExternalEvidenceRequest Request(PendingActivity activity) => new(
        activity.CampaignId,
        activity.ActivityId,
        activity.FlowId,
        activity.AwaitingStepId,
        activity.Revision,
        "instrument-feed",
        Guid.NewGuid().ToString("N"),
        Payload("{\"result\":\"observed\"}"));

    private static JsonElement Payload(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class EvidenceState
    {
        public object Gate { get; } = new();
        public Dictionary<(string CampaignId, string ActivityId), PendingActivity> Activities { get; } = new();
        public Dictionary<(string Source, string EventId), ExternalEvidenceReceipt> Receipts { get; } = new();
        public List<ActivityAuditFact> Audit { get; } = new();
        public long Sequence { get; set; }
    }

    private sealed class FakeEvidenceStore(EvidenceState state) : IExternalEvidenceStore
    {
        public Task<PendingActivity> CreatePendingActivityAsync(
            PendingActivity activity,
            CancellationToken cancellationToken = default)
        {
            lock (state.Gate)
            {
                state.Activities.Add((activity.CampaignId, activity.ActivityId), activity);
                AddAudit(activity, "Created", null);
                return Task.FromResult(activity);
            }
        }

        public Task<PendingActivity?> LoadActivityAsync(
            string campaignId,
            string activityId,
            CancellationToken cancellationToken = default)
        {
            lock (state.Gate)
                return Task.FromResult(state.Activities.GetValueOrDefault((campaignId, activityId)));
        }

        public Task<IReadOnlyList<PendingActivity>> LoadRecoveryProjectionAsync(CancellationToken cancellationToken = default)
        {
            lock (state.Gate)
                return Task.FromResult<IReadOnlyList<PendingActivity>>(state.Activities.Values.ToArray());
        }

        public Task<IReadOnlyList<ActivityAuditFact>> LoadAuditAsync(
            string campaignId,
            string activityId,
            CancellationToken cancellationToken = default)
        {
            lock (state.Gate)
                return Task.FromResult<IReadOnlyList<ActivityAuditFact>>(state.Audit
                    .Where(fact => fact.CampaignId == campaignId && fact.ActivityId == activityId).ToArray());
        }

        public Task<PendingActivity?> TryAdvanceAsync(
            PendingActivity expected,
            long expectedRevision,
            ActivityLifecycle nextLifecycle,
            CancellationToken cancellationToken = default)
        {
            lock (state.Gate)
            {
                if (!state.Activities.TryGetValue((expected.CampaignId, expected.ActivityId), out var current) ||
                    !SameSubject(current, expected) || current.Revision != expectedRevision ||
                    current.Lifecycle != ActivityLifecycle.PendingExternalEvidence)
                    return Task.FromResult<PendingActivity?>(null);

                var advanced = current with { Revision = current.Revision + 1, Lifecycle = nextLifecycle };
                state.Activities[(current.CampaignId, current.ActivityId)] = advanced;
                AddAudit(advanced, "Advanced", null);
                return Task.FromResult<PendingActivity?>(advanced);
            }
        }

        public Task<EvidenceReceiptResult> RecordReceiptAsync(
            ExternalEvidenceReceipt receipt,
            long expectedRevision,
            CancellationToken cancellationToken = default)
        {
            lock (state.Gate)
            {
                var key = (receipt.Source, receipt.EventId);
                if (state.Receipts.TryGetValue(key, out var existing))
                {
                    var sameDelivery = SameDelivery(existing, receipt);
                    var current = state.Activities.GetValueOrDefault((receipt.CampaignId, receipt.ActivityId));
                    var disposition = sameDelivery ? existing.Disposition : EvidenceDisposition.Rejected;
                    if (sameDelivery && current is not null)
                        AddAudit(current, "Duplicate", receipt);
                    else if (current is not null)
                        AddAudit(current, "Rejected", receipt);
                    return Task.FromResult(new EvidenceReceiptResult(
                        receipt with { Disposition = disposition },
                        current is not null && SameSubject(current, receipt) ? current : null,
                        sameDelivery));
                }

                var activity = state.Activities.GetValueOrDefault((receipt.CampaignId, receipt.ActivityId));
                var target = activity is not null && SameSubject(activity, receipt) ? activity : null;
                var resultDisposition = activity is null || !SameSubject(activity, receipt)
                    ? EvidenceDisposition.Rejected
                    : activity.Revision != expectedRevision
                        ? EvidenceDisposition.Stale
                        : activity.Lifecycle != ActivityLifecycle.PendingExternalEvidence
                            ? EvidenceDisposition.Rejected
                            : EvidenceDisposition.Accepted;

                if (resultDisposition == EvidenceDisposition.Accepted)
                {
                    target = activity! with { Revision = activity!.Revision + 1, Lifecycle = ActivityLifecycle.Completed };
                    state.Activities[(target.CampaignId, target.ActivityId)] = target;
                }

                var stored = receipt with { Disposition = resultDisposition };
                state.Receipts.Add(key, stored);
                AddAudit(target ?? activity ?? ToPlaceholder(receipt), resultDisposition.ToString(), receipt);
                return Task.FromResult(new EvidenceReceiptResult(stored, target, IsReplay: false));
            }
        }

        private void AddAudit(PendingActivity activity, string disposition, ExternalEvidenceReceipt? receipt)
        {
            state.Audit.Add(new ActivityAuditFact(
                ++state.Sequence,
                activity.CampaignId,
                activity.ActivityId,
                activity.Revision,
                disposition,
                receipt?.Source,
                receipt?.EventId,
                receipt?.PayloadHash,
                DateTimeOffset.UtcNow));
        }

        private static PendingActivity ToPlaceholder(ExternalEvidenceReceipt receipt) => new(
            receipt.CampaignId, receipt.ActivityId, receipt.FlowId, receipt.AwaitingStepId,
            "unknown-command", 1, ActivityLifecycle.PendingExternalEvidence, PhysicalOutcome.unknown);

        private static bool SameSubject(PendingActivity activity, ExternalEvidenceReceipt receipt) =>
            activity.CampaignId == receipt.CampaignId && activity.ActivityId == receipt.ActivityId &&
            activity.FlowId == receipt.FlowId && activity.AwaitingStepId == receipt.AwaitingStepId;

        private static bool SameSubject(PendingActivity activity, PendingActivity expected) =>
            activity.CampaignId == expected.CampaignId && activity.ActivityId == expected.ActivityId &&
            activity.FlowId == expected.FlowId && activity.AwaitingStepId == expected.AwaitingStepId;

        private static bool SameDelivery(ExternalEvidenceReceipt stored, ExternalEvidenceReceipt incoming) =>
            stored.PayloadHash == incoming.PayloadHash && stored.CampaignId == incoming.CampaignId &&
            stored.ActivityId == incoming.ActivityId && stored.FlowId == incoming.FlowId &&
            stored.AwaitingStepId == incoming.AwaitingStepId;
    }
}
