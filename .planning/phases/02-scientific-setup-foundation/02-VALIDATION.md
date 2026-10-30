---
phase: "02"
slug: "scientific-setup-foundation"
status: draft
nyquist_compliant: false
wave_0_complete: false
created: "2026-10-30"
---

# Phase 02 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

Seeded from `02-RESEARCH.md` §Validation Architecture (lines 436–515). This is a **draft strategy, not execution evidence**. No tests, builds, migrations, native acceptance, latency measurement or sign-off have been performed for this artifact.

Approved UI-SPEC and revised plans exist; historical missing-UI narration is superseded. Execution evidence remains pending. Final ownership follows 23 active plans/51 tasks/22 waves; 02-02–02-16 and 02-20–02-23 are superseded specifications only. P-05 resolves sizing for ONLY 18 T1's 25-file tracer; every other task has at most five files. All scientific approval, actual desktop tracer, baseline and execution review gates remain binding.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Existing xUnit 2.x and Testcontainers.PostgreSql 4.x for .NET; desktop Vitest 4.1.7 with Testing Library/jsdom; Cargo for native tests. Versions are research observations/declarations, not new installation recommendations. |
| **Config file** | Desktop: `helix-desktop/vitest.config.ts`, using `src/test/setup.ts`. .NET project configuration and the existing `PostgresFixture` in `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs`; copied SQL migrations load in ordinal filename order. |
| **Quick run command** | `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --filter FullyQualifiedName~ScientificSetup` — proposed subset; Wave 0 must create suites and prove nonzero discovery. Use the relevant requirement-specific command below when another boundary changes. |
| **Full suite command** | Execute the complete affected-suite command block below from the repository root; it is not a watch-mode command. |
| **Estimated runtime** | Unmeasured. Research proposes a warm unit-subset target under 30 seconds; container startup, restore/build and full integration timing remain unknown. |

### Full affected suite and build checks

Commands are grounded in the research; they are execution templates, not passed results.

```powershell
dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj
dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj
npm test --prefix helix-desktop -- --run
cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml
npm run build --prefix helix-desktop
dotnet build Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj -warnaserror
dotnet format --verify-no-changes
```

Run each command with exit-code enforcement and retain its output; an earlier failure must not be concealed by a later successful command. SQL tests must use owned isolated Testcontainers, not the live user's PostgreSQL database. Missing infrastructure or zero matching tests is not success. Existing framework availability does not imply scientific suites exist. Use the established npm --prefix helix-desktop test -- --run and npm --prefix helix-desktop run build forms in executable plans.

### Owned native integration lifecycle

02-18 creates phase02-e2e.ps1 and ScientificEndToEndTests as the sole owned-host lifecycle; 02-17 expands it. Tracer mode runs first, then expansions, then Integration and Acceptance modes. .NET orchestration owns disposable PostgreSQL, real Kestrel Agent composition and existing authenticated native Windows session, waits for readiness and executes the actual fully qualified Rust agent_security_scientific_e2e name using -- --ignored --exact --nocapture. Explicit integration requires exactly one executed native test, zero skips, checked exits, actual SQL assertions and cleanup of owned handles only. Host/session unavailability fails explicit integration. The ignored attribute isolates this live fixture from ordinary cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security; its intentional exclusion is not integration success. Acceptance deliberately invokes the owned-host Integration scenario, never a freestanding live fixture. Child-process-local configuration avoids altering unrelated test environments.

Actual authenticated desktop click-through remains a separate human evidence gate before 02-19; native production functions and jsdom alone do not prove Tauri entry-point reachability. APP-04 positive proof uses authority exclusively in the test host; production unavailable-authority UI is a separate fail-closed check, not campaign approval/execution evidence.

### Baseline and dependency caveats

Research records Phase 1 as administratively complete **with user-approved waivers**, not fully verified. Full smoke/UI/Tauri and distinct-role migration/apply acceptance were waived/unrun; clean production-build acceptance was waived/unpassed. Previously recorded diagnostics include `NU1603`, `NU1902` and `MSB3277`. These are prior records, not results rerun for this strategy.

Research also records a selected prerelease .NET 11 SDK and a prerelease .NET 10 SDK. Establish the actual affected restore/build/test baseline and record SDK selection before execution; neither SDK presence nor missing stable-version metadata establishes compatibility. Record prior failures separately from new regressions. Do not weaken warnings, authentication, immutable-role controls or assertions to claim a passing baseline. Any waiver remains a waiver, never green verification.

---

## Sampling Rate

