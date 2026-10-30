---
phase: "01"
slug: "agent-safety-boundary"
status: draft
nyquist_compliant: false
wave_0_complete: false
created: "2026-10-29"
---

# Phase 01 — Validation Strategy

> Concrete planning map for 14 executable plans and 28 tasks in seven waves. This is not implementation sign-off; native/SQL/crash tests are new execution work, not claimed passes.

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

| Task / Wave | Requirement | Threat Ref | Automated command / secure proof | Scaffold status |
|---|---|---|---|---|
| 01-01-01 / 1 | SAFE-01–04 | T-01-01–05 | `powershell -NoProfile -File scripts/verification/phase01-smoke.ps1 -Scenario Tracer` — real Tauri/native/Agent/PostgreSQL SetFlow/evidence/restart; `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~CommandAdmissionTests --verbosity minimal`; `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` | Create failing real-stack and fast admission/native tests first; fast checks supplement, never replace actual tracer |
| 01-01-02 / 1 | SAFE-01 | T-01-01/02/04/05 | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~AgentAuthenticationTests --verbosity minimal`; `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` | New host/native cases |
| 01-02-01 / 2 | SAFE-02 | T-01-06/08 | `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~CommandAdmissionTests --verbosity minimal`; inherited SafetyEngineTests command | New check truth table; extend baseline |
| 01-02-02 / 2 | SAFE-02 | T-01-08 | `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~SafetyEngineTests --verbosity minimal`; CommandAdmissionTests command above | Extend fresh/unknown safety and immediate E-stop cases |
| 01-03-01 / 2 | SAFE-02/03/04 | T-01-09/11 | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~ExecutionControlTests --verbosity minimal` | New durable control/barrier tests |
| 01-03-02 / 2 | SAFE-03/04 | T-01-10 | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~ReconciliationTests --verbosity minimal` | New real SQL crash matrix |
| 01-05-01 / 2 | SAFE-01/04 | T-01-15/17 | `npm --prefix helix-desktop test -- --run src/lib/agentClient.test.ts`; native agent_security command | New complete operation mappings |
| 01-05-02 / 2 | SAFE-01/04 | T-01-15/17 | `npm --prefix helix-desktop test -- --run src/lib/agentClient.test.ts`; native agent_security command above | Actual ConnectionConfig/useExperiment cases; local-only settings |
| 01-09-01 / 3 | SAFE-02/03/04 | T-01-25/26/28 | `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~CommandLedgerTests --verbosity minimal` | New runtime-role SQL tests; fail-loud fixture |
| 01-09-02 / 3 | SAFE-02/03 | T-01-25/27 | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~DispatchRaceTests --verbosity minimal` | New final-boundary barriers |
| 01-10-01 / 3 | SAFE-01/02 | T-01-07 | `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~WslCompilerTests --verbosity minimal` | Canonical nonmutating getters; unknown variables fail explicitly |
| 01-10-02 / 3 | SAFE-01/02 | T-01-07 | WslCompilerTests command above | Checked nonempty policy/graph compilation; no runtime authority |
| 01-12-01 / 3 | SAFE-01/04 | T-01-16/33 | `npm --prefix helix-desktop test -- --run src/hooks/useAgentHealth.test.ts src/hooks/useAgentWebSocket.test.ts src/lib/agentClient.test.ts` | Native health/event cursor, unavailable/stale/dedup cases |
| 01-12-02 / 3 | SAFE-01/04 | T-01-16 | `npm --prefix helix-desktop test -- --run src/lib/agentClient.test.ts` — render all four real discovery/palette callers | Actual caller integration cases |
| 01-04-01 / 4 | SAFE-01/02/03/04 | T-01-12 | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~DispatchBoundaryTests --verbosity minimal` | Canonical actual REST submissions/lifecycle; producer for 06 |
| 01-04-02 / 4 | SAFE-01/02/03/04 | T-01-12 | DispatchBoundaryTests command above | Actual authenticated gRPC canonical submission/lifecycle |
| 01-14-01 / 4 | SAFE-01/04 | T-01-34 | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter "FullyQualifiedName~CommandEvidenceTests.Spool\|FullyQualifiedName~CommandEvidenceTests.Saturation" --verbosity minimal` | Reserved bounded spool, saturation/listener quiescence, immutable denial records |
| 01-14-02 / 4 | SAFE-01/04 | T-01-35 | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~CommandEvidenceTests.SpoolRecovery --verbosity minimal` | Connected SQL identity/time/provenance-preserving recovery; commit-before-ack crashes |
| 01-06-01 / 5 | SAFE-01/02/03 | T-01-18/20 | `npm --prefix helix-desktop test -- --run src/components/ExecutionPanel/ExecutionPanel.test.tsx src/components/DeviceManualControl/DeviceManualControl.test.tsx` | Policy/actual DTO/control UI consumes producer 04 |
| 01-06-02 / 5 | SAFE-01/03 | T-01-19/20 | `npm --prefix helix-desktop test -- --run src/shell/HelixShell.test.tsx src/components/AgentSession/AgentSession.test.tsx` | New session; extend shell |
| 01-07-01 / 5 | SAFE-01/02/04 | T-01-21/22/23 | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~CommandEvidenceTests --verbosity minimal` | Authenticated immutable endpoints, recovered outage history/unavailable status |
| 01-07-02 / 5 | SAFE-01/04 | T-01-21/22/23 | `npm --prefix helix-desktop test -- --run src/hooks/useCommandEvidence.test.ts` | Native history/notes mounted in existing ExperimentTimeline; stable cursor/IDs |
| 01-11-01 / 5 | SAFE-01/02/03/04 | T-01-13 | DispatchBoundaryTests command above | Alternative executor/cursor effects and registry bridge |
| 01-11-02 / 5 | SAFE-01/02/03/04 | T-01-13/14 | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter "FullyQualifiedName~DispatchBoundaryTests\|FullyQualifiedName~AgentDeviceEndpointsTests\|FullyQualifiedName~AgentAuthenticationTests" --verbosity minimal` | Actual host/commit barriers; simulator-only composition |
| 01-13-01 / 6 | SAFE-04 | T-01-24/36 | `npm --prefix helix-desktop test -- --run src/shell/panels/ExperimentTimeline.test.tsx src/hooks/useCommandEvidence.test.ts` | Complete event/state/details/notes matrix, legacy and outage provenance |
| 01-13-02 / 6 | SAFE-02/04 | T-01-24 | `npm --prefix helix-desktop test -- --run src/components/SafetyPanel/SafetyPanel.test.tsx` | Complete explicit E6 empty/unknown/partial/overflow states |
| 01-08-01 / 7 | SAFE-01 | T-01-29 | Tracer smoke command against final launch config/account/listeners | Reuse mandatory real tracer |
| 01-08-02 / 7 | SAFE-01–04 | T-01-30/31/32 | `powershell -NoProfile -File scripts/verification/phase01-migrate.ps1 -CheckAndApply`; `powershell -NoProfile -File scripts/verification/phase01-smoke.ps1 -Scenario All`; `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~E2eWorkflowExecutionTests --verbosity minimal` | Final live schema/Tauri/process acceptance after all branches |

Every PLAN task has an `<automated>` immediately followed by `<fails_when>`. Each nonzero exit or zero matching tests/assertions fails; required database setup cannot silently skip. First task creates its failing smoke before any implementation; every behavior-adding task creates/extends its tests first. This is distributed Wave 0 scaffolding inside the task that owns the production behavior, not a foundation-only plan. No expansion begins before real tracer passes. Slow SQL/process gates measure latency separately from quick feedback.

ASVS level 1, block high/critical. T-01-01–36 are phase-unique; reserved T-01-SC appears in every plan. Moved threats retain their IDs: 07 in Plan 10, 13/14 in Plan 11, 16 in Plan 12, 24 in Plan 13; new 33/34/35/36 cover read reconnect, audit saturation/recovery and timeline disclosure. No installation tasks exist, and any new package needs official discovery/legitimacy audit with a blocking human gate for uncertain packages. SOURCE-AUDIT.md maps all requirements/decisions and 38 UI considerations. Three unclassified SAFE edge assumptions and six descriptor-less prohibitions remain flagged-unverified; they are not green enforcement evidence.

## Wave 0 Requirements

- [ ] Add authentication/route inventory and dispatch-boundary host fixtures with spy simulator effects.
- [ ] Add deterministic admission and concurrent reservation/termination fixtures.
- [ ] Add PostgreSQL ledger/control fixtures with visible setup failure and crash injection barriers.
- [ ] Add independently restart-surviving simulator state and reconciliation fixtures.
- [ ] Add native session/scope tests and prove native transport generation/build.
- [ ] Create first-tracer CommandAdmissionTests and native agent_security tests alongside the mandatory actual Tauri/Agent/PostgreSQL failing smoke.
- [ ] Add spool saturation/quiescence, immutable-record, crash/duplicate recovery and recovered-history/unavailable-status cases.
- [ ] Extend desktop timeline/client tests for durable history, structured diagnostics, controls and annotations.
- [ ] Start Docker Desktop or provide real local PostgreSQL before restart acceptance; daemon was unavailable during research.
- [x] Assign all 28 tasks and 36 unique numbered phase threat references after UI-SPEC and revised plans exist; execution proof remains pending.

Reuse existing frameworks; no framework installation is currently required.

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Scientist session bootstrap and revocation | SAFE-01 | Installed OS account/binaries need inspection; same-user SID pairing selected within user discretion | Verify same scientist account, native-only credential and revoked/expired denial; disclose same-user malicious-process limitation. |
| Effective deployed listener and native transport | SAFE-01 | Installed binaries/configuration were not inventoried | Launch coherent simulator-only Agent/desktop builds; inspect effective loopback listeners and confirm no lab-network control or anonymous route exceptions. |
| Timeline emphasis and outcome-first details | SAFE-04 | Visual/keyboard behavior supplements automated assertions | Verify action/device/time/actor/outcome, expandable evidence, accessible persistent-critical versus quiet-success treatment against approved UI-SPEC. |

Manual checks supplement automated route, broker and renderer tests; they do not replace them. SKELETON.md specifies simulation limits/freshness/exact effect match and reason-specific release. Scientist approves visible frozen limits before dispatch; missing approval/limits is not permission. Windows WinHTTP avoids unproven Rust protobuf generation with no new dependencies, but actual native/ACL build and real Tauri invocation remain mandatory gates.

## Validation Sign-Off

### Planning-artifact checks (2026-10-29)

- Revised GSD `frontmatter.validate --schema plan` and `verify.plan-structure` queries returned valid for all 14 PLAN files. These are artifact-schema/structure checks, not independent checker or application test results.
- Corrected read-only regex-based integrity scan returned 14 plans, 28 tasks, seven waves, 36 unique numbered threats, all 13 D citations and zero errors. Every task file occurs in its plan manifest; expansion tasks have at most five files and expansion plans at most nine. Only first tracer task has the approved exception. Same-wave manifests are disjoint and every dependency is present in an earlier wave.
- All tasks have required XML fields and paired automated/fails_when checks; every PLAN ends with complete `</output>` and contains no write-continue sentinel.
- The first custom revision scan used an invalid PowerShell string split and produced false manifest errors; that scan is discarded, never acceptance evidence.
- API/assumption/ORM/security contributions executed in array order during initial planning only; details and historical disclaimer in SOURCE-AUDIT.md and COVERAGE.md. No new package installation tasks exist.
- Same-account and same-elevation named-pipe mechanics and maintained WinHTTP lifecycle were checked against official docs; native build/real Tauri IPC/SQL acceptance is still unproven and must fail loudly if unavailable.
- No source/application/STATE/branch/commit changes performed by this planner. Preexisting STATE.md and PATTERNS.md work remains untouched. Only PLAN/SKELETON/audit/coverage artifacts and scoped ROADMAP/VALIDATION planning changes were authored.

- [ ] All tasks have automated verification or explicit Wave 0 dependencies.
- [ ] No three consecutive tasks lack automated verification.
- [ ] Wave 0 covers all missing test references.
- [ ] No watch-mode flags; filtered commands execute nonzero matching tests.
- [ ] Focused feedback latency measured against 30-second target.
- [ ] Real database crash/restart and native transport acceptance pass.
- [ ] Set `nyquist_compliant: true` only after evidence supports it.

**Approval:** pending
