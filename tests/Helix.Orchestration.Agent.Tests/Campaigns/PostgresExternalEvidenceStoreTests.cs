using Helix.Orchestration.Agent.Campaigns;
using Npgsql;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Campaigns;

[Collection("External evidence PostgreSQL")]
public sealed class PostgresExternalEvidenceStoreTests
{
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

    [Fact]
    public async Task Fresh_store_recovers_pending_activity_and_audit()
    {
        var connectionString = await GetAvailableConnectionStringAsync();

        var store = new PostgresExternalEvidenceStore(connectionString);
        await store.InitializeAsync();
        var activity = NewActivity();
        await store.CreatePendingActivityAsync(activity);

        var freshStore = new PostgresExternalEvidenceStore(connectionString);
        var recovered = await freshStore.LoadActivityAsync(activity.CampaignId, activity.ActivityId);
        var projection = await freshStore.LoadRecoveryProjectionAsync();
        var audit = await freshStore.LoadAuditAsync(activity.CampaignId, activity.ActivityId);

        Assert.Equal(activity, recovered);
        Assert.Contains(activity, projection);
        Assert.Single(audit);
        Assert.Equal("Created", audit[0].Disposition);
    }

    [Fact]
    public async Task Concurrent_duplicate_receipts_are_deduplicated_with_one_transition()
    {
        var connectionString = await GetAvailableConnectionStringAsync();

        var store = new PostgresExternalEvidenceStore(connectionString);
        await store.InitializeAsync();
        var activity = NewActivity();
        await store.CreatePendingActivityAsync(activity);
        var receipt = new ExternalEvidenceReceipt("instrument-feed", Guid.NewGuid().ToString("N"), "sha256:abc",
            activity.CampaignId, activity.ActivityId, activity.FlowId, activity.AwaitingStepId, EvidenceDisposition.Rejected);

        var results = await Task.WhenAll(
            store.RecordReceiptAsync(receipt, activity.Revision),
            store.RecordReceiptAsync(receipt, activity.Revision));

        Assert.Single(results, result => !result.IsReplay);
        Assert.Single(results, result => result.IsReplay);
        Assert.All(results, result => Assert.Equal(EvidenceDisposition.Accepted, result.Receipt.Disposition));
        var recovered = await store.LoadActivityAsync(activity.CampaignId, activity.ActivityId);
        Assert.NotNull(recovered);
        Assert.Equal(ActivityLifecycle.Completed, recovered!.Lifecycle);
        Assert.Equal(activity.Revision + 1, recovered.Revision);
        Assert.Single(await LoadReceiptsAsync(connectionString, receipt.Source, receipt.EventId));
        Assert.Equal(3, (await store.LoadAuditAsync(activity.CampaignId, activity.ActivityId)).Count);
    }

    [Fact]
    public async Task Conditional_advance_requires_all_subject_ids_and_expected_revision()
    {
        var connectionString = await GetAvailableConnectionStringAsync();

        var store = new PostgresExternalEvidenceStore(connectionString);
        await store.InitializeAsync();
        var activity = NewActivity();
        await store.CreatePendingActivityAsync(activity);

        Assert.Null(await store.TryAdvanceAsync(activity with { FlowId = "other-flow" }, activity.Revision, ActivityLifecycle.Completed));
        Assert.Null(await store.TryAdvanceAsync(activity, activity.Revision + 1, ActivityLifecycle.Completed));
        var advanced = await store.TryAdvanceAsync(activity, activity.Revision, ActivityLifecycle.Completed);

        Assert.NotNull(advanced);
        Assert.Equal(ActivityLifecycle.Completed, advanced!.Lifecycle);
        Assert.Equal(PhysicalOutcome.unknown, advanced.PhysicalOutcome);
        Assert.Null(await store.TryAdvanceAsync(activity, activity.Revision, ActivityLifecycle.Completed));
    }

    private static PendingActivity NewActivity()
    {
        var id = Guid.NewGuid().ToString("N");
        return new PendingActivity($"campaign-{id}", $"activity-{id}", $"flow-{id}", $"step-{id}",
            $"command-{id}", 1, ActivityLifecycle.PendingExternalEvidence, PhysicalOutcome.unknown);
    }

    private static async Task<string> GetAvailableConnectionStringAsync()
    {
        var connectionString = TestConnectionString;
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            return connectionString;
        }
        catch (NpgsqlException exception)
        {
            throw new InvalidOperationException(
                "PostgreSQL is required for durable campaign evidence integration tests; configure HELIX_TEST_POSTGRES or start the test database.",
                exception);
        }
    }

    private static async Task<string[]> LoadReceiptsAsync(string connectionString, string source, string eventId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT disposition FROM external_evidence_receipts WHERE source = @source AND event_id = @eventId",
            connection);
        command.Parameters.AddWithValue("source", source);
        command.Parameters.AddWithValue("eventId", eventId);
        var result = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(reader.GetString(0));
        return result.ToArray();
    }
}
