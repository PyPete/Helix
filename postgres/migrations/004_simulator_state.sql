-- Simulator effects are persisted separately from the Agent process so restart recovery can observe, never replay.
CREATE TABLE IF NOT EXISTS simulator_device_state (
    device_id TEXT NOT NULL,
    state_key TEXT NOT NULL,
    last_applied_command_id UUID NOT NULL,
    canonical_value TEXT NOT NULL,
    unit TEXT NOT NULL,
    generation BIGINT NOT NULL CHECK (generation > 0),
    observed_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (device_id, state_key)
);
