CREATE TABLE IF NOT EXISTS scientific_samples (
    sample_id uuid PRIMARY KEY,
    actor_sid text NOT NULL,
    origin text NOT NULL CHECK (origin IN ('collection','transformation')),
    parent_sample_ids uuid[] NOT NULL DEFAULT '{}',
    created_at timestamptz NOT NULL
);
CREATE TABLE IF NOT EXISTS scientific_sample_events (
    event_id uuid PRIMARY KEY,
    sample_id uuid NOT NULL REFERENCES scientific_samples(sample_id),
    event_kind text NOT NULL,
    label text NULL,
    custodian text NULL,
    location text NOT NULL,
    occurred_at timestamptz NOT NULL,
    recorded_at timestamptz NOT NULL,
    actor_sid text NOT NULL,
    server_sequence bigint NOT NULL,
    supersedes_event_id uuid NULL,
    correction_reason text NULL,
    UNIQUE(sample_id, server_sequence),
    CHECK ((event_kind <> 'Correction') OR (supersedes_event_id IS NOT NULL AND correction_reason IS NOT NULL))
);
CREATE TABLE IF NOT EXISTS scientific_sample_heads (
    sample_id uuid PRIMARY KEY REFERENCES scientific_samples(sample_id),
    server_sequence bigint NOT NULL,
    event_id uuid NOT NULL REFERENCES scientific_sample_events(event_id)
);
CREATE TABLE IF NOT EXISTS scientific_sample_operations (
    actor_sid text NOT NULL,
    operation_id uuid NOT NULL,
    operation_kind text NOT NULL,
    sample_id uuid NOT NULL,
    request_hash text NOT NULL,
    payload jsonb NOT NULL,
    recorded_at timestamptz NOT NULL,
    PRIMARY KEY(actor_sid, operation_id)
);
CREATE OR REPLACE FUNCTION scientific_sample_event_immutable() RETURNS trigger AS $$ BEGIN RAISE EXCEPTION 'Sample evidence is immutable'; END; $$ LANGUAGE plpgsql;
DROP TRIGGER IF EXISTS scientific_sample_events_immutable ON scientific_sample_events;
CREATE TRIGGER scientific_sample_events_immutable BEFORE UPDATE OR DELETE ON scientific_sample_events FOR EACH ROW EXECUTE FUNCTION scientific_sample_event_immutable();
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON scientific_samples, scientific_sample_events, scientific_sample_heads, scientific_sample_operations FROM PUBLIC;
DO $$
BEGIN
    -- The runtime role may append new immutable evidence and advance only the mutable stream head.
    -- It never receives UPDATE/DELETE on facts, events, or receipts.
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'helix_scientific_runtime') THEN
        GRANT SELECT, INSERT ON scientific_samples, scientific_sample_events, scientific_sample_operations TO helix_scientific_runtime;
        GRANT SELECT, INSERT, UPDATE ON scientific_sample_heads TO helix_scientific_runtime;
    END IF;
END $$;
