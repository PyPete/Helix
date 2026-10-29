-- Additive safeguards for the command ledger.  Migration tracking, not this
-- file, owns idempotence; all statements are nevertheless safe to replay.

CREATE OR REPLACE FUNCTION prevent_command_ledger_fact_mutation()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'command ledger facts are append-only';
END;
$$;

DROP TRIGGER IF EXISTS command_intents_append_only ON command_intents;
CREATE TRIGGER command_intents_append_only
    BEFORE UPDATE OR DELETE ON command_intents
    FOR EACH ROW EXECUTE FUNCTION prevent_command_ledger_fact_mutation();

DROP TRIGGER IF EXISTS command_evidence_append_only ON command_evidence;
CREATE TRIGGER command_evidence_append_only
    BEFORE UPDATE OR DELETE ON command_evidence
    FOR EACH ROW EXECUTE FUNCTION prevent_command_ledger_fact_mutation();

-- A stream sequence is a per-execution canonical order, not a global ID.
ALTER TABLE command_evidence
    ALTER COLUMN sequence_num SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uq_command_evidence_execution_sequence'
          AND conrelid = 'command_evidence'::regclass)
    THEN
        ALTER TABLE command_evidence
            ADD CONSTRAINT uq_command_evidence_execution_sequence
            UNIQUE (execution_id, sequence_num);
    END IF;
END;
$$;

REVOKE UPDATE, DELETE, TRUNCATE ON command_intents, command_evidence FROM PUBLIC;
