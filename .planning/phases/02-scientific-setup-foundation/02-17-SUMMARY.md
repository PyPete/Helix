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

**The authenticated native Agent/SQL tracer and all 65 Agent tests pass, but the prescribed Acceptance gate remains blocked by pre-existing repository-wide C# formatting violations.**

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
- PASS: `DurableRunspaceTests` now assert the intentional `NotSupportedException` safety boundary; the aggregate Agent suite passes 65/65.
- PASS: `dotnet build Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj --no-restore -warnaserror` completes with 0 warnings and 0 errors after the OPC UA dependency and platform-guard corrections.
- BLOCKED: the prescribed Acceptance rerun reaches `dotnet format Helix.Orchestration.slnx --verify-no-changes --no-restore`, which reports pre-existing whitespace violations across Agent, Core, Infrastructure, SiteServer, and test projects. The script therefore correctly stops before the desktop Vitest and production-build steps.

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

5. **[Rule 1 - Build safety] Restored warning-as-error Agent validation**
    - **Found during:** Task 2 Acceptance
    - **Fix:** Updated unavailable/vulnerable OPC UA and mismatched reactive dependencies, consumed async iterator cancellation, used the configuration-aware OPC endpoint selector, and guarded Windows-native registrations.
    - **Commit:** `dbced6c`

## Known Stubs

None.

## Blocker

The required formatting gate fails on existing whitespace violations outside this plan's files, including `AgentApplicationComposition.cs`, scientific Core/Infrastructure persistence files, SiteServer files, and multiple test projects. Per scope boundaries, they were not reformatted here. Acceptance and phase-completion routing cannot be marked complete until the repository's formatting baseline is repaired and the full Acceptance script completes, including the remaining desktop checks.

## Self-Check: PASSED

- Required acceptance report exists.
- Task commits `a0e957a`, `d5da621`, `7d32323`, `86fdc6b`, `4fc5127`, `d9892a9`, `17627e9`, `0218ab4`, and `dbced6c` are ancestors of HEAD.
