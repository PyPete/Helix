CREATE TABLE IF NOT EXISTS bounded_campaign_activities (
    campaign_id       TEXT        NOT NULL,
    activity_id       TEXT        NOT NULL,
    flow_id            TEXT        NOT NULL,
    awaiting_step_id   TEXT        NOT NULL,
    command_id         TEXT        NOT NULL,
    revision           BIGINT      NOT NULL CHECK (revision > 0),
    lifecycle          TEXT        NOT NULL CHECK (lifecycle IN ('PendingExternalEvidence', 'Completed')),
    physical_outcome   TEXT        NOT NULL CHECK (physical_outcome IN ('unknown', 'uncertain')),
    created_at         TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT pk_bounded_campaign_activities PRIMARY KEY (campaign_id, activity_id),
    CONSTRAINT uq_bounded_campaign_activities_campaign UNIQUE (campaign_id),
    CONSTRAINT uq_bounded_campaign_activities_subject UNIQUE (campaign_id, activity_id, flow_id, awaiting_step_id),
    CONSTRAINT uq_bounded_campaign_activities_command UNIQUE (command_id)
);

CREATE TABLE IF NOT EXISTS external_evidence_receipts (
    source             TEXT        NOT NULL,
    event_id           TEXT        NOT NULL,
    payload_hash       TEXT        NOT NULL,
    campaign_id        TEXT        NOT NULL,
    activity_id        TEXT        NOT NULL,
    flow_id            TEXT        NOT NULL,
    awaiting_step_id   TEXT        NOT NULL,
    disposition        TEXT        NOT NULL CHECK (disposition IN ('Accepted', 'Duplicate', 'Rejected', 'Stale')),
    received_at        TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT pk_external_evidence_receipts PRIMARY KEY (source, event_id)
);

CREATE INDEX IF NOT EXISTS ix_external_evidence_receipts_subject
    ON external_evidence_receipts (campaign_id, activity_id, flow_id, awaiting_step_id);

CREATE TABLE IF NOT EXISTS bounded_campaign_activity_audit (
    sequence_id        BIGSERIAL   PRIMARY KEY,
    campaign_id        TEXT        NOT NULL,
    activity_id        TEXT        NOT NULL,
    revision           BIGINT      NOT NULL,
    disposition        TEXT        NOT NULL,
    source             TEXT        NULL,
    event_id           TEXT        NULL,
    payload_hash       TEXT        NULL,
    occurred_at        TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
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
