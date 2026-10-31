CREATE TABLE IF NOT EXISTS scientific_procedures (
    procedure_id uuid PRIMARY KEY,
    actor_sid text NOT NULL,
    name text NOT NULL,
    draft_content jsonb NOT NULL,
    draft_token bigint NOT NULL DEFAULT 1,
    latest_revision_id uuid,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS scientific_procedure_revisions (
    revision_id uuid PRIMARY KEY,
    procedure_id uuid NOT NULL REFERENCES scientific_procedures(procedure_id),
    revision_name text NOT NULL,
    content_hash text NOT NULL,
    canonical_content jsonb NOT NULL,
    actor_sid text NOT NULL,
    recorded_at timestamptz NOT NULL,
    UNIQUE(procedure_id, revision_name)
);
ALTER TABLE scientific_procedures
    ADD CONSTRAINT scientific_procedures_latest_revision_fk
    FOREIGN KEY (latest_revision_id) REFERENCES scientific_procedure_revisions(revision_id) DEFERRABLE INITIALLY DEFERRED;

CREATE TABLE IF NOT EXISTS scientific_experiment_plans (
    plan_id uuid PRIMARY KEY,
    actor_sid text NOT NULL,
    name text NOT NULL,
    draft_content jsonb NOT NULL,
    draft_token bigint NOT NULL DEFAULT 1,
    latest_revision_id uuid,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS scientific_experiment_plan_revisions (
    revision_id uuid PRIMARY KEY,
    plan_id uuid NOT NULL REFERENCES scientific_experiment_plans(plan_id),
    procedure_revision_id uuid NOT NULL REFERENCES scientific_procedure_revisions(revision_id),
    revision_name text NOT NULL,
    content_hash text NOT NULL,
    canonical_content jsonb NOT NULL,
    actor_sid text NOT NULL,
    recorded_at timestamptz NOT NULL,
    UNIQUE(plan_id, revision_name)
);
ALTER TABLE scientific_experiment_plans
    ADD CONSTRAINT scientific_experiment_plans_latest_revision_fk
    FOREIGN KEY (latest_revision_id) REFERENCES scientific_experiment_plan_revisions(revision_id) DEFERRABLE INITIALLY DEFERRED;

CREATE OR REPLACE FUNCTION scientific_setup_immutable_publication() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'Published scientific content is immutable';
END;
$$ LANGUAGE plpgsql;
DROP TRIGGER IF EXISTS scientific_procedure_revisions_immutable ON scientific_procedure_revisions;
CREATE TRIGGER scientific_procedure_revisions_immutable BEFORE UPDATE OR DELETE ON scientific_procedure_revisions FOR EACH ROW EXECUTE FUNCTION scientific_setup_immutable_publication();
DROP TRIGGER IF EXISTS scientific_plan_revisions_immutable ON scientific_experiment_plan_revisions;
CREATE TRIGGER scientific_plan_revisions_immutable BEFORE UPDATE OR DELETE ON scientific_experiment_plan_revisions FOR EACH ROW EXECUTE FUNCTION scientific_setup_immutable_publication();
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON scientific_procedures, scientific_procedure_revisions, scientific_experiment_plans, scientific_experiment_plan_revisions FROM PUBLIC;
DO $$
BEGIN
    -- Development fixtures create the constrained role after the additive tables;
    -- production role provisioning may run before migrations. Support both orders.
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'helix_scientific_runtime') THEN
        GRANT SELECT, INSERT, UPDATE ON scientific_procedures, scientific_experiment_plans TO helix_scientific_runtime;
        GRANT SELECT, INSERT ON scientific_procedure_revisions, scientific_experiment_plan_revisions TO helix_scientific_runtime;
    END IF;
END $$;
