-- Repair least privilege for scientific evidence without rewriting applied migrations.
-- Reset the runtime role to its explicit write surface so no historical or ambient
-- grant can turn immutable evidence into mutable runtime state.
REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM helix_scientific_runtime;
REVOKE ALL PRIVILEGES ON SCHEMA public FROM helix_scientific_runtime;
REVOKE CREATE ON SCHEMA public FROM helix_scientific_runtime;

GRANT USAGE ON SCHEMA public TO helix_scientific_runtime;
GRANT SELECT, INSERT ON scientific_apparatus TO helix_scientific_runtime;
GRANT UPDATE (name, draft_content, draft_token, latest_revision_id) ON scientific_apparatus TO helix_scientific_runtime;
GRANT SELECT, INSERT ON scientific_apparatus_revisions, scientific_operations TO helix_scientific_runtime;
GRANT SELECT, INSERT ON scientific_procedures, scientific_experiment_plans TO helix_scientific_runtime;
GRANT UPDATE (name, draft_content, draft_token, latest_revision_id) ON scientific_procedures, scientific_experiment_plans TO helix_scientific_runtime;
GRANT SELECT, INSERT ON scientific_procedure_revisions, scientific_experiment_plan_revisions TO helix_scientific_runtime;
GRANT SELECT, INSERT ON scientific_samples, scientific_sample_events, scientific_sample_operations TO helix_scientific_runtime;
GRANT SELECT, INSERT, UPDATE ON scientific_sample_heads TO helix_scientific_runtime;
GRANT SELECT, INSERT ON scientific_sample_transformations, scientific_sample_lineage_edges TO helix_scientific_runtime;
GRANT SELECT, INSERT ON scientific_policy_revisions, scientific_frozen_setup_manifests TO helix_scientific_runtime;
