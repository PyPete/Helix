---
phase: 02-scientific-setup-foundation
plan: "18"
subsystem: scientific-apparatus
tags: [postgresql, aspnet-core, tauri, react, authenticated-agent, testcontainers]
requires:
  - phase: 02-01
    provides: approved scientific setup semantics and tracer preflight
provides:
  - authenticated, idempotent apparatus create and named immutable publication
  - scoped PostgreSQL receipts, restart-safe readback, and least-privilege runtime grants
  - native desktop apparatus client and isolated production-authenticated tracer
affects: [02-19, scientific-setup, agent-api, desktop]
tech-stack:
  added: [Testcontainers PostgreSQL fixture]
  patterns: [actor-scoped operation receipts, isolated authenticated Agent host verification]
key-files:
  created: [Helix.Orchestration.Infrastructure/Persistence/PostgresScientificSetupRepository.cs, tests/Helix.Orchestration.Agent.Tests/Scientific/ScientificEndToEndTests.cs, tests/Helix.Orchestration.Agent.Tests/Scientific/ScientificPostgresFixture.cs]
  modified: [Helix.Orchestration.Agent/Api/ScientificSetupEndpoints.cs, helix-desktop/src-tauri/src/commands/agent.rs, scripts/verification/phase02-e2e.ps1]
key-decisions:
  - "Apparatus mutations are authenticated, actor-scoped, idempotent receipts rather than device commands."
  - "The tracer owns its Agent and PostgreSQL lifecycle, so it does not require a user-run service."
patterns-established:
  - "Scientific publications are durable immutable revisions with restart readback."
  - "Runtime database roles are tested for mutation denial outside the repository path."
requirements-completed: [APP-01]
actuals:
  tokens: 19000
  tasks: 2
  commits: 6
plan_head_before: d81f723669744b43aaaf12e5c34d5ca058f809f9
plan_head_after: 8ebdebb
coverage:
  - id: D1
    description: Authenticated scientist can create, publish, and read one immutable apparatus revision after Agent restart.
    requirement: APP-01
    verification:
      - kind: e2e
        ref: powershell -NoProfile -File scripts/verification/phase02-e2e.ps1 -Mode Tracer
        status: pass
    human_judgment: false
  - id: D2
    description: Native desktop apparatus client exposes the guarded scientific command path.
    requirement: APP-01
    verification:
      - kind: automated_ui
        ref: helix-desktop/src/lib/scientificApparatusClient.test.ts
        status: pass
    human_judgment: false
duration: 1d
completed: 2026-10-31
status: complete
---

# Phase 02 Plan 18: Scientific Apparatus Tracer Summary

**Authenticated, idempotent apparatus publication persists immutable PostgreSQL evidence and survives an owned Agent restart without dispatching laboratory hardware.**

## Performance

- **Duration:** 1d
- **Completed:** 2026-10-31
- **Tasks:** 2
- **Files modified:** 28

## Accomplishments

- Added typed Vessel/Source apparatus publication with actor-scoped operation receipts and stale/conflicting operation protection.
- Added Agent routes, bounded guarded Tauri commands, and a reachable desktop apparatus panel/client.
- Added isolated PostgreSQL/owned-Agent integration coverage proving authenticated mutation, idempotency, exact restart readback, runtime mutation denial, and zero hardware dispatch.

## Task Commits

1. **Task 1: Publish and reopen one typed apparatus through actual desktop/native/Agent/SQL** — `7dfde16`, `2e12ace`, `534e21b`, `9d26b29`, `f7a965f`, `8ebdebb`
2. **Task 2: Reject uncertainty and canonical corruption on the retained publication slice** — covered by the idempotency/conflict and receipt assertions in `f7a965f` and `8ebdebb`.

## Verification Evidence

`powershell -NoProfile -File scripts/verification/phase02-e2e.ps1 -Mode Tracer` passed on 2026-10-31:

- 2 focused tests passed, including `Production_authenticated_scientific_trace_is_durable_idempotent_and_never_dispatches_hardware`.
- Testcontainers provisioned isolated PostgreSQL; the owned Kestrel Agent authenticated the request, persisted the receipt, restarted, and read the same apparatus revision.
- The trace asserted idempotent replay/conflict behavior, denied unauthenticated access, denied runtime mutation, and observed zero command dispatch.

## Decisions Made

- Scientific setup remains content/evidence only; it does not invoke device drivers or dispatch commands.
- The tracer owns ephemeral PostgreSQL and Agent processes to make this gate repeatable without user services.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Security] Added least-privilege runtime schema grants and mutation-denial proof**
- **Found during:** Task 1
- **Fix:** Added dedicated runtime-grant migration and isolated denial verification.
- **Committed in:** `7dfde16`, `2e12ace`, `8ebdebb`

**Total deviations:** 1 auto-fixed security correction. No scope expansion beyond the tracer.

## Known Stubs

None.

## Next Phase Readiness

02-19 may proceed: the required cross-tier apparatus tracer and its owned integration harness are available. Subsequent topology/assertion work must preserve actor-scoped receipts, immutable publication semantics, and zero-dispatch separation.

## Self-Check: PASSED

- Summary exists at the required phase path.
- All six implementation commits are ancestors of this branch.
