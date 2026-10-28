using System.Text.Json;
using System.Net;
using System.Net.Http.Json;
using Helix.Orchestration.Agent.Api;
using Helix.Orchestration.Agent.Campaigns;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Campaigns;

[Collection("External evidence PostgreSQL")]
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
            Request(activity) with { FlowId = activity.FlowId + "-other" }, "instrument-feed", store);

        Assert.Equal(nameof(EvidenceDisposition.Rejected), response.Disposition);
        Assert.Null(response.Projection);
        var unchanged = await store.LoadActivityAsync(activity.CampaignId, activity.ActivityId);
        Assert.Equal(activity, unchanged);
        Assert.Contains(await store.LoadAuditAsync(activity.CampaignId, activity.ActivityId), fact => fact.Disposition == "Rejected");
    }

    [Fact]
    public async Task Reconciles_recovery_time_signal_once()
    {
        var connectionString = await GetRequiredConnectionStringAsync();
        var beforeRecovery = new PostgresExternalEvidenceStore(connectionString);
        await beforeRecovery.InitializeAsync();
        var activity = NewActivity();
        await beforeRecovery.CreatePendingActivityAsync(activity);

        // A fresh store reloads durable state while the correlated signal arrives during recovery.
        var recoveredStore = new PostgresExternalEvidenceStore(connectionString);
        var request = Request(activity);
        var recoveryTask = recoveredStore.LoadRecoveryProjectionAsync();
        var submissionsTask = Task.WhenAll(
            ExternalEvidenceEndpoints.ProcessAsync(request, "instrument-feed", recoveredStore),
            ExternalEvidenceEndpoints.ProcessAsync(request, "instrument-feed", recoveredStore));
        await Task.WhenAll(recoveryTask, submissionsTask);
        var recoveredProjection = await recoveryTask;
        var recoveredActivity = recoveredProjection.SingleOrDefault(candidate =>
            candidate.CampaignId == activity.CampaignId && candidate.ActivityId == activity.ActivityId);
        if (recoveredActivity is not null)
            Assert.Equal(activity, recoveredActivity);
        var submissions = await submissionsTask;

        Assert.Single(submissions, result => !result.Replayed && result.Disposition == nameof(EvidenceDisposition.Accepted));
        Assert.Single(submissions, result => result.Replayed && result.Disposition == nameof(EvidenceDisposition.Accepted));
        var replay = await ExternalEvidenceEndpoints.ProcessAsync(
            request, "instrument-feed", new PostgresExternalEvidenceStore(connectionString));
        Assert.True(replay.Replayed);
        Assert.Equal(nameof(EvidenceDisposition.Accepted), replay.Disposition);
        Assert.Contains("does not assert offline instrument control or physical completion", replay.Notice);

        var afterRecovery = new PostgresExternalEvidenceStore(connectionString);
        var completed = await afterRecovery.LoadActivityAsync(activity.CampaignId, activity.ActivityId);
        Assert.NotNull(completed);
        Assert.Equal(ActivityLifecycle.Completed, completed!.Lifecycle);
        Assert.Equal(activity.Revision + 1, completed.Revision);
        Assert.Equal(activity.CommandId, completed.CommandId);
        Assert.Equal(PhysicalOutcome.unknown, completed.PhysicalOutcome);
        var audit = await afterRecovery.LoadAuditAsync(activity.CampaignId, activity.ActivityId);
        Assert.Single(audit, fact => fact.Disposition == nameof(EvidenceDisposition.Accepted));
        Assert.Equal(2, audit.Count(fact => fact.Disposition == nameof(EvidenceDisposition.Duplicate)));
        Assert.Equal(1, await CountReceiptsAsync(connectionString, request.Source, request.EventId));
    }

    [Fact]
    public async Task Ingress_authenticates_source_and_binds_it_to_the_request()
    {
        var state = new EvidenceState();
        var store = new FakeEvidenceStore(state);
        var activity = NewActivity();
        await store.CreatePendingActivityAsync(activity);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ExternalEvidence:Sources:instrument-feed"] = "test-source-token"
        });
        builder.Services.AddExternalEvidenceAuthentication();
        builder.Services.AddSingleton<IExternalEvidenceStore>(store);
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapExternalEvidenceEndpoints();
        await app.StartAsync();

        using var client = app.GetTestClient();
        var request = Request(activity);
        var unauthenticated = await client.PostAsJsonAsync("/external-evidence", request);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        client.DefaultRequestHeaders.Add(ExternalEvidenceSourceAuthenticationHandler.SourceHeader, "instrument-feed");
        client.DefaultRequestHeaders.Add(ExternalEvidenceSourceAuthenticationHandler.TokenHeader, "wrong-token");
        var invalidCredential = await client.PostAsJsonAsync("/external-evidence", request);
        Assert.Equal(HttpStatusCode.Unauthorized, invalidCredential.StatusCode);

        client.DefaultRequestHeaders.Remove(ExternalEvidenceSourceAuthenticationHandler.TokenHeader);
        client.DefaultRequestHeaders.Add(ExternalEvidenceSourceAuthenticationHandler.TokenHeader, "test-source-token");
        var forgedSource = await client.PostAsJsonAsync(
            "/external-evidence", request with { Source = "forged-feed" });
        Assert.Equal(HttpStatusCode.Forbidden, forgedSource.StatusCode);

        var accepted = await client.PostAsJsonAsync("/external-evidence", request);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var completed = await store.LoadActivityAsync(activity.CampaignId, activity.ActivityId);
        Assert.Equal(ActivityLifecycle.Completed, completed!.Lifecycle);
        Assert.Single(state.Receipts);
    }

    [Fact]
    public async Task Reused_delivery_with_altered_payload_is_rejected()
    {
        var store = new FakeEvidenceStore(new EvidenceState());
        var activity = NewActivity();
        await store.CreatePendingActivityAsync(activity);
        var request = Request(activity);

        var accepted = await ExternalEvidenceEndpoints.ProcessAsync(request, "instrument-feed", store);
        var altered = await ExternalEvidenceEndpoints.ProcessAsync(
            request with { Payload = Payload("{\"result\":\"altered\"}") }, "instrument-feed", store);

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
            Request(activity) with { ExpectedRevision = activity.Revision + 1 }, "instrument-feed", store);

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

    private static string TestConnectionString =>
        Environment.GetEnvironmentVariable("HELIX_TEST_POSTGRES") ??
        Environment.GetEnvironmentVariable("ConnectionStrings__HelixOrchestration") ??
        (Environment.GetEnvironmentVariable("PGHOST") is { Length: > 0 } host
            ? new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.TryParse(Environment.GetEnvironmentVariable("PGPORT"), out var port) ? port : 5432,
                Database = Environment.GetEnvironmentVariable("PGDATABASE") ?? "helix_orchestration",
                Username = Environment.GetEnvironmentVariable("PGUSER") ?? "helix",
                Password = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "helix_dev_password"
            }.ConnectionString ?? throw new InvalidOperationException("A PostgreSQL connection string is required.")
            : "Host=localhost;Port=5432;Database=helix_orchestration;Username=helix;Password=helix_dev_password");

    private static async Task<string> GetRequiredConnectionStringAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(TestConnectionString);
            await connection.OpenAsync();
            return TestConnectionString;
        }
        catch (NpgsqlException exception)
        {
            throw new InvalidOperationException(
                "PostgreSQL is required for durable recovery verification; configure HELIX_TEST_POSTGRES or start the test database.",
                exception);
        }
    }

    private static async Task<int> CountReceiptsAsync(string connectionString, string source, string eventId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM external_evidence_receipts WHERE source = @source AND event_id = @eventId",
            connection);
        command.Parameters.AddWithValue("source", source);
        command.Parameters.AddWithValue("eventId", eventId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
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

[CollectionDefinition("External evidence PostgreSQL", DisableParallelization = true)]
public sealed class ExternalEvidencePostgresCollection { }
