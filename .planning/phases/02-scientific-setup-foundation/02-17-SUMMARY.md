---
phase: 02-scientific-setup-foundation
plan: "17"
subsystem: scientific-integration-acceptance
tags: [tauri, aspnet-core, postgresql, testcontainers, verification]
requires: [02-28]
provides: [fixed-native-agent-endpoint, checked-integration-harness, honest-acceptance-report]
affects: [APP-01, APP-02, APP-03, APP-04, SAMP-01, SAMP-02]
tech-stack:
  added: []
  patterns: [owned-fixed-loopback-host, checked-nonzero-verification]
key-files:
  created: [.planning/phases/02-scientific-setup-foundation/02-ACCEPTANCE.md]
  modified: [scripts/verification/phase02-e2e.ps1, tests/Helix.Orchestration.Agent.Tests/Scientific/ScientificPostgresFixture.cs, tests/Helix.Orchestration.Agent.Tests/Integration/ScientificEndToEndTests.cs]
key-decisions:
  - "The owned verification host must bind the same fixed loopback endpoint as the native client."
  - "A runtime-role reconciliation failure is acceptance evidence, not a waivable success."
metrics:
  duration: 12min
  completed: 2026-11-01
  tasks: 2
status: blocked
actuals:
  tokens: 3400
  tasks: 2
  commits: 4
plan_head_before: af55964464b79cc649854086211c9740602fce75
plan_head_after: 86fdc6b3732b8fc35adf467ccf9b1366fe952606
---

# Phase 02 Plan 17: Scientific End-to-End Acceptance Summary

**The final harness binds its owned Agent to the native endpoint and reports an actual runtime-role failure instead of asserting Phase 02 acceptance.**

## Accomplishments

- Added a failing-then-passing regression for the fixed `127.0.0.1:5080` owned Agent endpoint.
- Added Integration and Acceptance PowerShell modes that check Docker availability, child exits, and nonzero test execution.
- Wrote the final acceptance report with explicit human-needed checks and Phase 3 approval/campaign boundaries.

## Verification

- RED: `Owned_authenticated_agent_uses_the_native_loopback_endpoint` failed against the dynamically allocated port (`Expected: 5080`, `Actual: 54895`).
- GREEN: the focused fixed-endpoint assertion passed after the fixture bound port 5080.
- BLOCKED: `powershell -NoProfile -File scripts/verification/phase02-e2e.ps1 -Mode Integration` fails when `CommandReconciliationWorker` reads `command_dispatches` using the constrained runtime role. The host stops and the post-restart authenticated request returns HTTP 503.

## Deviations from Plan

### Auto-fixed Issues

1. **[Rule 1 - Bug] Fixed endpoint mismatch between native client and owned test host**
   - **Found during:** Task 1
   - **Fix:** Bound the disposable Agent host to fixed native loopback port 5080.
   - **Commit:** `d5da621`

2. **[Rule 1 - Bug] Made phase verification reject zero-test child results and failed exits**
   - **Found during:** Task 1
   - **Fix:** Added checked Integration and Acceptance command modes.
   - **Commit:** `7d32323`

## Known Stubs

None.

## Blocker

The production-composed reconciliation worker requires command-ledger access that the least-privilege scientific runtime role is denied. Resolving this requires a security/composition decision about the worker's database identity or its registration in the owned scientific host. No phase-end verification or completion routing was run.

## Self-Check: PASSED

- Required acceptance report exists.
- Task commits `a0e957a`, `d5da621`, `7d32323`, and `86fdc6b` are ancestors of HEAD.
