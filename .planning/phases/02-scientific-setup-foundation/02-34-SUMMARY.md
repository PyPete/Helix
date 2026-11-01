---
phase: 02-scientific-setup-foundation
plan: "34"
subsystem: scientific-samples
tags: [samples, custody, agent, tauri, security]
requires: [02-33]
provides: [authenticated-sample-admission, guarded-native-sample-broker]
affects: [SAMP-01, SAMP-02]
tech-stack:
  added: []
  patterns: [actor-scoped-receipts, fixed-target-native-commands, generated-tauri-acl]
key-files:
  created: []
  modified:
    - Helix.Orchestration.Agent/Samples/SampleService.cs
    - Helix.Orchestration.Agent/Api/SampleEndpoints.cs
    - tests/Helix.Orchestration.Agent.Tests/Integration/SamplesTests.cs
    - helix-desktop/src-tauri/src/commands/agent.rs
    - helix-desktop/src-tauri/src/lib.rs
    - helix-desktop/src-tauri/permissions/agent.toml
    - helix-desktop/src-tauri/gen/schemas/acl-manifests.json
decisions:
  - Explicit authenticated confirmation is the only native route that admits collection facts; plan completion is never interpreted as collection evidence.
  - Native sample IPC is constrained to fixed loopback Agent endpoints, the main window, and the opaque native session.
metrics:
  duration: continuation session
  completed: 2026-11-01
  tasks: 2
status: complete
plan_head_before: a856fed3d44a95b70b23686e91b7f6744977e1e6
plan_head_after: 359ddd6248315147ae500433524c709a29c95731
commits: 2
actuals:
  tokens: 56492
  tasks: 2
  commits: 2
---

# Phase 02 Plan 34: Authenticated Sample Admission Summary

Explicit scientist-confirmed collection and append-only custody evidence now flow through actor-scoped Agent APIs and fixed, permissioned native desktop commands.

## Completed Tasks

1. **Confirm and append custody through verified Agent scope** — Added authenticated sample list/read/confirm/event/receipt routes backed by the scoped service and PostgreSQL repository, with host tests for empty, ordering, replay, and adjacency isolation. (`21d4bc3`)
2. **Register guarded collection/custody/history native commands** — Added fixed local `sample_list`, `sample_read`, `sample_confirm`, `sample_append_event`, and `sample_events` commands, generated ACL permission metadata, input bounds, status rejection, and native security tests. (`359ddd6`)

## Verification

- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~SamplesTests --verbosity minimal` — passed (3 tests).
- `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` — passed (11 tests; 1 intentionally ignored owned-host fixture).

## Decisions Made

- Collection is admitted only from an explicit authenticated confirmation with the original operation identity; no completion ordering or neighboring activity can infer a sample.
- Tauri capability access remains main-window-only through `allow-agent-broker`; generated ACL metadata is regenerated from the explicit permission list.

## Deviations from Plan

### Auto-fixed Issues

1. **[Rule 2 - Critical functionality] Added runtime sample scope and role support**
- **Found during:** Task 1
- **Issue:** The listed endpoint scope alone could not read or persist actor-scoped sample facts under the runtime database role.
- **Fix:** Added the scoped repository listing contract, receipt materialization, and migration grants required for authenticated routes to function.
- **Files modified:** `Helix.Orchestration.Core/Samples/ISampleRepository.cs`, `Helix.Orchestration.Infrastructure/Persistence/PostgresSampleRepository.cs`, `postgres/migrations/008_scientific_samples.sql`
- **Commit:** `21d4bc3`

## Known Stubs

None.

## Self-Check: PASSED

- Required Agent, native broker, permission, generated ACL, test, and summary artifacts exist.
- Task commits `21d4bc3` and `359ddd6` are ancestors of the plan head.
