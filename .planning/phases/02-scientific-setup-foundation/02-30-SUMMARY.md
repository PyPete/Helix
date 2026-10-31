---
phase: 02-scientific-setup-foundation
plan: "30"
subsystem: scientific lifecycle API and desktop broker
tags: [agent, postgres, tauri, scientific-setup, authorization]
requires:
  - phase: 02-29
    provides: independent procedure and plan draft/publication storage
provides:
  - actor-scoped authenticated procedure and plan lifecycle routes
  - fixed-kind native commands for procedures and plans
affects: [02-31, scientific-setup-ui]
tech-stack:
  added: []
  patterns: [server-authoritative actor identity, fixed native routes, scoped revision pin lookup]
key-files:
  created: [Helix.Orchestration.Agent/ScientificSetup/ScientificSetupService.cs]
  modified: [Helix.Orchestration.Agent/Api/ScientificSetupEndpoints.cs, Helix.Orchestration.Infrastructure/Persistence/PostgresScientificSetupRepository.cs, helix-desktop/src-tauri/src/commands/agent.rs]
decisions:
  - Procedure revision pins are resolved by their actor-scoped revision ID before a plan is accepted.
  - Native clients use a fixed procedure-or-plan route selector rather than renderer-supplied URLs or verbs.
metrics:
  duration: continuation session
  completed: 2026-10-31
status: complete
actuals:
  tokens: 5200
  tasks: 2
  commits: 2
plan_head_before: 2dd5edf1e643640cd21d47fd8103ed34cb3f641a
plan_head_after: 883adcf7cee28cf8cd1fd564cc7c2c9a187812db
commits: 2
---

# Phase 02 Plan 30: Authenticated Procedure/Plan Lifecycle Summary

Completed server-authoritative, actor-scoped scientific procedure and plan drafts, publications, revision reads, and fixed native lifecycle routing.

## Tasks Completed

1. Added scoped repository/service lifecycle operations and authenticated Agent endpoints for both scientific kinds.
2. Added fixed-kind Tauri commands, registration, permissions, and native guard coverage.

## Verification

- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~ScientificSetupTests --verbosity minimal` — passed (2 tests).
- `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` — passed (8 tests; 1 intentional ignored live fixture).

## Decisions Made

- Procedure pins are loaded only through an actor-scoped exact revision lookup.
- The desktop command layer accepts only fixed `procedure` and `plan` kinds, never arbitrary paths or HTTP methods.

## Deviations from Plan

### Auto-fixed Issues

1. [Rule 1 - Bug] Replaced the initial empty aggregate-ID procedure revision lookup with an actor-scoped revision lookup.
- **Found during:** Task 1
- **Fix:** Added `ReadProcedureByRevisionAsync` and used it before validating plan content.
- **Commit:** `ceb58d5`

## Known Stubs

None.

## Self-Check: PASSED

- Confirmed lifecycle service and route files exist.
- Confirmed `ceb58d5` and `883adcf` are ancestors of HEAD.
