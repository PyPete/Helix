---
phase: "01"
slug: "agent-safety-boundary"
status: draft
nyquist_compliant: false
wave_0_complete: false
created: "2026-10-29"
---

# Phase 01 — Validation Strategy

> Draft seeded from 01-RESEARCH.md. Assign task IDs, waves, and unique threat references after PLAN.md files exist. This is not implementation sign-off.

## Test Infrastructure

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9, ASP.NET Core host tests, Vitest 4.1.7, Rust native tests |
| Config | Existing Core/Agent/Infrastructure test csproj files; helix-desktop/package.json; helix-desktop/src-tauri/Cargo.toml |
| Quick baseline | `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~SafetyEngineTests --verbosity minimal` |
| Phase suite | Run each affected Core, Agent, Infrastructure, desktop, and native suite below |
| Estimated runtime | Not measured for new phase tests; target focused feedback under 30 seconds, database/restart suite measured separately |

The baseline has 9 passing safety tests and 14 passing desktop tests, but does not establish Phase 1 acceptance. Every test command fails on non-zero exit; filtered suites also fail acceptance when no matching tests execute. Database unavailability or silently bypassed assertions cannot count as green.

## Sampling Rate

- After every task commit: run the affected focused unit, host, renderer, or native tests; baseline safety tests alone are insufficient.
- After every plan wave: run all affected suites and verify host composition and execution authority.
- Before `/gsd-verify-work`: all affected suites, native transport smoke, and real PostgreSQL crash/restart tests must pass.
- Target quick feedback latency: 30 seconds; measure slower integration gates separately.

### Affected-suite commands

From the repository root, execute separately:

```powershell
dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --verbosity minimal
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --verbosity minimal
dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --verbosity minimal
dotnet format --verify-no-changes
```

From `helix-desktop`, execute separately:

```powershell
npm test -- --run
npm run build
```

From `helix-desktop/src-tauri`, execute separately:

```powershell
cargo test
cargo check
```

Non-zero exit fails each command. Infrastructure tests must demonstrate a connected database and executed assertions, not a silent return. Native generation/build is unproven; managed warnings-as-errors compliance needs a separately planned build check.

## Per-Task Verification Map

Task IDs, plan numbers, waves, and threat IDs are pending planning. Named test classes below are proposed Wave 0 targets, not existing coverage.

| Task ID / Plan / Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Target | File Exists | Status |
|-----------------------|-------------|------------|-----------------|-----------|------------------|-------------|--------|
| Pending planning | SAFE-01 | Pending threat model | Every read/write/gRPC/upgrade request authenticated; invalid sessions denied and audited; native scope restricted | Host/native | AgentAuthenticationTests and native scope/session tests | Wave 0 | Pending |
| Pending planning | SAFE-01 | Pending threat model | Manual, compiled/raw workflow, alternate executor and variable-read paths cannot bypass guarded dispatch; no real-device registration | Host/architecture | DispatchBoundaryTests with spy effect counts | Wave 0 | Pending |
| Pending planning | SAFE-02 | Pending threat model | Each capability/safety/lease/resource/budget/termination check independently blocks effect; rejection versus hold preserved | Unit/host | CommandAdmissionTests and DispatchBoundaryTests | Wave 0 | Pending |
| Pending planning | SAFE-02 | Pending threat model | Compile-to-dispatch changes, lease expiry, competing reservations, exhausted budgets and stop races cannot leak effects | Concurrency/database | DispatchRaceTests and CommandLedgerTests | Wave 0 | Pending |
| Pending planning | SAFE-03 | Pending threat model | Pause/hold/abort durable; resume advances cursor without replay; abort remains terminal | Host/database | ExecutionControlTests and ReconciliationTests | Wave 0 | Pending |
| Pending planning | SAFE-03 | Pending threat model | Crash windows around intent, effect, outcome and acknowledgement reconcile once; unreadable/mismatched state held; independent holds persist | Process/database | ReconciliationTests with recreated host/simulator and durable PostgreSQL | Wave 0 | Pending |
| Pending planning | SAFE-04 | Pending threat model | Intent/check evidence precedes effect; actor/correlation retained; idempotency and immutable facts enforced | Repository/host | CommandLedgerTests and CommandEvidenceTests | Wave 0 | Pending |
| Pending planning | SAFE-04 | Pending threat model | Completed/reconnected history uses canonical sequence and deduplication; critical outcomes emphasized; annotations append-only | Renderer | Extend ExperimentTimeline.test.tsx and broker-client tests | Partial display tests only | Pending |

Planner must replace each target with concrete `<automated>` commands and immediate `<fails_when>` statements, anchored to created test files and nonempty matching test counts.

## Wave 0 Requirements

- [ ] Add authentication/route inventory and dispatch-boundary host fixtures with spy simulator effects.
- [ ] Add deterministic admission and concurrent reservation/termination fixtures.
- [ ] Add PostgreSQL ledger/control fixtures with visible setup failure and crash injection barriers.
- [ ] Add independently restart-surviving simulator state and reconciliation fixtures.
- [ ] Add native session/scope tests and prove native transport generation/build.
- [ ] Extend desktop timeline/client tests for durable history, structured diagnostics, controls and annotations.
- [ ] Start Docker Desktop or provide real local PostgreSQL before restart acceptance; daemon was unavailable during research.
- [ ] Assign every task and unique phase threat reference after UI-SPEC and plans exist.

Reuse existing frameworks; no framework installation is currently required.

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Scientist session bootstrap and revocation | SAFE-01 | OS-user/scientist pairing mechanics require confirmation | Approve the pairing model; verify native credential is not exposed to renderer and revoked sessions fail closed. |
| Effective deployed listener and native transport | SAFE-01 | Installed binaries/configuration were not inventoried | Launch coherent simulator-only Agent/desktop builds; inspect effective loopback listeners and confirm no lab-network control or anonymous route exceptions. |
| Timeline emphasis and outcome-first details | SAFE-04 | Visual contract is pending UI-SPEC | Verify action/device/time/actor/outcome summary, expandable evidence, accessible critical-event emphasis and quiet ordinary successes against UI-SPEC. |

Manual checks supplement automated route, broker and renderer tests; they do not replace them. Concrete numerical policy limits, observation freshness and hold-release semantics must be confirmed before defaults are locked.

## Validation Sign-Off

- [ ] All tasks have automated verification or explicit Wave 0 dependencies.
- [ ] No three consecutive tasks lack automated verification.
- [ ] Wave 0 covers all missing test references.
- [ ] No watch-mode flags; filtered commands execute nonzero matching tests.
- [ ] Focused feedback latency measured against 30-second target.
- [ ] Real database crash/restart and native transport acceptance pass.
- [ ] Set `nyquist_compliant: true` only after evidence supports it.

**Approval:** pending
