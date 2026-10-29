-- A dispatch is irrevocably uncertain after its durable start record until an
-- observed outcome is written. Recovery therefore holds; it never retries.
CREATE TABLE IF NOT EXISTS execution_controls (
    execution_id UUID PRIMARY KEY,
    control_state TEXT NOT NULL CHECK (control_state IN ('Running', 'Paused', 'Held', 'Aborted')),
    revision BIGINT NOT NULL CHECK (revision >= 0),
    hold_reasons JSONB NOT NULL DEFAULT '[]'::jsonb,
    actor TEXT NULL,
    evidence TEXT NULL,
    changed_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE IF NOT EXISTS execution_control_events (
    event_id UUID PRIMARY KEY,
    execution_id UUID NOT NULL REFERENCES execution_controls(execution_id),
    revision BIGINT NOT NULL,
    control_state TEXT NOT NULL,
    hold_reasons JSONB NOT NULL,
    actor TEXT NOT NULL,
    evidence TEXT NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    UNIQUE (execution_id, revision)
);

CREATE TABLE IF NOT EXISTS command_dispatches (
    command_id UUID PRIMARY KEY REFERENCES command_intents(command_id),
    execution_id UUID NOT NULL REFERENCES command_streams(execution_id),
    dispatch_state TEXT NOT NULL CHECK (dispatch_state IN ('InFlight', 'Confirmed', 'HeldForReconciliation')),
    state_key TEXT NOT NULL,
    canonical_value TEXT NOT NULL,
    unit TEXT NOT NULL,
    observed_generation BIGINT NULL,
    started_at TIMESTAMPTZ NOT NULL,
    resolved_at TIMESTAMPTZ NULL,
    resolution TEXT NULL
);

CREATE INDEX IF NOT EXISTS ix_command_dispatches_in_flight
    ON command_dispatches (dispatch_state, started_at)
    WHERE dispatch_state = 'InFlight';
