---
phase: 02-scientific-setup-foundation
plan: "25"
subsystem: scientific-postgresql-hardening
tags: [postgresql, testcontainers, migrations, least-privilege, immutable-evidence]
requires: [02-42]
provides: [runtime-immutable-role-proof, atomic-migration-replay-proof]
affects: [APP-01, APP-02, APP-03, APP-04, SAMP-01, SAMP-02]
tech-stack:
  added: []
  patterns: [owned-disposable-postgresql, explicit-runtime-role, atomic-sql-receipt]
key-files:
  created:
    - postgres/migrations/011_scientific_runtime_immutable_repair.sql
    - tests/Helix.Orchestration.Infrastructure.Tests/Persistence/ScientificPostgresFixture.cs
    - tests/Helix.Orchestration.Infrastructure.Tests/Persistence/ScientificSchemaTests.cs
    - tests/Helix.Orchestration.Infrastructure.Tests/Persistence/ScientificMigrationTests.cs
  modified:
    - scripts/verification/phase01-migrate.ps1
decisions:
  - Runtime role privileges are reset and then regranted only to named draft, stream-head, and append paths by additive migration 011.
  - Migration SQL and its checksum receipt are applied under one advisory-locked PostgreSQL transaction on an explicitly named target.
metrics:
  duration: 30min
  completed: 2026-11-01
  tasks: 2
status: complete
plan_head_before: d6b3d234ac004a2839fa033de4c3475491d431c3
plan_head_after: 84e9ddc389c92e25634145d095690467dfee8a9c
commits: 3
actuals:
  tokens: 109
  tasks: 2
  commits: 3
requirements-completed: [APP-01, APP-02, APP-03, APP-04, SAMP-01, SAMP-02]
---

# Phase 02 Plan 25: Runtime Immutability and Atomic Migration Summary

Real disposable PostgreSQL proves runtime scientists may update only drafts and sample heads while immutable scientific evidence remains protected through atomic migration crash and replay handling.

## Accomplishments

- Added additive migration 011; it revokes all runtime table/schema privileges and regrants only selected draft fields, sample-head updates, and append-only evidence paths.
- Added an owned Testcontainers fixture that never falls back to `PGHOST`, separates migration owner/runtime roles, and fails initialization explicitly.
- Proved actual runtime denial of delete, truncate, trigger disable, schema DDL, fact updates, and effective privilege propagation across all delivered immutable scientific tables.
- Reworked the PowerShell migration runner to require explicit identities and target selection, reject unowned disposable targets, serialize each apply/checksum receipt in one advisory-locked transaction, and check child `psql` exits.
- Added disposable runner coverage for crash-before-receipt rollback, fresh replay, repeat apply, checksum mutation rejection, and legacy-row preservation.

## Task Commits

1. **Task 1: Reject immutable fact mutation under every actual runtime grant** — `731bb2f`
2. **Task 2: Preserve scientific evidence through actual migration crash/replay** — `84e9ddc`

## Verification

- `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~ScientificSchemaTests|FullyQualifiedName~ScientificMigrationTests" --verbosity minimal` — passed (3 real disposable PostgreSQL tests; 0 skipped).
- Runtime repair explicitly revokes all table/schema privileges from `helix_scientific_runtime`, then grants only `UPDATE (name, draft_content, draft_token, latest_revision_id)` on drafts and `UPDATE` on `scientific_sample_heads`; immutable facts receive only `SELECT, INSERT` where runtime appends are authorized.
- The runner test injected failure after migration SQL and before the tracking insert; the receipt count remained zero, replay created exactly one receipt, a changed migration checksum failed, and the seeded `legacy_fixture` row remained `preserve`.

## Decisions Made

- Existing migrations 006–010 were not modified; least-privilege correction is a new unused additive migration.
- A PowerShell `GRANT` without grant options may exit successfully as a no-op, so the role test proves no effective privilege reaches an unrelated role rather than treating that no-op as a successful escalation.

## Deviations from Plan

### Auto-fixed Issues

1. **[Rule 3 - Blocking issue] Added the approved additive runtime-role repair migration**
   - **Found during:** Task 1
   - **Issue:** The runtime-role test exposed that the required permission boundary needed an explicit complete revoke/regrant repair path.
   - **Fix:** Added `011_scientific_runtime_immutable_repair.sql` without changing applied migrations.
   - **Files modified:** `postgres/migrations/011_scientific_runtime_immutable_repair.sql`, scientific fixture/tests.
   - **Commit:** `731bb2f`

2. **[Rule 1 - Bug] Corrected Testcontainers label inspection and PowerShell host invocation**
   - **Found during:** Task 2
   - **Issue:** Docker Go-template quoting and the unavailable `pwsh` executable prevented the isolated runner test from starting.
   - **Fix:** Used JSON label inspection and Windows PowerShell with repository-root discovery.
   - **Files modified:** `scripts/verification/phase01-migrate.ps1`, `ScientificMigrationTests.cs`
   - **Commit:** `84e9ddc`

## Known Stubs

None.

## Self-Check: PASSED

- Required additive migration, disposable fixture, schema proof, runner, and migration proof files exist.
- Task commits `731bb2f` and `84e9ddc` are present in HEAD ancestry.
