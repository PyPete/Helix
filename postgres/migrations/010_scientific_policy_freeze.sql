CREATE TABLE IF NOT EXISTS scientific_policy_revisions (
    revision_id uuid PRIMARY KEY,
    policy_id uuid NOT NULL UNIQUE,
    schema text NOT NULL,
    canonical_content bytea NOT NULL,
    content_hash text NOT NULL,
    created_by_sid text NOT NULL,
    source text NOT NULL,
    recorded_at timestamptz NOT NULL,
    UNIQUE (policy_id, content_hash)
);

CREATE TABLE IF NOT EXISTS scientific_frozen_setup_manifests (
    manifest_id uuid PRIMARY KEY,
    apparatus_revision_id uuid NOT NULL REFERENCES scientific_apparatus_revisions(revision_id),
    procedure_revision_id uuid NOT NULL REFERENCES scientific_procedure_revisions(revision_id),
    plan_revision_id uuid NOT NULL REFERENCES scientific_experiment_plan_revisions(revision_id),
    policy_revision_id uuid NOT NULL REFERENCES scientific_policy_revisions(revision_id),
    actor_sid text NOT NULL,
    manifest jsonb NOT NULL,
    frozen_at timestamptz NOT NULL
);

CREATE OR REPLACE FUNCTION scientific_policy_freeze_immutable() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'Frozen scientific policy evidence is immutable';
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS scientific_policy_revisions_immutable ON scientific_policy_revisions;
CREATE TRIGGER scientific_policy_revisions_immutable BEFORE UPDATE OR DELETE ON scientific_policy_revisions FOR EACH ROW EXECUTE FUNCTION scientific_policy_freeze_immutable();
DROP TRIGGER IF EXISTS scientific_frozen_setup_manifests_immutable ON scientific_frozen_setup_manifests;
CREATE TRIGGER scientific_frozen_setup_manifests_immutable BEFORE UPDATE OR DELETE ON scientific_frozen_setup_manifests FOR EACH ROW EXECUTE FUNCTION scientific_policy_freeze_immutable();

REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON scientific_policy_revisions, scientific_frozen_setup_manifests FROM PUBLIC;
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'helix_scientific_runtime') THEN
        GRANT SELECT, INSERT ON scientific_policy_revisions, scientific_frozen_setup_manifests TO helix_scientific_runtime;
    END IF;
END $$;