- **After every task commit:** Run its relevant command from the verification map; the Core ScientificSetup subset is the default quick check. Use focused SQL/native checks whenever those boundaries change.
- **After every plan wave:** Run the full affected suite and build checks above; record test discovery, exit codes and failures.
- **Before `/gsd-verify-work`:** Full affected suite must be green, isolated distinct-role SQL protections proven, and authenticated native acceptance recorded. Unpassed baseline gates require an explicit separate disposition, not an implicit pass.
- **Max feedback latency:** Target under 30 seconds for warmed unit subsets only; unmeasured and not yet satisfied. Measure in Wave 0 and revise sampling if needed. No latency claim is made for container startup or the full suite.

---

## Per-Task Verification Map

The following entries identify actual revised task owners, not completed tests. Every owner creates its failing regression suite before implementation and proves nonzero discovery. Threat references point to numbered PLAN registers; no provisional threat IDs count as registered coverage.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 02-01-T1/T2 | 01 | 1 | All six | T-02-01 | Approval and honest nonzero baseline, no scientific pass inferred | Preflight | Per-task commands in 02-01 | Pending | ⬜ pending |
| 02-18-T1/T2 | 18 | 2 | APP-01 | T-02-38/39/40 | Real desktop/native/session/Agent/SQL publication/restart; actual desktop gate | E2E + human | `powershell -NoProfile -File scripts/verification/phase02-e2e.ps1 -Mode Tracer` | Planned deliverable | ⬜ pending |
| 02-19-T1/T2 | 19 | 3 | APP-01 | T-02-42 | Full topology/discovery/history and same-token concurrency | Core + SQL + UI | ScientificSetupTests and ScientificSetup.test.tsx commands in 19 | Planned suites | ⬜ pending |
| 02-19-T3 | 19 | 3 | APP-02 | T-02-41 | Exact assertions/units/dates/provenance; no authority | Unit + UI | `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~ScientificSetupTests --verbosity minimal` | Planned suite | ⬜ pending |
| 02-29-T1/T2 | 29 | 4 | APP-03 | T-02-61 | Complete independent content, declared exact parameters, atomic SQL publication/pins | Core + SQL | Per-task ScientificSetupTests commands in 29 | Planned suites | ⬜ pending |
| 02-30-T1/T2 | 30 | 5 | APP-03 | T-02-62 | Scoped full lifecycle routes and guarded native registration | Agent + Cargo | Per-task ScientificSetupTests/agent_security commands in 30 | Planned suites | ⬜ pending |
| 02-31-T1/T2 | 31 | 6 | APP-03 | T-02-63 | Complete procedure/plan fields/states and separate reachable entries | Vitest | Per-task client/form/shell commands in 31 | Planned suites | ⬜ pending |
| 02-32-T1/T2/T3 | 32 | 7 | APP-03 | T-02-64 | Reusable source-linked derive and explicit compatible adopt | SQL + Cargo + UI | Per-task SQL/native/ProcedurePlans commands in 32 | Planned suites | ⬜ pending |
| 02-33-T1/T2 | 33 | 8 | SAMP-01,SAMP-02 | T-02-65 | Stable UUID/origin and atomic sequence-safe append-only correction history | Core + SQL | Per-task SampleFactsTests/SamplesTests commands in 33 | Planned suites | ⬜ pending |
| 02-34-T1/T2 | 34 | 9 | SAMP-01,SAMP-02 | T-02-66 | Explicit confirmation, classified adjacency/empty/ordering and scoped native custody | Agent + Cargo | Per-task SamplesTests/agent_security commands in 34 | Planned suites | ⬜ pending |
| 02-35-T1/T2/T3 | 35 | 10 | SAMP-01,SAMP-02 | T-02-67 | Complete exact receipts/confirmation/custody states and Samples reachability | Vitest | Per-task client/Samples/shell commands in 35 | Planned suites | ⬜ pending |
| 02-36-T1/T2 | 36 | 11 | SAMP-02 | T-02-68 | Atomic attributable many-parent/new-child lineage, relevant quantities and bounded SQL | Core + SQL | Per-task SampleFactsTests/SamplesTests commands in 36 | Planned suites | ⬜ pending |
| 02-37-T1/T2 | 37 | 12 | SAMP-02 | T-02-69 | Per-reference authorization, complete transform/lineage routes and native guards | Agent + Cargo | Per-task SamplesTests/agent_security commands in 37 | Planned suites | ⬜ pending |
| 02-38-T1/T2 | 38 | 13 | SAMP-02 | T-02-70 | Complete parent/activity/child preview, linked evidence and all states | Vitest | Per-task client/Samples/SampleLineage commands in 38 | Planned suites | ⬜ pending |
| 02-39-T1/T2 | 39 | 14 | APP-04 | T-02-71 | Immutable policy content, independent approval provenance, exact SQL freeze | Core + SQL | Per-task PolicyReferenceTests commands in 39 | Planned suites | ⬜ pending |
| 02-40-T1/T2 | 40 | 15 | APP-04 | T-02-72 | Trusted test-host positive freeze, unavailable production authority, exact routes | Agent + SQL | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~FrozenSetupTests --verbosity minimal` | Planned suite | ⬜ pending |
| 02-41-T1/T2 | 41 | 16 | APP-04 | T-02-73 | Guarded fixed commands and exact client refs/authority/receipt validation | Cargo + Vitest | Per-task agent_security/scientificClient commands in 41 | Planned suites | ⬜ pending |
| 02-42-T1/T2/T3 | 42 | 17 | APP-04 | T-02-74 | Complete assembly states/fifth entry and positive SQL immutable reconstruction | Vitest + Agent + SQL | Per-task FrozenAssembly/shell/FrozenSetupTests/FrozenReferenceTests commands in 42 | Planned suites | ⬜ pending |
| 02-24-T1/T2 | 24 | 18 | APP-04 | T-02-51/52 | Actual canonical consumer and current exact authority validation | Agent + SQL | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~FrozenSetupTests --verbosity minimal` | Planned suite | ⬜ pending |
| 02-25-T1/T2 | 25 | 18 | All six | T-02-53/54 | Actual runtime-role denials and migration crash/replay tracking | SQL + runner | Per-task ScientificSchemaTests/ScientificMigrationTests commands in 25 | Planned suites | ⬜ pending |
| 02-26-T1/T2 | 26 | 19 | All six | T-02-55/56 | Ordinary isolated native guards and exact typed receipt recovery | Cargo + Vitest + Agent | Per-task agent_security/client commands in 26 | Planned suites | ⬜ pending |
| 02-27-T1/T2;02-28-T1/T2/T3 | 27,28 | 20,21 | All six | T-02-57/58/59/60 | All five entries, close/restore, full states/recovery/accessibility | Vitest + human | Per-task UI/client/shell commands in 27/28 | Planned suites | ⬜ pending |
| 02-17-T1/T2 | 17 | 22 | All six | T-02-36/37 | Owned-host complete native integration and honest handoff | E2E + acceptance | `powershell -NoProfile -File scripts/verification/phase02-e2e.ps1 -Mode Acceptance` | Planned deliverable | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky. No row is green.*

