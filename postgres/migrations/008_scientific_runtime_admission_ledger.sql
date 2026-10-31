-- The constrained scientific runtime records immutable admission facts during
-- desktop-session bootstrap. It must not gain dispatch or scientific-revision
-- mutation privileges to do so.
GRANT SELECT, INSERT, UPDATE (next_sequence) ON TABLE command_streams TO helix_scientific_runtime;
GRANT SELECT, INSERT ON TABLE command_intents, command_evidence TO helix_scientific_runtime;
GRANT SELECT ON TABLE command_dispatches TO helix_scientific_runtime;
