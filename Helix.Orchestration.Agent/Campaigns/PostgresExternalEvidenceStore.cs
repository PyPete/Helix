using Dapper;
using Npgsql;

namespace Helix.Orchestration.Agent.Campaigns;

public sealed class PostgresExternalEvidenceStore(string connectionString) : IExternalEvidenceStore
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Keep existing installations current as well as fresh databases initialized from 003.
        const string sql = """
            CREATE TABLE IF NOT EXISTS bounded_campaign_activities (
                campaign_id TEXT NOT NULL,
                activity_id TEXT NOT NULL,
                flow_id TEXT NOT NULL,
                awaiting_step_id TEXT NOT NULL,
                command_id TEXT NOT NULL,
                revision BIGINT NOT NULL CHECK (revision > 0),
                lifecycle TEXT NOT NULL CHECK (lifecycle IN ('PendingExternalEvidence', 'Completed')),
                physical_outcome TEXT NOT NULL CHECK (physical_outcome IN ('unknown', 'uncertain')),
                created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
                CONSTRAINT pk_bounded_campaign_activities PRIMARY KEY (campaign_id, activity_id),
                CONSTRAINT uq_bounded_campaign_activities_campaign UNIQUE (campaign_id),
                CONSTRAINT uq_bounded_campaign_activities_subject UNIQUE (campaign_id, activity_id, flow_id, awaiting_step_id),
                CONSTRAINT uq_bounded_campaign_activities_command UNIQUE (command_id)
            );
            CREATE TABLE IF NOT EXISTS external_evidence_receipts (
                source TEXT NOT NULL,
                event_id TEXT NOT NULL,
                payload_hash TEXT NOT NULL,
                campaign_id TEXT NOT NULL,
                activity_id TEXT NOT NULL,
                flow_id TEXT NOT NULL,
                awaiting_step_id TEXT NOT NULL,
                disposition TEXT NOT NULL CHECK (disposition IN ('Accepted', 'Duplicate', 'Rejected', 'Stale')),
                received_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
                CONSTRAINT pk_external_evidence_receipts PRIMARY KEY (source, event_id)
            );
            CREATE INDEX IF NOT EXISTS ix_external_evidence_receipts_subject
                ON external_evidence_receipts (campaign_id, activity_id, flow_id, awaiting_step_id);
            CREATE TABLE IF NOT EXISTS bounded_campaign_activity_audit (
                sequence_id BIGSERIAL PRIMARY KEY,
                campaign_id TEXT NOT NULL,
                activity_id TEXT NOT NULL,
                revision BIGINT NOT NULL,
                disposition TEXT NOT NULL,
                source TEXT NULL,
                event_id TEXT NULL,
                payload_hash TEXT NULL,
                occurred_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
            );
            CREATE INDEX IF NOT EXISTS ix_bounded_campaign_activity_audit_subject
                ON bounded_campaign_activity_audit (campaign_id, activity_id, sequence_id);
            CREATE OR REPLACE FUNCTION reject_bounded_campaign_activity_audit_mutation()
            RETURNS trigger AS $$
            BEGIN
                RAISE EXCEPTION 'bounded campaign activity audit facts are immutable';
            END;
            $$ LANGUAGE plpgsql;
            DROP TRIGGER IF EXISTS trg_bounded_campaign_activity_audit_immutable
                ON bounded_campaign_activity_audit;
            CREATE TRIGGER trg_bounded_campaign_activity_audit_immutable
                BEFORE UPDATE OR DELETE ON bounded_campaign_activity_audit
                FOR EACH ROW EXECUTE FUNCTION reject_bounded_campaign_activity_audit_mutation();
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<PendingActivity> CreatePendingActivityAsync(
        PendingActivity activity,
        CancellationToken cancellationToken = default)
    {
        ValidateActivity(activity);
        if (activity.Lifecycle != ActivityLifecycle.PendingExternalEvidence)
            throw new ArgumentException("A new bounded activity must be pending external evidence.", nameof(activity));

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string insert = """
            INSERT INTO bounded_campaign_activities
                (campaign_id, activity_id, flow_id, awaiting_step_id, command_id, revision, lifecycle, physical_outcome)
            VALUES
                (@CampaignId, @ActivityId, @FlowId, @AwaitingStepId, @CommandId, @Revision, @Lifecycle, @PhysicalOutcome);
            """;
        await connection.ExecuteAsync(new CommandDefinition(insert, new
        {
            activity.CampaignId,
            activity.ActivityId,
            activity.FlowId,
            activity.AwaitingStepId,
            activity.CommandId,
            activity.Revision,
            Lifecycle = activity.Lifecycle.ToString(),
            PhysicalOutcome = activity.PhysicalOutcome.ToString()
        }, transaction, cancellationToken: cancellationToken));

        await InsertAuditAsync(connection, transaction, activity, "Created", null, null, null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return activity;
    }

    public async Task<PendingActivity?> LoadActivityAsync(
        string campaignId,
        string activityId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT campaign_id AS "CampaignId", activity_id AS "ActivityId", flow_id AS "FlowId",
                   awaiting_step_id AS "AwaitingStepId", command_id AS "CommandId", revision AS "Revision",
                   lifecycle AS "Lifecycle", physical_outcome AS "PhysicalOutcome"
            FROM bounded_campaign_activities
            WHERE campaign_id = @campaignId AND activity_id = @activityId;
            """;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ActivityRow>(
            new CommandDefinition(sql, new { campaignId, activityId }, cancellationToken: cancellationToken));
        return row?.ToActivity();
    }

    public async Task<IReadOnlyList<PendingActivity>> LoadRecoveryProjectionAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT campaign_id AS "CampaignId", activity_id AS "ActivityId", flow_id AS "FlowId",
                   awaiting_step_id AS "AwaitingStepId", command_id AS "CommandId", revision AS "Revision",
                   lifecycle AS "Lifecycle", physical_outcome AS "PhysicalOutcome"
            FROM bounded_campaign_activities
            WHERE lifecycle = 'PendingExternalEvidence'
            ORDER BY campaign_id;
            """;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync<ActivityRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.Select(row => row.ToActivity()).ToArray();
    }

    public async Task<IReadOnlyList<ActivityAuditFact>> LoadAuditAsync(
        string campaignId,
        string activityId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT sequence_id AS "SequenceId", campaign_id AS "CampaignId", activity_id AS "ActivityId",
                   revision AS "Revision", disposition AS "Disposition", source AS "Source", event_id AS "EventId",
                   payload_hash AS "PayloadHash", occurred_at AS "OccurredAt"
            FROM bounded_campaign_activity_audit
            WHERE campaign_id = @campaignId AND activity_id = @activityId
            ORDER BY sequence_id;
            """;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync<ActivityAuditFact>(
            new CommandDefinition(sql, new { campaignId, activityId }, cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<PendingActivity?> TryAdvanceAsync(
        PendingActivity expected,
        long expectedRevision,
        ActivityLifecycle nextLifecycle,
        CancellationToken cancellationToken = default)
    {
        ValidateActivity(expected);
        if (nextLifecycle != ActivityLifecycle.Completed)
            throw new ArgumentOutOfRangeException(nameof(nextLifecycle), "Only pending-to-completed advancement is supported.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var current = await LoadForUpdateAsync(connection, transaction, expected.CampaignId, expected.ActivityId, cancellationToken);
        if (current is null || !SameSubject(current, expected) || current.Revision != expectedRevision ||
            current.Lifecycle != ActivityLifecycle.PendingExternalEvidence)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var advanced = current with { Revision = current.Revision + 1, Lifecycle = nextLifecycle };
        await UpdateActivityAsync(connection, transaction, current, advanced, expectedRevision, cancellationToken);
        await InsertAuditAsync(connection, transaction, advanced, "Advanced", null, null, null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return advanced;
    }

    public async Task<EvidenceReceiptResult> RecordReceiptAsync(
        ExternalEvidenceReceipt receipt,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        ValidateReceipt(receipt);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string insertReceipt = """
            INSERT INTO external_evidence_receipts
                (source, event_id, payload_hash, campaign_id, activity_id, flow_id, awaiting_step_id, disposition)
            VALUES
                (@Source, @EventId, @PayloadHash, @CampaignId, @ActivityId, @FlowId, @AwaitingStepId, 'Rejected')
            ON CONFLICT (source, event_id) DO NOTHING
            RETURNING source;
            """;
        var inserted = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
            insertReceipt, receipt, transaction, cancellationToken: cancellationToken));

        if (inserted is null)
        {
            const string existingSql = """
                SELECT source AS "Source", event_id AS "EventId", payload_hash AS "PayloadHash",
                       campaign_id AS "CampaignId", activity_id AS "ActivityId", flow_id AS "FlowId",
                       awaiting_step_id AS "AwaitingStepId", disposition AS "Disposition"
                FROM external_evidence_receipts
                WHERE source = @Source AND event_id = @EventId
                FOR UPDATE;
                """;
            var existing = await connection.QuerySingleAsync<ReceiptRow>(new CommandDefinition(
                existingSql, receipt, transaction, cancellationToken: cancellationToken));
            var sameDelivery = SameDelivery(existing, receipt);
            var current = await LoadForUpdateAsync(connection, transaction, receipt.CampaignId, receipt.ActivityId, cancellationToken);
            var disposition = sameDelivery ? Enum.Parse<EvidenceDisposition>(existing.Disposition) : EvidenceDisposition.Rejected;
            var targetActivity = current is not null && SameSubject(current, receipt) ? current : null;
            await InsertAuditAsync(connection, transaction, current ?? ToActivity(receipt),
                sameDelivery ? "Duplicate" : "Rejected", receipt.Source, receipt.EventId, receipt.PayloadHash, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new EvidenceReceiptResult(receipt with { Disposition = disposition }, targetActivity, IsReplay: sameDelivery);
        }

        var activity = await LoadForUpdateAsync(connection, transaction, receipt.CampaignId, receipt.ActivityId, cancellationToken);
        EvidenceDisposition resultDisposition;
        PendingActivity? resultActivity = activity;
        if (activity is null || !SameSubject(activity, receipt))
        {
            resultDisposition = EvidenceDisposition.Rejected;
        }
        else if (activity.Revision != expectedRevision)
        {
            resultDisposition = EvidenceDisposition.Stale;
        }
        else if (activity.Lifecycle != ActivityLifecycle.PendingExternalEvidence)
        {
            resultDisposition = EvidenceDisposition.Rejected;
        }
        else
        {
            resultDisposition = EvidenceDisposition.Accepted;
            resultActivity = activity with { Revision = activity.Revision + 1, Lifecycle = ActivityLifecycle.Completed };
            await UpdateActivityAsync(connection, transaction, activity, resultActivity, expectedRevision, cancellationToken);
        }

        const string setDisposition = """
            UPDATE external_evidence_receipts SET disposition = @disposition
            WHERE source = @source AND event_id = @eventId;
            """;
        await connection.ExecuteAsync(new CommandDefinition(setDisposition, new
        {
            disposition = resultDisposition.ToString(),
            source = receipt.Source,
            eventId = receipt.EventId
        }, transaction, cancellationToken: cancellationToken));

        var auditActivity = resultActivity ?? activity ?? ToActivity(receipt);
        await InsertAuditAsync(connection, transaction, auditActivity, resultDisposition.ToString(),
            receipt.Source, receipt.EventId, receipt.PayloadHash, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new EvidenceReceiptResult(receipt with { Disposition = resultDisposition }, resultActivity, IsReplay: false);
    }

    private static async Task<PendingActivity?> LoadForUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string campaignId,
        string activityId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT campaign_id AS "CampaignId", activity_id AS "ActivityId", flow_id AS "FlowId",
                   awaiting_step_id AS "AwaitingStepId", command_id AS "CommandId", revision AS "Revision",
                   lifecycle AS "Lifecycle", physical_outcome AS "PhysicalOutcome"
            FROM bounded_campaign_activities
            WHERE campaign_id = @campaignId AND activity_id = @activityId
            FOR UPDATE;
            """;
        var row = await connection.QuerySingleOrDefaultAsync<ActivityRow>(new CommandDefinition(
            sql, new { campaignId, activityId }, transaction, cancellationToken: cancellationToken));
        return row?.ToActivity();
    }

    private static async Task UpdateActivityAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        PendingActivity current,
        PendingActivity advanced,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE bounded_campaign_activities
            SET revision = @Revision, lifecycle = @Lifecycle, updated_at = clock_timestamp()
            WHERE campaign_id = @CampaignId AND activity_id = @ActivityId AND flow_id = @FlowId
              AND awaiting_step_id = @AwaitingStepId AND revision = @ExpectedRevision
              AND lifecycle = 'PendingExternalEvidence';
            """;
        var changed = await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            advanced.Revision,
            Lifecycle = advanced.Lifecycle.ToString(),
            advanced.CampaignId,
            advanced.ActivityId,
            advanced.FlowId,
            advanced.AwaitingStepId,
            ExpectedRevision = expectedRevision
        }, transaction, cancellationToken: cancellationToken));
        if (changed != 1 || !SameSubject(current, advanced))
            throw new InvalidOperationException("The pending activity changed while its row lock was held.");
    }

    private static async Task InsertAuditAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        PendingActivity activity,
        string disposition,
        string? source,
        string? eventId,
        string? payloadHash,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO bounded_campaign_activity_audit
                (campaign_id, activity_id, revision, disposition, source, event_id, payload_hash)
            VALUES
                (@CampaignId, @ActivityId, @Revision, @Disposition, @Source, @EventId, @PayloadHash);
            """;
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            activity.CampaignId,
            activity.ActivityId,
            activity.Revision,
            disposition,
            source,
            eventId,
            payloadHash
        }, transaction, cancellationToken: cancellationToken));
    }

    private static bool SameSubject(PendingActivity activity, PendingActivity other) =>
        activity.CampaignId == other.CampaignId && activity.ActivityId == other.ActivityId &&
        activity.FlowId == other.FlowId && activity.AwaitingStepId == other.AwaitingStepId;

    private static bool SameSubject(PendingActivity activity, ExternalEvidenceReceipt receipt) =>
        activity.CampaignId == receipt.CampaignId && activity.ActivityId == receipt.ActivityId &&
        activity.FlowId == receipt.FlowId && activity.AwaitingStepId == receipt.AwaitingStepId;

    private static bool SameDelivery(ReceiptRow stored, ExternalEvidenceReceipt incoming) =>
        stored.PayloadHash == incoming.PayloadHash && stored.CampaignId == incoming.CampaignId &&
        stored.ActivityId == incoming.ActivityId && stored.FlowId == incoming.FlowId &&
        stored.AwaitingStepId == incoming.AwaitingStepId;

    private static void ValidateActivity(PendingActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ValidateIdentifier(activity.CampaignId, nameof(activity.CampaignId));
        ValidateIdentifier(activity.ActivityId, nameof(activity.ActivityId));
        ValidateIdentifier(activity.FlowId, nameof(activity.FlowId));
        ValidateIdentifier(activity.AwaitingStepId, nameof(activity.AwaitingStepId));
        ValidateIdentifier(activity.CommandId, nameof(activity.CommandId));
        if (activity.Revision < 1)
            throw new ArgumentOutOfRangeException(nameof(activity), "Activity revision must be positive.");
        if (activity.PhysicalOutcome is not (PhysicalOutcome.unknown or PhysicalOutcome.uncertain))
            throw new ArgumentOutOfRangeException(nameof(activity), "Physical outcome must remain unknown or uncertain.");
    }

    private static void ValidateReceipt(ExternalEvidenceReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ValidateIdentifier(receipt.Source, nameof(receipt.Source));
        ValidateIdentifier(receipt.EventId, nameof(receipt.EventId));
        ValidateIdentifier(receipt.PayloadHash, nameof(receipt.PayloadHash));
        ValidateIdentifier(receipt.CampaignId, nameof(receipt.CampaignId));
        ValidateIdentifier(receipt.ActivityId, nameof(receipt.ActivityId));
        ValidateIdentifier(receipt.FlowId, nameof(receipt.FlowId));
        ValidateIdentifier(receipt.AwaitingStepId, nameof(receipt.AwaitingStepId));
    }

    private static void ValidateIdentifier(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Identifiers and hashes must not be empty.", name);
    }

    private sealed class ActivityRow
    {
        public string CampaignId { get; set; } = "";
        public string ActivityId { get; set; } = "";
        public string FlowId { get; set; } = "";
        public string AwaitingStepId { get; set; } = "";
        public string CommandId { get; set; } = "";
        public long Revision { get; set; }
        public string Lifecycle { get; set; } = "";
        public string PhysicalOutcome { get; set; } = "";

        public PendingActivity ToActivity() => new(CampaignId, ActivityId, FlowId, AwaitingStepId, CommandId,
            Revision, Enum.Parse<ActivityLifecycle>(Lifecycle), Enum.Parse<PhysicalOutcome>(PhysicalOutcome));
    }

    private sealed class ReceiptRow
    {
        public string Source { get; set; } = "";
        public string EventId { get; set; } = "";
        public string PayloadHash { get; set; } = "";
        public string CampaignId { get; set; } = "";
        public string ActivityId { get; set; } = "";
        public string FlowId { get; set; } = "";
        public string AwaitingStepId { get; set; } = "";
        public string Disposition { get; set; } = "";
    }

    private static PendingActivity ToActivity(ExternalEvidenceReceipt receipt) => new(
        receipt.CampaignId, receipt.ActivityId, receipt.FlowId, receipt.AwaitingStepId,
        "unknown-command", 1, ActivityLifecycle.PendingExternalEvidence, PhysicalOutcome.unknown);
}
