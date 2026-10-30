-- Add experiment_id column to experiments (idempotent)
DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'experiments' AND column_name = 'experiment_id'
    ) THEN
        ALTER TABLE experiments ADD COLUMN experiment_id TEXT NOT NULL DEFAULT '';
    END IF;
END $$;

-- Backfill: use runspace_id as experiment_id where not yet set
UPDATE experiments SET experiment_id = runspace_id WHERE experiment_id = '';

-- Unique index on experiment_id
CREATE UNIQUE INDEX IF NOT EXISTS ix_experiments_experiment_id ON experiments(experiment_id);

-- Experiment events log
CREATE TABLE IF NOT EXISTS experiment_events (
    sequence_num    BIGSERIAL       PRIMARY KEY,
    experiment_id   TEXT            NOT NULL,
    phase           TEXT            NOT NULL DEFAULT 'none',
    event_type      TEXT            NOT NULL,
    runspace_id     TEXT            NOT NULL DEFAULT '',
    step_id         TEXT            NULL,
    step_label      TEXT            NULL,
    is_manual       BOOLEAN         NULL,
    device_id       TEXT            NULL,
    capability      TEXT            NULL,
    parameters_json JSONB           NULL,
    result_json     JSONB           NULL,
    occurred_at     TIMESTAMPTZ     NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_experiment_events_experiment_id
    ON experiment_events(experiment_id, sequence_num);
