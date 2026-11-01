---
phase: 02-scientific-setup-foundation
plan: "38"
subsystem: scientific-sample-lineage
tags: [typescript, csharp, postgresql, tauri, samples, provenance]
requires: [02-37]
provides: [typed-transformation-client, accessible-bounded-lineage, durable-transformation-provenance]
affects: [SAMP-02]
tech-stack:
  added: []
  patterns: [original-operation-recovery, immutable-transformation-evidence, bounded-lineage-pages]
key-files:
  created:
    - helix-desktop/src/components/Samples/SampleLineage.tsx
    - helix-desktop/src/components/Samples/SampleLineage.test.tsx
  modified:
    - helix-desktop/src/lib/scientificClient.ts
    - helix-desktop/src/components/Samples/Samples.tsx
    - Helix.Orchestration.Infrastructure/Persistence/PostgresSampleRepository.cs
    - Helix.Orchestration.Agent/Api/SampleEndpoints.cs
decisions:
  - Transformation occurrence is supplied as evidence, while actor and recording time are Agent-derived and immutable.
  - Lineage entries expose activity provenance only from persisted transformation rows, never inferred UI state.
metrics:
  duration: 9min
  completed: 2026-11-01
  tasks: 2
status: complete
plan_head_before: f4e210a4943813c06879ae49b8192ef614b983c2
plan_head_after: 6d0613bbedef0593f985506f1d4055d21133e836
commits: 5
actuals:
  tokens: 11000
  tasks: 2
  commits: 5
requirements-completed: [SAMP-02]
---

# Phase 02 Plan 38: Truthful Transformation Evidence Summary

The desktop now records and traverses bounded transformation evidence using exact immutable IDs, persisted occurrence time, Agent-derived attribution, and server recording time.

## Accomplishments

- Added typed native client transformation and lineage calls with strict acknowledgement, identity, page-bound, and lineage-envelope validation.
- Added an accessible Record transformation dialog with parent/activity/child confirmation, optional quantity provenance, original-key uncertainty recovery, and child transformation origin display.
- Added an accessible lineage table with exact sample navigation, direction selection, continuation, truncation, quantity/not-supplied state, and immutable activity provenance.
- Extended persisted lineage reconstruction to return the transformation operation, authenticated actor SID, supplied occurrence time, server recording time, and quantity evidence.

## Task Commits

1. **Task 1: Consume complete transformation and bounded lineage receipts** — `f757cc1`, `a2a3234`
2. **Task 2: Record and navigate complete accessible transformation evidence** — `6bb2000`, `6dd3187`, `6d0613b`

## Verification

- `npm --prefix helix-desktop test -- --run src/lib/scientificClient.test.ts src/components/Samples/Samples.test.tsx src/components/Samples/SampleLineage.test.tsx` — passed (10 tests).
- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~SamplesTests --verbosity minimal` — passed (4 tests).
- `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` — passed (12 tests; one intentional ignored live fixture).

## Decisions Made

- The Agent, not the renderer, supplies the transformation actor identity and server recording time.
- Existing additive `009_scientific_transformations.sql` columns are now used to reconstruct durable provenance; no destructive schema change was required.

## Deviations from Plan

### Auto-fixed Issues

1. **[Rule 2 - Critical evidence completeness] Extended the transformation evidence path after user authorization**
- **Found during:** Task 2
- **Issue:** the schema already stored occurrence/actor/recorded time, but the domain, route, lineage DTO, client, and UI dropped those immutable fields.
- **Fix:** propagated the fields from Agent-backed persistence through the bounded lineage response and accessible UI.
- **Files modified:** Core sample facts, Agent endpoint, PostgreSQL repository, client, Samples UI, lineage UI, and integration tests.
- **Commit:** `6d0613b`

## Known Stubs

None.

## Self-Check: PASSED

- Required client, Samples, lineage, Agent persistence, and integration-test artifacts exist.
- Task commits `f757cc1`, `a2a3234`, `6bb2000`, `6dd3187`, and `6d0613b` are ancestors of the recorded plan head.