### Threat behavior groupings (not additional threat IDs)

- **Authority/attribution bypass:** Reject client-invented actor/policy/device authority, wrong sessions/windows, foreign refs/receipts and setup dispatch; credentials native-only. Active refs: T-02-38/41/52/55/62/66/69/71/72/73.
- **Immutable evidence corruption:** Reject stale publications, inconsistent refs, runtime update/delete/truncate/safeguard changes and cache/latest authority after restart. Active refs: T-02-39/42/51/53/54/61/65/68/71/74.
- **Duplicate/partial/concurrent processing:** Race actor-bound keys/heads, inject precommit and lost-ACK failures, separate SQL connections and exact/+1 graph/unit/quantity budgets. Active refs: T-02-40/42/56/59/61/64/65/68.

For APP-01 and APP-03, Core unit checks are supplemented by SQL publication/reference tests. For APP-04, SQL freeze checks are supplemented by Agent execution-consumer tests; an API returning IDs alone is insufficient. For SAMP-01, add UI confirmation tests; for SAMP-02, add Core projection/lineage tests. These supplemental suites are Wave 0 requirements below, not implemented coverage.

---

## Wave 0 Requirements

All entries remain uncompleted. 01 owns baseline/approval; 18 the real tracer/shared harness; 19 and 29–42 complete bounded feature regressions; 24 canonical consumption; 25 role/migration checks; 26–28 adversarial/client/state regressions; 17 final integration/acceptance. Each packet creates its fail-first tests before its automated verification. No empty passing tests, zero discovery or skipped SQL may satisfy these gates.

