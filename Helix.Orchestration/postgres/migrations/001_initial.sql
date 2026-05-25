CREATE TABLE IF NOT EXISTS campaigns (
	campaign_id TEXT PRIMARY KEY,
	name TEXT NOT NULL,
	description TEXT NULL,
	status TEXT NOT NULL,
	created_at TIMESTAMPTZ NOT NULL,
	started_at TIMESTAMPTZ NULL,
	ended_at TIMESTAMPTZ NULL
);

CREATE TABLE IF NOT EXISTS campaign_iterations (
	campaign_id TEXT NOT NULL,
	iteration_index INTEGER NOT NULL,
	parameters_json JSONB NOT NULL,
	runspace_id TEXT NULL,
	status TEXT NOT NULL,
	message TEXT NULL,
	created_at TIMESTAMPTZ NOT NULL,
	started_at TIMESTAMPTZ NULL,
	ended_at TIMESTAMPTZ NULL,
	CONSTRAINT pk_campaign_iterations PRIMARY KEY (campaign_id, iteration_index),
	CONSTRAINT fk_campaign_iterations_campaign
		FOREIGN KEY (campaign_id)
		REFERENCES campaigns(campaign_id)
		ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS experiments (
	runspace_id TEXT PRIMARY KEY,
	campaign_id TEXT NULL,
	definition_id TEXT NOT NULL,
	name TEXT NOT NULL,
	status TEXT NOT NULL,
	created_at TIMESTAMPTZ NOT NULL,
	started_at TIMESTAMPTZ NULL,
	ended_at TIMESTAMPTZ NULL,
	fault_message TEXT NULL,
	definition_json JSONB NOT NULL,
	CONSTRAINT fk_experiments_campaign
		FOREIGN KEY (campaign_id)
		REFERENCES campaigns(campaign_id)
		ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS ix_experiments_campaign_id ON experiments(campaign_id);
CREATE INDEX IF NOT EXISTS ix_campaign_iterations_runspace_id ON campaign_iterations(runspace_id);
