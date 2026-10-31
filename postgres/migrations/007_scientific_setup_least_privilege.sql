-- Scientific setup persistence has an owner role for migrations and a narrow
-- runtime role for Agent reads and the append/update operations it performs.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'helix_scientific_owner') THEN
        CREATE ROLE helix_scientific_owner NOLOGIN NOINHERIT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'helix_scientific_runtime') THEN
        CREATE ROLE helix_scientific_runtime NOLOGIN NOINHERIT;
    END IF;
END $$;

ALTER TABLE scientific_apparatus OWNER TO helix_scientific_owner;
ALTER TABLE scientific_apparatus_revisions OWNER TO helix_scientific_owner;
ALTER TABLE scientific_operations OWNER TO helix_scientific_owner;

ALTER TABLE scientific_operations
    ADD COLUMN IF NOT EXISTS request_hash text NOT NULL DEFAULT '';

REVOKE ALL ON TABLE scientific_apparatus, scientific_apparatus_revisions, scientific_operations FROM PUBLIC;
REVOKE ALL ON TABLE scientific_apparatus, scientific_apparatus_revisions, scientific_operations FROM helix_scientific_runtime;

GRANT USAGE ON SCHEMA public TO helix_scientific_runtime;
GRANT SELECT, INSERT ON scientific_apparatus TO helix_scientific_runtime;
GRANT UPDATE (latest_revision_id, draft_token) ON scientific_apparatus TO helix_scientific_runtime;
GRANT SELECT, INSERT ON scientific_apparatus_revisions TO helix_scientific_runtime;
GRANT SELECT, INSERT ON scientific_operations TO helix_scientific_runtime;

-- Permit the migration principal to exercise the runtime role locally; the
-- role itself remains NOLOGIN and has no ownership or DDL privileges.
GRANT helix_scientific_runtime TO CURRENT_USER;
