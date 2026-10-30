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

The UI gate remains blocked (`frontend=true`, `hasUiSpec=false`, `block=true`). No planner or pattern mapper has started. Task IDs, plan/wave assignments, proposed test names and threat references below must be finalized after the UI gate and plans exist; none are authoritative plan assignments.

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

Run each command with exit-code enforcement and retain its output; an earlier failure must not be concealed by a later successful command. SQL tests must use isolated Testcontainers or an explicitly isolated test database, not the live user's PostgreSQL database. Missing infrastructure or zero matching tests is not success. Existing framework availability does not imply the proposed scientific suites exist.

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

**Every ID prefixed `02-TBD-` is a provisional coverage placeholder, not an existing task.** Plan and wave are TBD until plans exist. All scientific test files/suites below are proposed Wave 0 gaps; their names/filters must be finalized and discover nonzero tests. Threat IDs are local provisional strategy labels, not references to an existing approved threat model.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 02-TBD-APP01 | TBD | TBD | APP-01 | T-02-P01 / P02 | Typed components, ports, roles, connections and device bindings; one draft; explicit immutable publication; stale-token rejection | Unit + SQL integration | `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --filter FullyQualifiedName~ScientificSetup` | Proposed suite — missing/W0 | ⬜ pending |
| 02-TBD-APP02 | TBD | TBD | APP-02 | T-02-P01 / P03 | Validate capability/inspection assertions, exact units, bounds and provenance on Agent; zero physical dispatch from setup | Unit + Agent integration | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter FullyQualifiedName~ScientificSetup` | Proposed suite — missing/W0 | ⬜ pending |
| 02-TBD-APP03 | TBD | TBD | APP-03 | T-02-P02 | Independent procedure/plan revisions; published procedure pin and source-linked derivation; representation changes preserve identity/meaning | Unit + UI | `npm test --prefix helix-desktop -- --run src/components/ScientificSetup/ScientificSetup.test.tsx` | Proposed file — missing/W0 | ⬜ pending |
| 02-TBD-APP04 | TBD | TBD | APP-04 | T-02-P02 / P03 | Durable frozen apparatus/procedure/plan/policy references survive later publication and repository reconstruction; execution consumer uses frozen refs | SQL + Agent integration | `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --filter FullyQualifiedName~ScientificSetup` | Proposed suite — missing/W0 | ⬜ pending |
| 02-TBD-SAMP01 | TBD | TBD | SAMP-01 | T-02-P01 / P03 | Only explicit verified scientist confirmation creates collection; workflow completion does not; repeated request returns same sample | Agent + UI | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter FullyQualifiedName~Samples` | Proposed suite — missing/W0 | ⬜ pending |
| 02-TBD-SAMP02 | TBD | TBD | SAMP-02 | T-02-P02 / P03 | Label-independent identity, append-only attributed custody, atomic many-parent/many-child transformations, cycle/race rollback | SQL integration + unit | `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --filter FullyQualifiedName~Samples` | Proposed suite — missing/W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky. No row is green.*

### Provisional threat coverage

- **T-02-P01 — authority/attribution bypass:** Reject client-invented verified actor/policy/device assertions, wrong or terminated sessions, other windows, unauthorized record/receipt access and setup-to-driver dispatch. Keep credentials native-only.
- **T-02-P02 — immutable scientific evidence corruption:** Test stale publications, frozen transitive-reference consistency, forbidden SQL update/delete/truncate/safeguard changes under the runtime role, and restart reconstruction without cache authority.
- **T-02-P03 — duplicate/partial/concurrent processing:** Race identical and mismatched actor-bound idempotency keys; inject failure before commit and acknowledgement loss after commit; exercise custody/lineage on separate SQL connections; reject oversize/deep graphs, unknown units and non-finite quantities.

For APP-01 and APP-03, Core unit checks are supplemented by SQL publication/reference tests. For APP-04, SQL freeze checks are supplemented by Agent execution-consumer tests; an API returning IDs alone is insufficient. For SAMP-01, add UI confirmation tests; for SAMP-02, add Core projection/lineage tests. These supplemental suites are Wave 0 requirements below, not implemented coverage.

---

## Wave 0 Requirements

All entries are proposed/uncompleted. Finalize filenames and plan ownership after planning; do not satisfy these by adding empty stubs that pass or skipping missing infrastructure.

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
| Authenticated Tauri setup editing/publication and exact-version presentation | APP-01–APP-04 | Actual Windows broker, session and native IPC need real acceptance; component mocks do not exercise them | After UI gate/plans/implementation, launch the authenticated native desktop against an isolated acceptance Agent/database. Create a valid simulator-bound draft, publish explicitly, inspect exact IDs, derive/adopt a procedure and assemble consistent frozen refs. Publish later versions; verify old refs remain fixed and setup causes no physical command. Record errors, evidence and actor attribution. |
| Separate collection confirmation and custody/lineage display | SAMP-01–SAMP-02 | Real scientist interaction and native acknowledgement/uncertainty path require acceptance | In isolated acceptance data, complete a planned collection step and verify no confirmed sample is created. Confirm separately; record stable ID, relabel without identity change, append custody, perform split/pool and inspect attributable parents/children/units. Interrupt acknowledgement and resolve the original operation; verify no duplicate fact. |
| Distinct-role database migration/deployment acceptance | APP-01–APP-04, SAMP-01–SAMP-02 | Isolated automated tests do not certify actual deployment role ownership/grants; application against user data needs owner approval | Confirm separate migration/runtime identities without revealing secrets. Retain isolated migration/replay/negative-role test evidence first. Obtain explicit deployment approval and backup/recovery disposition before any live apply; record migration tracking and runtime denial of immutable update/delete/truncate/safeguard changes. Do not run destructive verification on the live user database. |
| Baseline and prior waiver disposition | All six | Phase 1 waived/unpassed gates need explicit human disposition, not inherited passing status | Present fresh affected baseline output alongside prior Phase 1 records. Record any failure/waiver separately from passed tests; retain pending native/full smoke/migration/build acceptance until actually demonstrated or explicitly dispositioned. |

Workspace comparison/redesign approval remains Phase 02.1 work. These checks do not approve a production workspace redesign or bypass the current missing UI-SPEC gate.

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
