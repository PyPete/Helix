---
phase: 02-scientific-setup-foundation
plan: "42"
subsystem: scientific-setup
tags: [desktop, frozen-references, postgresql, authority, reconstruction]
requires: [02-41]
provides: [exact-assembly-ui, shell-assembly-entry, sql-freeze-reconstruction-evidence]
affects: [APP-04]
tech-stack:
  added: []
  patterns: [immutable-manifest-readback, trusted-test-authority, isolated-postgresql-fixture]
key-files:
  created:
    - helix-desktop/src/components/ScientificSetup/FrozenAssembly.tsx
    - helix-desktop/src/components/ScientificSetup/FrozenAssembly.test.tsx
    - tests/Helix.Orchestration.Infrastructure.Tests/Persistence/FrozenReferenceTests.cs
  modified:
    - helix-desktop/src/shell/WorkspaceLayout.tsx
    - helix-desktop/src/shell/HelixShell.tsx
    - helix-desktop/src/shell/ActivityBar.tsx
    - tests/Helix.Orchestration.Agent.Tests/Integration/FrozenSetupTests.cs
decisions:
  - Assembly remains read-only and only exposes a confirmation path for a valid authoritative policy; it has no campaign or device-command control.
  - Positive authority is confined to test-only fixtures; production's unavailable authority remains fail-closed.
metrics:
  duration: 8min
  completed: 2026-11-01
  tasks: 3
status: complete
plan_head_before: 614afb53940806d301c6895c99cd17738e8b2868
plan_head_after: 32976ea
commits: 4
actuals:
  tokens: 26100
  tasks: 3
  commits: 4
requirements-completed: [APP-04]
---

# Phase 02 Plan 42: Frozen Assembly and SQL Reconstruction Summary

Exact scientific assembly evidence is reachable through the desktop shell, while a real isolated PostgreSQL fixture proves immutable frozen references and compiled bytes survive later publication and repository reconstruction.

## Accomplishments

- Added a read-only assembly surface that displays all exact references, policy provenance, explicit no-campaign/no-command copy, a confirmation dialog, and fail-closed unavailable-authority state.
- Added an Assembly tab and Activity Bar entry so the shell can restore and reopen the scientific assembly surface.
- Added isolated PostgreSQL coverage that persists a trusted positive policy/manifest, publishes a later policy, reconstructs the repository, and verifies original references, provenance, compiled bytes, hash, and timestamp.
- Kept production authority fail-closed while testing only bound, unexpired trusted provenance in the Agent test host.

## Task Commits

1. **Task 1: Confirm exact scientific references with truthful authority states** — `4d4eca4`
2. **Task 2: Reach and restore Assembly as the fifth scientific entry** — `58de3f1`
3. **Task 3: Prove positive immutable freeze after publication and reconstruction** — `9df3d64`, `32976ea`

## Verification

- `npm --prefix helix-desktop test -- --run src/components/ScientificSetup/FrozenAssembly.test.tsx` — passed (2 tests).
- `npm --prefix helix-desktop test -- --run src/shell/WorkspaceLayout.test.tsx src/shell/HelixShell.test.tsx` — passed (8 tests).
- `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~FrozenReferenceTests --verbosity minimal` — passed (2 real PostgreSQL tests).
- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~FrozenSetupTests --verbosity minimal` — passed (2 tests).

## Decisions Made

- A blocked production policy remains a visible non-commit outcome, not a successful freeze proof.
- The positive authority seam is only instantiated inside tests and checks bound policy identity, hash, schema, scope, and expiry.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking issue] Seeded actual published reference rows for the SQL manifest fixture**
- **Found during:** Task 3
- **Issue:** The frozen-manifest foreign keys correctly rejected synthetic, unpublished apparatus/procedure/plan references.
- **Fix:** The isolated fixture now inserts actual published reference rows before freezing, preserving the real SQL proof boundary.
- **Files modified:** `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/FrozenReferenceTests.cs`
- **Commit:** `9df3d64`

## Known Stubs

None.

## Self-Check: PASSED

- Frozen assembly component and SQL reconstruction test files exist.
- Task commits `4d4eca4`, `58de3f1`, `9df3d64`, and `32976ea` are present in HEAD ancestry.