- [ ] Establish affected restore/build/test baseline, SDK choice, nonzero discovery and separately recorded Phase 1 caveats.
- [ ] `tests/Helix.Orchestration.Core.Tests/ScientificSetup/` — APP-01–APP-04 topology/revision/plan/freeze suites.
- [ ] `tests/Helix.Orchestration.Core.Tests/Samples/` — SAMP-01–SAMP-02 event projection, quantity validation and lineage suites.
- [ ] Infrastructure scientific setup/sample suites — isolated fresh-install/upgrade/repeat-apply migrations, reconstruction, concurrency, partial-write rollback and distinct migration/runtime-role mutation-denial tests.
- [ ] Reuse the existing PostgreSQL fixture with explicit availability assertions; reject silent-return skips and require nonzero filtered test discovery. Confirm environment variables cannot redirect destructive tests to the live database.
- [ ] Agent scientific setup/sample integration suites — verified authentication/attribution, idempotency-result lookup, zero dispatch and frozen execution-reference consumer with authoritative policy provider.
- [ ] `helix-desktop/src/components/ScientificSetup/ScientificSetup.test.tsx` — draft/publish conflicts, exact-version display, plan pin/derivation and representation identity preservation.
- [ ] `helix-desktop/src/components/Samples/Samples.test.tsx` — separate collection confirmation, errors/uncertainty, custody and lineage views.
- [ ] Native scientific-command tests — wrong-window/destination/redirect/session/size rejection; bounded pagination and lost-acknowledgement handling without a fresh operation identity.
- [ ] Representative topology/lineage size fixtures, scientific vocabulary/unit rules, correction semantics, transitive reference-consistency rules and canonical hash test vectors approved before contracts are fixed.
- [ ] Document supervised authenticated Windows and distinct-role database deployment acceptance below; no production migration is authorized by this draft.
- [ ] Measure warmed quick-test duration and finalize per-task/plan/wave sampling assignments. No three consecutive execution tasks may lack automated verification.
- [ ] Existing test frameworks are reused; no new package installation is anticipated. Any new external dependency requires the legitimacy protocol before installation.

---

## Manual-Only Verifications

These are **manual deployment/native acceptance checks**, in addition to automated unit/integration tests. All are pending. Mocked React/canvas tests and isolated SQL role tests cannot certify the actual installed native/session flow or deployed database identities.

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Authenticated Tauri setup editing/publication and exact-version presentation | APP-01–APP-04 | Actual Windows broker, session and native IPC need real acceptance; component mocks do not exercise them | First prove 02-18 apparatus publication/restart in actual authenticated Tauri before 02-19. After expansions, use the isolated owned acceptance Agent/database to inspect simulator bindings, independent procedures/plans, derive/adopt and exact frozen refs. Positive freeze uses trusted test-host authority only; actual production unavailable-authority state must block. Publish later versions; verify original refs fixed and zero setup dispatch. Record errors, evidence and actor attribution. |
| Separate collection confirmation and custody/lineage display | SAMP-01–SAMP-02 | Real scientist interaction and native acknowledgement/uncertainty path require acceptance | In isolated acceptance data, complete a planned collection step and verify no confirmed sample is created. Confirm separately; record stable ID, relabel without identity change, append custody, perform split/pool and inspect attributable parents/children/units. Interrupt acknowledgement and resolve the original operation; verify no duplicate fact. |
| Distinct-role database migration/deployment acceptance | APP-01–APP-04, SAMP-01–SAMP-02 | Isolated automated tests do not certify actual deployment role ownership/grants; application against user data needs owner approval | Confirm separate migration/runtime identities without revealing secrets. Retain isolated migration/replay/negative-role test evidence first. Obtain explicit deployment approval and backup/recovery disposition before any live apply; record migration tracking and runtime denial of immutable update/delete/truncate/safeguard changes. Do not run destructive verification on the live user database. |
| Baseline and prior waiver disposition | All six | Phase 1 waived/unpassed gates need explicit human disposition, not inherited passing status | Present fresh affected baseline output alongside prior Phase 1 records. Record any failure/waiver separately from passed tests; retain pending native/full smoke/migration/build acceptance until actually demonstrated or explicitly dispositioned. |

Workspace comparison/redesign approval remains Phase 02.1 work. These checks consume the approved scientific UI-SPEC; they do not approve a workspace redesign.

---

## Validation Sign-Off

- [ ] UI gate satisfied and actual plans/task assignments exist.
- [ ] All tasks have `<automated>` verify or explicit Wave 0 dependencies.
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify.
- [ ] Wave 0 covers all missing test references and filters discover nonzero tests.
- [ ] Execution commands use no watch-mode flags.
- [ ] Warm unit feedback latency measured below the proposed 30-second target; full-suite timing recorded separately.
- [ ] Full affected tests/build checks green; prior baseline failures/waivers separately recorded.
- [ ] Isolated database immutability/concurrency/fault tests and real native/database manual acceptance recorded.
- [ ] `wave_0_complete: true` justified by completed evidence.
- [ ] `nyquist_compliant: true` set only after evidence-based validation.

**Approval:** pending. `status: draft`, `nyquist_compliant: false` and `wave_0_complete: false` remain unchanged until the appropriate validation workflow establishes evidence. This document does not claim execution or acceptance.
