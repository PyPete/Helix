-- Immutable, per-execution command-admission evidence.
CREATE TABLE IF NOT EXISTS command_streams (
    execution_id UUID PRIMARY KEY,
    next_sequence BIGINT NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS command_intents (
    command_id UUID PRIMARY KEY,
    execution_id UUID NOT NULL REFERENCES command_streams(execution_id),
    correlation_id UUID NOT NULL,
    device_id TEXT NOT NULL,
    capability TEXT NOT NULL,
    canonical_parameters JSONB NOT NULL,
    verified_actor_sid TEXT NOT NULL,
    verified_actor_display_name TEXT NOT NULL,
    session_reference TEXT NOT NULL,
    policy_revision TEXT NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE IF NOT EXISTS command_evidence (
    evidence_id UUID PRIMARY KEY,
    command_id UUID NOT NULL REFERENCES command_intents(command_id),
    execution_id UUID NOT NULL REFERENCES command_streams(execution_id),
    sequence_num BIGINT NOT NULL,
    admission_state TEXT NOT NULL CHECK (admission_state IN ('Allowed', 'Rejected', 'Held')),
    checks_json JSONB NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    UNIQUE (execution_id, sequence_num),
    UNIQUE (command_id)
);

CREATE INDEX IF NOT EXISTS ix_command_evidence_execution_sequence
    ON command_evidence (execution_id, sequence_num);
