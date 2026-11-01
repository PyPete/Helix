---
phase: 02-scientific-setup-foundation
plan: "37"
subsystem: scientific-sample-lineage
tags: [csharp, rust, postgresql, samples, lineage, tauri]
requires: [02-36]
provides: [scoped-transformation-api, bounded-lineage-api, guarded-native-lineage-commands]
affects: [SAMP-02]
tech-stack:
  added: []
  patterns: [transaction-scoped-advisory-locks, actor-scoped-receipts, fixed-native-routes]
key-files:
  created: []
  modified:
    - Helix.Orchestration.Agent/Samples/SampleService.cs
    - Helix.Orchestration.Agent/Api/SampleEndpoints.cs
    - Helix.Orchestration.Infrastructure/Persistence/PostgresSampleRepository.cs
    - tests/Helix.Orchestration.Agent.Tests/Integration/SamplesTests.cs
    - helix-desktop/src-tauri/src/commands/agent.rs
    - helix-desktop/src-tauri/src/lib.rs
    - helix-desktop/src-tauri/permissions/agent.toml
decisions:
  - Transformation persistence serializes original operation IDs and parent identities with domain-separated transaction advisory locks, retaining restricted runtime privileges.
  - Native transformation and lineage calls use fixed routes, validated UUIDs/direction/page bounds, and main-window session authority.
metrics:
  duration: 33min
  completed: 2026-11-01
  tasks: 2
status: complete
plan_head_before: 4dd57865a5ae36afe569baf451cfb37c307abe76
plan_head_after: 02bde57
commits: 3
actuals:
  tokens: 8600
  tasks: 2
  commits: 3
requirements-completed: [SAMP-02]
---

# Phase 02 Plan 37: Guarded Sample Transformation API Summary

Authenticated Agent and native clients can now submit attributable transformations and inspect bounded lineage without broadening runtime database or device authority.

## Accomplishments

- Added authenticated `POST /api/samples/transformations` and `GET /api/samples/{id}/lineage` routes with bounded request/page validation and actor-scoped operation lookup.
- Replaced restricted-role row locks with deterministic, domain-separated `pg_advisory_xact_lock` transaction locks for operation replay and ordered parent validation.
- Added fixed Tauri `sample_transform` and `sample_lineage` commands, registered only for the main-window Agent broker permission.
- Added isolated Agent integration coverage for transformation creation, descendant lineage, and zero hardware/capability dispatch.

## Task Commits

1. **Task 1: Admit scoped transformations and exact bounded lineage** — `83243d7`, `d3d7685`
2. **Task 2: Register guarded transformation and lineage commands** — `02bde57`

## Verification

- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~SamplesTests --verbosity minimal` — passed (4 tests).
- `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` — passed (12 tests; one intentional ignored live fixture).

## Decisions Made

- Advisory locks are derived from SHA-256 domain-separated actor/operation or actor/parent values and end with the transaction, avoiding session locks and runtime UPDATE grants.
- The native broker exposes no renderer-controlled route or HTTP verb for sample transformation or lineage reads.

## Deviations from Plan

### Auto-fixed Issues

1. **[Rule 1 - Bug] Repaired restricted-runtime transformation persistence**
- **Found during:** Task 1
- **Issue:** row locking caused the constrained runtime role to receive HTTP 500 permission failures.
- **Fix:** replaced row locks with transaction-scoped advisory locks and deterministic parent ordering; isolated Agent clients now use distinct actors to prevent test cross-contamination.
- **Files modified:** `PostgresSampleRepository.cs`, `ScientificPostgresFixture.cs`
- **Commit:** `d3d7685`

## Known Stubs

None.

## Self-Check: PASSED

- Required Agent, repository, native command, permission, and test artifacts exist.
- Task commits `83243d7`, `d3d7685`, and `02bde57` are ancestors of the recorded plan head.
