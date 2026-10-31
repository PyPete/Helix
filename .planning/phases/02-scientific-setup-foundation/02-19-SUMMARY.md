---
phase: 02-scientific-setup-foundation
plan: "19"
subsystem: scientific-setup
tags: [apparatus, postgresql, tauri, react, concurrency]
requires: [02-18]
provides: [typed-apparatus-topology, revision-owned-assertions, expected-token-drafts, immutable-adoption]
affects: [02-29, scientific-setup, desktop]
tech-stack:
  added: []
  patterns: [agent-authoritative-draft-mutations, actor-scoped-receipts, explicit-conflict-review]
key-files:
  created: [Helix.Orchestration.Core/ScientificSetup/ScientificSetupValidator.cs, Helix.Orchestration.Core/ScientificSetup/ScientificUnits.cs, helix-desktop/src/components/ScientificSetup/ScientificSetup.tsx]
  modified: [Helix.Orchestration.Infrastructure/Persistence/PostgresScientificSetupRepository.cs, Helix.Orchestration.Agent/Api/ScientificSetupEndpoints.cs, helix-desktop/src-tauri/src/commands/agent.rs]
decisions:
  - "Scientific setup remains validated recorded content and never dispatches hardware."
  - "Draft conflicts retain local input until an explicit Reload or Compare action."
metrics:
  duration: "continuation execution"
  tasks: 3
  files: 17
status: complete
actuals:
  tokens: 22000
  tasks: 3
  commits: 8
plan_head_before: 4b43696
plan_head_after: 0bdd648
---

# Phase 02 Plan 19: Complete Apparatus Topology and Conflict-Safe Drafts Summary

Typed apparatus topology, recorded assertions, and immutable revision adoption now flow through Agent-authoritative expected-token drafts with explicit desktop conflict review.

## Accomplishments

- Validates the approved component, role, port, media, direction, cardinality, binding, and physical-loop catalog without granting device authority.
- Adds exact scientific unit validation and revision-owned capability/calibration/inspection assertions; assertions remain recorded evidence, never live safety or calibration approval.
- Persists expected-token draft updates, scoped idempotent receipts, named immutable publications, and explicit historical-revision adoption.
- Adds authenticated main-window-only native draft commands and a desktop Reload/Compare surface that keeps local inputs and operation identity after conflict.

## Verification Evidence

- `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~ScientificSetupTests --verbosity minimal` — 3 passed.
- `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~ScientificSetup --verbosity minimal` — 4 passed.
- `npm --prefix helix-desktop test -- --run src/components/ScientificSetup/ScientificSetup.test.tsx` — 1 passed.
- `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` — 7 passed, 1 ignored integration fixture.

## Task Commits

1. Topology and assertion validation — `1f366c5`, `c8f5122`, `a6921d6`, `7342082`.
2. Draft persistence, native access, and revision adoption — `5f5c5b6`, `b158653`, `749e659`.
3. Desktop conflict review — `0bdd648`.

## Deviations from Plan

### Auto-fixed Issues

1. **[Rule 1 - Bug] Reconstructed PostgreSQL revision timestamps through a persistence projection.**
   - **Found during:** historical adoption verification.
   - **Fix:** Materialized database timestamps safely before constructing the immutable revision record.

## Known Stubs

None.

## Next Phase Readiness

02-29 can use the actor-scoped expected-token draft and immutable-publication pattern for independently revisioned procedures and plans. No scientific setup route or desktop control dispatches equipment.

## Self-Check: PASSED

- Required summary exists in the phase directory.
- All listed commits are ancestors of this branch.
