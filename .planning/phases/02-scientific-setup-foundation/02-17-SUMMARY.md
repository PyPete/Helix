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
  patterns: [owned-fixed-loopback-host, checked-nonzero-verification, separate-reconciliation-identity]
key-files:
  created: [.planning/phases/02-scientific-setup-foundation/02-ACCEPTANCE.md]
  modified: [Helix.Orchestration.Agent/AgentApplicationComposition.cs, Helix.Orchestration.Agent/Security/SessionBootstrapWorker.cs, Helix.Orchestration.Agent/Api/ScientificSetupEndpoints.cs, scripts/verification/phase02-e2e.ps1, tests/Helix.Orchestration.Agent.Tests/Scientific/ScientificPostgresFixture.cs, tests/Helix.Orchestration.Agent.Tests/Scientific/ScientificEndToEndTests.cs]
key-decisions:
  - "The owned verification host must bind the same fixed loopback endpoint as the native client."
  - "Reconciliation and security-audit draining use a dedicated infrastructure connection while the scientific runtime remains least privileged."
metrics:
  duration: 42min
  completed: 2026-11-01
  tasks: 2
status: blocked
actuals:
  tokens: 5100
  tasks: 2
  commits: 8
plan_head_before: af55964464b79cc649854086211c9740602fce75
  plan_head_after: 17627e99d48003ac0f43aab24e0b977d84569e54
---

# Phase 02 Plan 17: Scientific End-to-End Acceptance Summary

**The native authenticated Agent/SQL tracer passes under a separate reconciliation identity while legacy runspace tests still block aggregate acceptance.**

## Accomplishments

- Added a failing-then-passing regression for the fixed `127.0.0.1:5080` owned Agent endpoint.
- Reconciliation and durable security-audit drain now use the separately privileged `ConnectionStrings:Reconciliation` identity; the scientific runtime remains denied command-ledger access.
- The owned native loopback tracer passes authenticated creation, idempotent replay/conflict, restart readback using the same authenticated scientist, and no-hardware/immutable-role assertions.
- Marked scientific endpoint handler services explicitly as DI dependencies so direct endpoint hosts do not infer them as HTTP bodies.

## Verification

- RED: `Owned_authenticated_agent_uses_the_native_loopback_endpoint` failed against the dynamically allocated port (`Expected: 5080`, `Actual: 54895`).
- GREEN: the focused fixed-endpoint assertion passed after the fixture bound port 5080.
- PASS: `powershell -NoProfile -File scripts/verification/phase02-e2e.ps1 -Mode Integration` completed with three .NET tests and exactly one ignored native fixture test.
- PASS: focused `ScientificEndToEndTests.Production_authenticated_scientific_trace_is_durable_idempotent_and_never_dispatches_hardware` and `ScientificSetupEndpointsTests` passed.
- BLOCKED: Acceptance runs 65 Agent tests; 61 pass and two unrelated legacy `DurableRunspaceTests` still expect execution that the safety boundary intentionally disables. The original Acceptance run also exposed two endpoint-DI failures, fixed in `17627e9`; its full rerun remains blocked by the legacy failures.

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

3. **[Rule 2 - Missing critical infrastructure identity] Kept security audit draining under the separate reconciliation identity**
   - **Found during:** Task 1
   - **Fix:** Constructed both `SessionBootstrapWorker` and `CommandReconciliationWorker` with the dedicated infrastructure ledger rather than the restricted runtime ledger.
   - **Commit:** `4fc5127`

4. **[Rule 1 - Bug] Declared scientific endpoint services as DI dependencies**
   - **Found during:** Task 2 Acceptance
   - **Fix:** Added explicit `FromServices` binding so minimal API route construction cannot treat `ScientificSetupService` as a request body.
   - **Commit:** `17627e9`

## Known Stubs

None.

## Blocker

`DurableRunspaceTests.StartAsync_DelegatesToWorkflowExecutor_AndCompletes` and `PauseAsync_CancelsCurrentExecution_AndSetsPausedStatus` expect the deliberately disabled legacy runspace execution path. They fail with `NotSupportedException: Legacy DurableRunspace execution is disabled. Use canonical dispatch with a persisted cursor.` This is outside the scientific reconciliation scope; acceptance and phase completion routing cannot be marked complete until the owning safety/runspace work updates or retires those stale tests.

## Self-Check: PASSED

- Required acceptance report exists.
- Task commits `a0e957a`, `d5da621`, `7d32323`, `86fdc6b`, `4fc5127`, `d9892a9`, and `17627e9` are ancestors of HEAD.
