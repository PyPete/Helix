CREATE TABLE IF NOT EXISTS scientific_sample_transformations (
    transformation_id uuid PRIMARY KEY,
    actor_sid text NOT NULL,
    transformation_kind text NOT NULL CHECK (transformation_kind IN ('Split', 'Pool', 'Processing')),
    operation_description text NOT NULL CHECK (length(btrim(operation_description)) > 0),
    quantity_value numeric NULL,
    quantity_unit_code text NULL,
    quantity_dimension text NULL,
    quantity_supplied boolean NOT NULL DEFAULT false,
    occurred_at timestamptz NOT NULL,
    recorded_at timestamptz NOT NULL,
    CHECK ((quantity_supplied = false AND quantity_value IS NULL AND quantity_unit_code IS NULL AND quantity_dimension IS NULL)
        OR (quantity_supplied = true AND quantity_value > 0 AND quantity_unit_code IS NOT NULL AND quantity_dimension IS NOT NULL))
);

CREATE TABLE IF NOT EXISTS scientific_sample_lineage_edges (
    transformation_id uuid NOT NULL REFERENCES scientific_sample_transformations(transformation_id),
    parent_sample_id uuid NOT NULL REFERENCES scientific_samples(sample_id),
    child_sample_id uuid NOT NULL REFERENCES scientific_samples(sample_id),
    PRIMARY KEY (transformation_id, parent_sample_id, child_sample_id),
    CHECK (parent_sample_id <> child_sample_id)
);

CREATE INDEX IF NOT EXISTS ix_scientific_sample_lineage_edges_parent ON scientific_sample_lineage_edges(parent_sample_id, transformation_id, child_sample_id);
CREATE INDEX IF NOT EXISTS ix_scientific_sample_lineage_edges_child ON scientific_sample_lineage_edges(child_sample_id, transformation_id, parent_sample_id);

CREATE OR REPLACE FUNCTION scientific_sample_transformation_immutable() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'Sample transformation evidence is immutable';
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS scientific_sample_transformations_immutable ON scientific_sample_transformations;
CREATE TRIGGER scientific_sample_transformations_immutable BEFORE UPDATE OR DELETE ON scientific_sample_transformations
    FOR EACH ROW EXECUTE FUNCTION scientific_sample_transformation_immutable();
DROP TRIGGER IF EXISTS scientific_sample_lineage_edges_immutable ON scientific_sample_lineage_edges;
CREATE TRIGGER scientific_sample_lineage_edges_immutable BEFORE UPDATE OR DELETE ON scientific_sample_lineage_edges
    FOR EACH ROW EXECUTE FUNCTION scientific_sample_transformation_immutable();

REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON scientific_sample_transformations, scientific_sample_lineage_edges FROM PUBLIC;
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'helix_scientific_runtime') THEN
        GRANT SELECT, INSERT ON scientific_sample_transformations, scientific_sample_lineage_edges TO helix_scientific_runtime;
    END IF;
END $$;
