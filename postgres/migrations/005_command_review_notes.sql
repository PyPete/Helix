-- Append-only, idempotent scientist context for immutable command evidence.
CREATE TABLE IF NOT EXISTS command_review_notes (
    note_id UUID PRIMARY KEY,
    operation_id UUID NOT NULL UNIQUE,
    command_id UUID NOT NULL REFERENCES command_intents(command_id),
    verified_actor_sid TEXT NOT NULL,
    verified_actor_display_name TEXT NOT NULL,
    note_text TEXT NOT NULL CHECK (char_length(note_text) BETWEEN 1 AND 2000),
    occurred_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_command_review_notes_command_id
    ON command_review_notes (command_id, occurred_at, note_id);

DROP TRIGGER IF EXISTS command_review_notes_append_only ON command_review_notes;
CREATE TRIGGER command_review_notes_append_only
    BEFORE UPDATE OR DELETE ON command_review_notes
    FOR EACH ROW EXECUTE FUNCTION prevent_command_ledger_fact_mutation();

REVOKE UPDATE, DELETE, TRUNCATE ON command_review_notes FROM PUBLIC;
