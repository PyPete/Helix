# Phase 02: Scientific Setup Foundation - Pattern Map

**Mapped:** 2026-10-30
**Scope:** APP-01, APP-02, APP-03, APP-04, SAMP-01, SAMP-02
**Files classified:** 35 proposed additions/modifications; names of additions are planner proposals, not existing APIs.
**Analog coverage:** 30 / 35 have structural analogs; 5 require new scientific semantics.

## Scope and preflight

Goal: **Scientists can define the revisioned scientific setup and sample facts that a campaign will freeze as its evidence base.**

Use existing `02-CONTEXT.md`, `02-RESEARCH.md`, `02-VALIDATION.md`, and approved `02-UI-SPEC.md`; do not regenerate research or validation. Current UI gate passed (`frontend:true`, `hasUiSpec:true`, `block:false`); UI-SPEC sign-off is 7/7 despite draft frontmatter. This supersedes VALIDATION's historical blocked-UI narration, not its pending verification status.

Blocking preflight passed. Context drift reports `block:false` and no stale artifacts. Codebase drift is advisory under the parent workflow: 65 structural elements, `action_required:true`, `directive:"warn"`, `spawn_mapper:false`, even though its payload also includes `block:true`. Ground plans in current tracked source. Suggested refresh remains `/gsd-map-codebase --paths AGENTS.md,Helix.Orchestration.Agent,Helix.Orchestration.Core,Helix.Orchestration.Devices,Helix.Orchestration.Infrastructure,deploy,docker-compose.yml,helix-desktop,tests`.

Phase 1 closure is administrative with waivers, not passing dependency acceptance. Preserve NU1603, NU1902, MSB3277 and unverified native/deployment gates as named caveats. State still points at Phase 02.1; parent workflow must reconcile ordering before execution without overwriting unrelated changes.

**UI fence:** implement thin setup/sample entry points in the existing shell. Canvas-first context, graph/cards equivalence and contextual inspection are provisional directions, not permission to replace the production workspace. Comparative spike evaluation and production redesign belong to Phase 02.1. Use approved UI-SPEC tokens, copy, interaction and accessibility requirements; do not copy the existing injected purple dock palette as the approved scientific palette.

## File Classification

`N` = proposed new file; `M` = existing modification; `C` = conditional modification. Pattern IDs refer to concrete assignments below. “Exact” means structural role/data-flow match, not pre-existing scientific implementation.

| New/Modified File | Role | Data Flow | Closest Analog / assignment | Match Quality |
|---|---|---|---|---|
| N `Helix.Orchestration.Core/ScientificSetup/ApparatusDefinition.cs` | model | transform | `Helix.Orchestration.Core/Devices/DeviceDescriptor.cs` (P1) | role-match |
| N `Helix.Orchestration.Core/ScientificSetup/ScientificRevision.cs` | model | CRUD | `Helix.Orchestration.Core/Execution/CommandIntent.cs` (P1) | role-match |
| N `Helix.Orchestration.Core/ScientificSetup/ProcedureDefinition.cs` | model | transform | `Helix.Orchestration.Core/Execution/CommandIntent.cs` (P1) | role-match |
| N `Helix.Orchestration.Core/ScientificSetup/ExperimentPlanDefinition.cs` | model | transform | `Helix.Orchestration.Core/Execution/CommandIntent.cs` (P1) | role-match |
| N `Helix.Orchestration.Core/ScientificSetup/IScientificSetupRepository.cs` | service (port) | CRUD | `Helix.Orchestration.Core/Execution/CommandIntent.cs` (P1/P2) | exact |
| N `Helix.Orchestration.Core/ScientificSetup/ScientificSetupValidator.cs` | utility | transform | none; research A2/A5 (P1) | none |
| N `Helix.Orchestration.Core/ScientificSetup/ScientificContentCanonicalizer.cs` | utility | transform | none; research A8 | none |
| N `Helix.Orchestration.Core/ScientificSetup/FrozenSetupManifest.cs` | model | transform | `Helix.Orchestration.Agent/Experiments/IExperimentManager.cs` (P4, shape only) | role-match |
| N `Helix.Orchestration.Core/ScientificSetup/IPolicyRevisionProvider.cs` | service (port) | request-response | none for authoritative immutable approved policy (P4) | none |
| N `Helix.Orchestration.Core/Samples/SampleFacts.cs` | model | event-driven | `Helix.Orchestration.Core/Execution/CommandIntent.cs` (P1/P2) | role-match |
| N `Helix.Orchestration.Core/Samples/ISampleRepository.cs` | service (port) | CRUD / event-driven | `Helix.Orchestration.Core/Execution/CommandIntent.cs` (P1/P2) | role-match |
| N `Helix.Orchestration.Core/Samples/SampleProjection.cs` | utility | transform | none for custody/correction semantics | none |
| N `Helix.Orchestration.Core/Samples/SampleLineageValidator.cs` | utility | transform | none for transactional many-parent lineage | none |
| N `Helix.Orchestration.Infrastructure/Persistence/PostgresScientificSetupRepository.cs` | service | CRUD | `Helix.Orchestration.Infrastructure/Persistence/PostgresCommandLedger.cs` (P2) | exact |
| N `Helix.Orchestration.Infrastructure/Persistence/PostgresSampleRepository.cs` | service | CRUD / event-driven | `Helix.Orchestration.Infrastructure/Persistence/PostgresCommandLedger.cs` (P2) | exact |
| N `Helix.Orchestration.Agent/ScientificSetup/ScientificSetupService.cs` | service | request-response / CRUD | `Helix.Orchestration.Agent/Execution/ExecutionControlService.cs` (P3) | exact |
| N `Helix.Orchestration.Agent/Samples/SampleService.cs` | service | request-response / event-driven | `Helix.Orchestration.Agent/Execution/ExecutionControlService.cs` (P3) | role-match |
| N `Helix.Orchestration.Agent/ScientificSetup/FrozenSetupResolver.cs` | service | request-response | `Helix.Orchestration.Agent/Experiments/IExperimentManager.cs` (P4) | role-match |
| N `Helix.Orchestration.Agent/Api/ScientificSetupEndpoints.cs` | controller / route | request-response | `Helix.Orchestration.Agent/Api/CommandEvidenceEndpoints.cs` (P3) | exact |
| N `Helix.Orchestration.Agent/Api/SampleEndpoints.cs` | controller / route | request-response | `Helix.Orchestration.Agent/Api/CommandEvidenceEndpoints.cs` (P3) | exact |
| M `Helix.Orchestration.Agent/AgentApplicationComposition.cs` | config | request-response | same file (P3) | exact |
| M `Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs` | config | CRUD | registration seam; P2/P3 lifetimes | exact |
| C `Helix.Orchestration.Agent/Experiments/IExperimentManager.cs` | model / service (port) | request-response | same file (P4) | exact |
| M `helix-desktop/src-tauri/src/commands/agent.rs` | controller / middleware | request-response | same file (P5) | exact |
| M `helix-desktop/src-tauri/src/lib.rs` | config | request-response | existing native registration seam (P5) | exact |
| M `helix-desktop/src-tauri/permissions/agent.toml` | config | request-response | existing explicit command permissions (P5) | exact |
| M `helix-desktop/src/lib/agentClient.ts` | service | request-response | same file (P5) | exact |
| N `helix-desktop/src/components/ScientificSetup/ScientificSetup.tsx` | component | event-driven / request-response | `helix-desktop/src/shell/HelixShell.tsx` (P6, composition only) | role-match |
| N `helix-desktop/src/components/Samples/Samples.tsx` | component | event-driven / request-response | `helix-desktop/src/shell/HelixShell.tsx` (P6, composition only) | role-match |
| M `helix-desktop/src/shell/WorkspaceLayout.tsx` | component | event-driven / file-I/O | same file (P6) | exact |
| N `postgres/migrations/<next-unused>_scientific_setup_and_samples.sql` | migration | batch / CRUD | `postgres/migrations/004_agent_ledger_constraints.sql` (P7) | exact |
| M `scripts/verification/phase01-migrate.ps1` | utility | batch / file-I/O | same file (P7) | exact |
| N `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/ScientificSetupTests.cs` | test | CRUD / batch | `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/CommandLedgerTests.cs` (P8) | exact |
| N `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/SamplesTests.cs` | test | CRUD / event-driven | `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/CommandLedgerTests.cs` (P8) | role-match |
| M `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs` | test (fixture) | batch / file-I/O | same file (P8) | exact |

### Supporting test/config files (mandatory plan coverage; not included in the 35 implementation rows)

| New/Modified File or proposed suite | Role | Data Flow | Assignment |
|---|---|---|---|
| N `tests/Helix.Orchestration.Core.Tests/ScientificSetup/ScientificSetupTests.cs` | test | transform | P1: topology, publications, plan pin, units, canonical vectors; new scientific assertions |
| N `tests/Helix.Orchestration.Core.Tests/Samples/SampleFactsTests.cs` | test | transform / event-driven | P1/P8: sequence projection, corrections, lineage |
| N `tests/Helix.Orchestration.Agent.Tests/Integration/ScientificSetupTests.cs` | test | request-response | P3/P4: authenticated actor, zero dispatch, frozen consumer |
| N `tests/Helix.Orchestration.Agent.Tests/Integration/SamplesTests.cs` | test | request-response | P3/P8: explicit confirmation, actor-scoped receipt, zero dispatch |
| N `helix-desktop/src/components/ScientificSetup/ScientificSetup.test.tsx` | test | event-driven / request-response | P5/P6: conflict, exact refs, editable draft, graph/cards identity |
| N `helix-desktop/src/components/Samples/Samples.test.tsx` | test | event-driven / request-response | P5/P6: explicit confirmation, empty history, custody/error UI |
| M `helix-desktop/src/lib/agentClient.test.ts` | test | request-response | P5: hoisted native mock, lost ACK, error payloads |
| M native `agent.rs` test module | test | request-response | P5: window/session/bounds/status/error assertions |
| M `helix-desktop/src/shell/HelixShell.test.tsx` | test | event-driven | P6: existing-shell entry points, state/persistence |
| C `tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj` | config | file-I/O | P8: preserve existing migration glob copy; modify only if fixture inputs require it |
| C `helix-desktop/src-tauri/capabilities/default.json` | config | request-response | P5: inspect existing main-window permission linkage; no broad permission expansion |
| N setup/sample co-located CSS, if needed | component / config | transform | approved UI-SPEC tokens; no inferred palette |

These paths expand RESEARCH's proposed directories/test suites into actionable candidate names. Planner may split/rename while retaining every responsibility and requirement. No SiteServer, protobuf, hardware driver, dashboard, legacy workflow-file migration, analysis CSV, or campaign engine change is implied.

## Pattern Assignments

### P1 — Core records, ports and distinct scientific identity

**Apply to:** apparatus, revision, procedure, plan, sample facts and repository-port files.

**Analogs:** `Helix.Orchestration.Core/Devices/DeviceDescriptor.cs:11–16`; `Helix.Orchestration.Core/Execution/CommandIntent.cs:82–130` (the actual file containing `ICommandLedger` and execution-store ports; there is no existing `ICommandLedger.cs`).

**Core typed binding excerpt** (`DeviceDescriptor.cs:11–16`):

```csharp
public sealed record DeviceDescriptor(
    string DeviceId,
    string DeviceAlias,
    string DeviceType,
    IReadOnlyList<string> CapabilityNames,
    IReadOnlyDictionary<string, object?> Metadata);
```

Copy the typed record/read-only collection style and feature namespaces, not `Metadata` as a substitute for scientific schema. Bind stable discovered device identity separately from editable labels. Current simulator discovery exposes one `simulator` identity; UI duplicates do not create additional discovered instruments.

Copy cancellable typed durable-store port conventions from `CommandIntent.cs:82–130`, but define separate scientific ports/tables. Immutable facts and revisions are not physical command intents. Model aggregate ID, draft concurrency token, named publication ID, schema version and canonical content hash separately. Apparatus connections are not WSL procedure edges. Plans independently publish and pin a published procedure; derived procedures carry source provenance.

**Validation/error adaptation:** expected input failures return typed aggregate diagnostics; stale tokens are explicit conflicts. Concrete topology/role/unit catalogs, canonicalization, correction and lineage algorithms have no existing domain analog. Use RESEARCH A2/A4/A5/A8 and require approved fixtures/vectors before schema stabilization. Do not claim universal UCUM conversion or parent depletion/conservation.

### P2 — Transactional scientific repositories

**Apply to:** `PostgresScientificSetupRepository.cs`, `PostgresSampleRepository.cs`, their Core ports, and infrastructure registration.

**Analog:** `Helix.Orchestration.Infrastructure/Persistence/PostgresCommandLedger.cs`.

- Imports and class/dependency conventions: lines 1–14.
- One connection/transaction and identity lock before stream ordering: lines 25–41.
- Immutable request equality and mismatched identity rejection: lines 58–66.
- Commit before acknowledgement: lines 104–105.
- Bounded, ordered pagination: lines 213–250.

**Concrete identity lock excerpt** (within lines 25–41):

```sql
SELECT pg_advisory_xact_lock(hashtextextended(@CommandId::text, 0));
```

**Concrete mismatch diagnostic** (within lines 58–66):

```text
Command identity was reused with different immutable intent content.
```

Adapt identity locking to actor + scientific operation kind + original operation key and compare the canonical validated request. Do not reuse physical command tables. Atomically commit receipt and all domain effects; commit must precede returned success. A receipt lookup must enforce the same authenticated actor/scope authorization as its mutation.

Publication transaction: lock aggregate, check expected draft token, validate draft/references, insert immutable publication, advance latest/draft base, store receipt, commit. Enforce one draft and unique revision sequencing with database keys, not process-local locks.

Sample transaction: assign sample ID on explicit confirmation; append initial fact with verified actor, occurrence time and server sequence/time. Custody checks expected stream position. Transformations lock affected parents in deterministic order and commit activity, children, quantities, all parent/child edges and receipt together. Newly created children simplify cycle prevention; accepting pre-existing children needs cross-writer graph serialization. Projections reconstruct from server sequence, not backdated occurrence time.

**I/O/error pattern:** parameterized commands, propagated cancellation and transaction disposal on failure. Preserve typed conflict/identity-mismatch distinctions. Use separate SQL connections for races and injected write-boundary failures; successful in-memory locks are not evidence of database correctness.

### P3 — Authenticated Agent routes and services; composition

**Apply to:** scientific/sample endpoints and application services; Agent/infrastructure composition.

**Analogs:** `Helix.Orchestration.Agent/Api/CommandEvidenceEndpoints.cs:68–102`, `Helix.Orchestration.Agent/Execution/ExecutionControlService.cs`, `Helix.Orchestration.Agent/AgentApplicationComposition.cs:272–281,320–335`.

Copy endpoint actor extraction and fail-closed response handling from `CommandEvidenceEndpoints.cs:68–88`; copy typed revision conflict-to-response handling from lines 91–102. The actor comes from authentication middleware, never request JSON. Adapt `ExecutionControlService`'s expected-revision/typed-conflict/store-authority pattern, not its physical execution controls. Domain services must not accept dispatchers/drivers to publish setup or record samples.

**Composition excerpt** (`AgentApplicationComposition.cs:272–280`):

```csharp
    // Infrastructure repositories (scoped, via connection string)
    builder.Services.AddHelixInfrastructure(builder.Configuration);
    builder.Services.AddSingleton<DesktopSessionService>();
    builder.Services.AddHostedService<SessionBootstrapWorker>();
    builder.Services.AddSingleton<CommandAdmissionEvaluator>();
    builder.Services.AddSingleton<IAdmissionStateSource, SimulatorAdmissionStateSource>();
    builder.Services.AddSingleton<AdmissionContextProvider>();
    builder.Services.AddSingleton<ICommandDispatcher, AgentCommandDispatcher>();
    builder.Services.AddSingleton<ExecutionControlService>();
```

Preserve infrastructure's repository lifetimes; a singleton scientific service must not capture a scoped repository. Choose compatible service lifetimes explicitly rather than copying every singleton registration.

**Authentication ordering excerpt** (`AgentApplicationComposition.cs:325–335`):

```csharp
    app.UseCors();
    if (includeDesktopSessionAuthentication)
        app.UseMiddleware<DesktopSessionAuthenticationHandler>();
    app.UseWebSockets();
    app.MapGrpcService<AgentGrpcService>();
    app.MapHealthChecks("/health");
    app.MapWorkflowEndpoints();
    app.MapRunspaceEndpoints();
    app.MapDeviceEndpoints();
    app.MapCommandEvidenceEndpoints();
    app.MapExperimentEndpoints();
```

Map new route modules after authentication. The test-host switch allowing already verified actors is not a production bypass. CORS/localhost is not authentication. Authorize record reads, revision references, mutations and receipts, not only collection creation. Enforce JSON bytes/depth/edge counts and paging in Agent as well as native code. Return actionable typed diagnostics for bad units/references, conflicts, missing records and unauthorized scope.

### P4 — Frozen manifest and execution consumer seam

**Apply to:** `FrozenSetupManifest.cs`, `FrozenSetupResolver.cs`, `IPolicyRevisionProvider.cs`; conditional canonical-definition contract modification.

**Analog:** `Helix.Orchestration.Agent/Experiments/IExperimentManager.cs` (structural contract only).

**Imports** (lines 1–4):

```csharp
using Helix.Orchestration.Core.Experiment;
using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Agent.Experiments;
```

**Canonical binding seam** (lines 45–50):

```csharp
public sealed record CanonicalExperimentDefinition(
    RunspaceDefinition Definition,
    string PolicyRevision,
    string DefinitionHash,
    string ActorSid,
    DateTimeOffset AcceptedAtUtc);
```

This current Agent-owned record is not a scientific freeze manifest and `PolicyRevision` alone is not proof of immutable approval. Keep new domain contracts inward in Core; do not make Core reference Agent types. Persist exact apparatus/procedure/plan/approved-policy IDs, hashes/schema versions and attribution. Latest resolves once at assembly; subsequent execution/restart resolves only the stored manifest.

Planner must settle transitive consistency: if latest plan pins an older published procedure, preserve that pin and require explicit plan adoption/publication before changing it. An authoritative immutable approved-policy provider is a new integration gate; never manufacture a policy approval string. Test a consumer after later publications AND repository reconstruction; returning IDs from a freeze endpoint is insufficient APP-04 evidence. Retain Agent compiler/admission and physical safety validation when future execution consumes this manifest. Do not implement the later campaign engine here.

### P5 — Native scientific operations and typed desktop client

**Apply to:** Rust command additions, registration/permissions, `agentClient.ts` and co-located tests.

**Analog:** `helix-desktop/src-tauri/src/commands/agent.rs`; `helix-desktop/src/lib/agentClient.ts` and `agentClient.test.ts`.

Rust anchors: imports 1–7; fixed host/window and bounds 9–19; guards 69–84; mutations 204–209; result lookup 233–239; pagination 241–255; redirect denial 343–359; response bounds 378–388; boundary tests 397–425.

**Existing bounds excerpt** (`agent.rs:9–15`, named-pipe line omitted):

```rust
const MAIN_WINDOW: &str = "main";
const AGENT_HOST: &str = "127.0.0.1";
const AGENT_PORT: u16 = 5080;
const MAX_REQUEST_BYTES: usize = 4 * 1024;
const MAX_RESPONSE_BYTES: usize = 64 * 1024;
const MAX_SESSION_RESPONSE_BYTES: u64 = 2 * 1024;
```

Copy guard-before-I/O, native-only credentials, fixed local destination, redirect denial and bounded response reads. Add dedicated resource-specific commands, not arbitrary URL/verb IPC. Determine operation-specific limits using representative apparatus/history/lineage fixtures and matching Agent limits; do not blindly enlarge globals. Inspect explicit registration and `permissions/agent.toml` linkage to main-window capabilities; do not hand-edit generated ACL/schema artifacts.

Client anchors: imports 1–2; JSON parsing 119–125; invoke wrapper 156–158; uncertainty result 244–262; stable mutation request 264–272; bounded pagination 296–318. Tests: hoisted native invoke mock at `agentClient.test.ts:1–9`; lost ACK at 94–116.

Copy stable operation identity and uncertain-outcome lookup using the original identity, never a fresh mutation ID. The renderer receives typed results, not tokens. Error state must distinguish committed, rejected/conflict, and genuinely uncertain transport outcomes.

**Do not blind-copy current acknowledgement weakness:** the native transport currently returns response body without checking HTTP status; `parseNativeJson<T>` only casts parsed JSON, and `mutation` treats returned JSON as Completed. Scientific operations must preserve/classify HTTP errors and validate result envelopes before declaring committed success. Add tests for 400/401/403/409/500, malformed success/error JSON, lost ACK after commit and original-key lookup. Client-generated actor or canonical hash cannot become authoritative.

### P6 — Minimal existing-shell UI integration

**Apply to:** setup/sample components, CSS/tests and `WorkspaceLayout.tsx`; only narrow shell entry-point changes if needed.

**Analogs:** `helix-desktop/src/shell/HelixShell.tsx:1–16,134–164`, `helix-desktop/src/shell/WorkspaceLayout.tsx:1–20,174–185,258–268,300–312`.

Copy concrete relative imports and existing session/error composition from HelixShell. Copy rc-dock tab factory at WorkspaceLayout lines 174–185; after relevant prop/session changes use the `updateTab` pattern at 258–268, since persisted dock content does not automatically refresh. Preserve stable save/load tab IDs at 300–312; persistence stores layout/presentation, not scientific evidence authority.

New components must explicitly show draft versus published state, publication name/action, exact pinned revisions, reload/compare on conflict and actionable remote errors. Sample collection requires a separate deliberate scientist confirmation; do not create samples when a planned step/runspace reports completion. Display immutable sample ID separately from editable label, empty history/lineage states, attributable ordered custody and transformation provenance.

Share view primitives only. Apparatus topology and procedure flow retain separate canonical types/validators. Graph/cards toggling must preserve stable IDs, selection, draft state and meaning. Existing shell composition is only a structural analogy: do not infer a new canvas/workspace architecture from it. Spike models are design evidence awaiting review, not production-copy analogs. Follow approved `02-UI-SPEC.md` for final tokens/copy/accessibility and keep redesign in Phase 02.1.

### P7 — Additive schema and migration runner

**Apply to:** next-unused scientific migration, least-privileged grants and migration runner changes.

**Analogs:** `postgres/migrations/004_agent_ledger_constraints.sql:4–42`; `scripts/verification/phase01-migrate.ps1:15–40`.

**Immutable trigger clauses** (SQL lines 13–21):

```sql
BEFORE UPDATE OR DELETE ON command_intents
BEFORE UPDATE OR DELETE ON command_evidence
```

These are separate clauses extracted from the two existing trigger definitions, not one runnable SQL statement.

**Privilege excerpt** (line 42):

```sql
REVOKE UPDATE, DELETE, TRUNCATE ON command_intents, command_evidence FROM PUBLIC;
```

Copy exception-trigger structure at 4–21 and unique/reference constraint style at 23–40 onto new publication/event/manifest tables; retain separate mutable drafts/projections. The excerpt alone does not protect against table owner/runtime grants. Runtime must not own schema/tables, truncate immutable facts, disable triggers or obtain migration privileges. Test real distinct roles and forbidden statements.

Runner anchors: distinct-role gate 15–19, enforced psql exit 24–27, SHA256 tracking/enumeration 29–40.

**Tracking table shape** (within lines 29–40):

```sql
helix_schema_migrations (migration_id text PRIMARY KEY, checksum text NOT NULL, applied_at timestamptz NOT NULL DEFAULT now())
```

**Enumeration excerpt** (within lines 29–40):

```powershell
Get-ChildItem 'postgres/migrations' -File -Filter '*.sql' | Sort-Object Name
```

Allocate a next-unused additive filename at execution. Existing names include duplicate 004/005 prefixes; do not edit applied migrations or assume a number without checking. Current runner defaults target live `helix-postgres`; migration application and checksum receipt occur in separate invocations/transactions. Do not copy these as isolated or crash-safe behavior. Add explicit disposable-target handling and prove fresh install, upgrade, repeat apply, changed-checksum rejection and crash-between-steps safety. Either atomically apply SQL with its tracking receipt or establish/test replay-safe behavior. No live migration is authorized by this pattern map.

### P8 — Isolated SQL verification, concurrency and fixture/runner failures

**Apply to:** scientific SQL test files, shared fixture and migration test harness; supporting unit/Agent/native/UI tests must cover their own boundaries.

**Analogs:** `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs`; `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/CommandLedgerTests.cs`.

Fixture anchors: disposable Testcontainers setup 31–38; deterministic migration load/apply 67–81; fail-closed initialization 83–88. `CommandLedgerTests.cs`: concurrent barrier and independent calls 18–46; changed-intent rejection 48–66; unavailable fixture throws 68–69.

**Fixture failure diagnostic** (within lines 83–88):

```text
PostgreSQL fixture initialization failed; required SQL tests cannot run.
```

**Existing migration-copy item** (`tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj:29–31`):

```xml
<None Include="..\..\postgres\migrations\*.sql" Link="migrations\%(Filename)%(Extension)" CopyToOutputDirectory="Always" />
```

Copy xUnit asynchronous fixture/container lifecycle, ordered migration discovery, task barrier and explicit required-fixture failure. Do NOT copy fixture's `PGHOST` live-database redirect/defaults at 17–28 or legacy successful early returns at 123–124. Scientific SQL verification must require a disposable database, visibly fail when unavailable, and assert nonzero discovered tests.

Required SQL assertions: concurrent publication has one winner; actor/operation/content-bound receipts are stable; failed transactions leave no partial publication/head/sample/transformation; custody expected-position races conflict; lineage cannot jointly form cycles; reconstruction preserves exact freeze/head/custody; actual runtime role cannot mutate immutable facts; isolated migration runner fresh/upgrade/repeat/crash tests do not connect to live user data. Native/Agent/UI tests supplement these, not substitute for transactional SQL evidence.

## Shared Patterns

### Authority and authentication

Source: P3 middleware ordering and endpoint verified-actor extraction. Apply to all routes/services/readback. Agent alone resolves actor, canonical content/hash, discovered binding and approved-policy reference. No save/publish/confirmation/custody gesture may dispatch hardware; enforce with a zero-dispatch spy. Do not weaken existing capability/safety/lease/resource/budget/termination admission.

### Atomicity, conflicts and uncertainty

Source: P2 ledger identity lock/equality/commit; P3 typed revision conflict; P5 original-key lookup. Apply across every scientific mutation. Concurrency tokens and receipt identities are distinct from display labels and schema versions. Persist receipt with domain facts; reject mismatched reuse. Distinguish deterministic rejection from lost acknowledgement; never silently rebase/replay with new identities.

### Validation and presentation

Source: typed records P1, bounded native operations P5, exact-ref UI P6; scientific validation remains new. Approve concrete vocabularies, exact case-sensitive units, finite positive quantities where applicable, port compatibility, correction semantics, plan/procedure consistency and canonical hash vectors. Agent validates untrusted proposals; renderer shows aggregate actionable diagnostics as text, not raw HTML. Layout/selection is presentation and cannot change scientific identity or canonical meaning.

### Persistence, cancellation and logging

Source: transactional ledger P2, migration/fixture P7/P8. Use parameterized SQL, durable ports, consistent lock ordering, cancellable I/O and fail-visible operational exceptions. Preserve immutable references and reconstructible projections. Follow AGENTS constructor guards, nullable annotations, `_camelCase` fields, file-scoped feature namespaces, `Async` suffixes and structured injected logging. Do not log native credentials/session tokens. No new package is proposed; any introduction requires the research legitimacy/install gate.

### Security verification

Use RESEARCH's version-qualified ASVS 5.0.0 L1 assessment plus explicit high-severity authority/evidence gates, not a certification claim. Authorization applies to guessed IDs, referenced revisions and receipts as well as mutations. Test wrong/expired/terminated session, wrong window, forbidden reference scope, oversized/deep payloads, unauthorized SQL role statements and rejected HTTP acknowledgements.

## No Analog Found

| File | Role | Data Flow | Reason / planner source |
|---|---|---|---|
| `ScientificSetupValidator.cs` | utility | transform | No approved scientific topology/unit/calibration validator; RESEARCH A2/A5; do not apply procedure-DAG rules to plumbing. |
| `ScientificContentCanonicalizer.cs` | utility | transform | No established scientific canonical serialization/schema/hash vectors; RESEARCH A8. |
| `IPolicyRevisionProvider.cs` | service | request-response | Existing revision string does not prove immutable policy approval; RESEARCH A3 integration gate. |
| `SampleProjection.cs` | utility | transform | Custody ordering, backdating and supersession semantics are new; RESEARCH A4. |
| `SampleLineageValidator.cs` | utility | transform | No existing scientific many-parent/many-child cycle-safe transaction semantics; RESEARCH A4/A5. |

Record/port/SQL scaffolding analogs do not resolve these missing scientific contracts. Do not report new scientific algorithms as copied existing behavior.

## Deterministic spec-less edge report

**Report location:** this section of `.planning/phases/02-scientific-setup-foundation/02-PATTERNS.md` (no separate report/input file).
**Engine:** exported `analyzeCoverage` from `C:/Users/u643664/.config/opencode/gsd-core/bin/lib/edge-probe.cjs`, invoked through inline Node stdin over the six exact requirement texts from RESEARCH/REQUIREMENTS.
**SPEC state:** no `02-SPEC.md`; section helpers succeeded with edges/prohibitions `supplied:false`.
**Toggle:** `workflow.specless_probe_fallback` absent; default ON. Successful engine invocation exited 0 and validated `{items[],coverage{}}`. Initial Node `-e` quoting failure produced no coverage and was replaced by the successful stdin invocation. No temporary files were needed.

### Raw unresolved rows (faithful field values)

| Requirement | Edge | Status | Verification | Resolution | Reason |
|---|---|---|---|---|---|
| APP-01 | concurrency | unresolved | null | null | null |
| APP-02 | unclassified | unresolved | null | null | null |
| APP-03 | unclassified | unresolved | null | null | null |
| APP-04 | unclassified | unresolved | null | null | null |
| SAMP-01 | adjacency | unresolved | null | null | null |
| SAMP-01 | empty | unresolved | null | null | null |
| SAMP-01 | ordering | unresolved | null | null | null |
| SAMP-02 | unclassified | unresolved | null | null | null |

```json
{
  "applicable": 8,
  "resolved": 0,
  "unresolved": 8,
  "unclassified": 4,
  "byVerification": { "explicit": 0, "backstop": 0 }
}
```

This is an inventory of deterministic probe results, not completed acceptance or inferred coverage. Preserve the no-silent-drop equality **8 applicable = 0 resolved + 8 unresolved**. Do not manufacture raw classifications/resolutions, dismiss rows automatically, or mistake suggested tests below for engine-resolved evidence.

### Planner acceptance suggestions for the four classified edges

| Edge | Proposed explicit acceptance / test |
|---|---|
| APP-01 / concurrency | Race separate SQL connections saving/publishing the same expected draft token: one succeeds, the other returns typed conflict; one active draft, no duplicate publication/head or silent overwrite. UI offers reload/compare. |
| SAMP-01 / adjacency | Planned collection-step completion immediately before/after scientist confirmation does not create a sample. Only explicit authenticated confirmation does; repeating the same original operation identity returns the same sample/fact. Test adjacent execution/step references for correct correlation and no cross-step leakage. |
| SAMP-01 / empty | No confirmation means no confirmed sample, including completed planned activity and initially empty sample/history UI. Reject missing required confirmation provenance according to the finalized contract without inserting partial rows; optional planned reference may remain absent if explicitly allowed. |
| SAMP-01 / ordering | Step completion and explicit confirmation may arrive in either order; only the explicit scientist action creates the fact, correlated to its intended reference. Preserve occurrence time separately from server recording sequence; retries/lost ACK cannot duplicate/reorder facts. |

Planner must assign explicit verification or a concrete named backstop to each classified edge, with test path/command/acceptance anchor. Confirm exact probe meaning when binding these suggestions; these are proposed acceptance, not rewritten raw results.

### Four unresolved unclassified assumptions

| Requirement row | Needed planner decision/coverage, without inventing an engine classification |
|---|---|
| APP-02 / unclassified | Approve capability/unit/calibration/inspection vocabulary, attribution/date/validity rules and distinction between recorded assertion and live safety authority. |
| APP-03 / unclassified | Specify independent identities/publication, derived-procedure reuse, plan pin/adoption and graph/cards semantics-preserving projection. |
| APP-04 / unclassified | Identify authoritative approved-policy provider; settle transitive plan/procedure consistency and durable reconstructed execution-consumer verification. |
| SAMP-02 / unclassified | Approve append-only correction/backdating rules, identity versus label, custody sequence, transformation quantities and serialized cycle prevention. |

Keep these rows unresolved until the parent fallback workflow supplies the required decision/classification; domain test proposals do not resolve unclassified probe rows. Prohibition recall is a **separate** planner step because no SPEC prohibitions were supplied. Recall at minimum: no setup dispatch, no client actor/hash/policy authority, no published-fact mutation, no latest re-resolution, no inferred collection, no timestamp-only custody ordering, no silent new-ID retries, no live-db test redirection/successful skips, no workspace redesign or automatic legacy JSON migration.

## Planner handoff

- Init: phase `02`, `Scientific Setup Foundation`, `Pending`; granularity `standard`; all selected models `inherit`; research/checker/Nyquist enabled; `commit_docs:true`; `text_mode:false`; `auto_advance:false`; `auto_chain_active:false`; mode `yolo`.
- Phase/planning/roadmap/context/research found; reviews/plans/SPEC absent; `plan_count:0`, initial `patterns_path:null`; threat duplicates zero; section manifest contains no optional steps. MVP query `true` from roadmap.
- Gates requested in parent workflow exited 0. Compact false; spec-less config lookup exited 1 only because key absent (default ON); stall enabled, interval 5 minutes, threshold 10.
- Active hooks: AI integration, research, UI, mapper; planner API coverage, assumption-delta, schema detection and security ASVS 1/high contributions; advisory drift gates and blocking UI gate; no intel step.
- Resume planner with existing inputs and this map; create plans only in the parent orchestration, then run enabled checker/security/API/assumption/schema contributions and resolve probe/prohibition handoff without dropping unresolved rows. Do not assume optional steps or automatic advance.
- Before execution: reconcile Phase 02/02.1 state ordering, finalize scientific vocabularies/hash vectors/corrections/policy provider/consistency, establish nonzero affected test discovery and baseline disposition, and schedule isolated SQL + authenticated Windows native acceptance.

## Metadata

**Analog search scope:** tracked Core, Agent, Infrastructure, desktop Rust/React shell/client, PostgreSQL migrations, verification scripts and existing Infrastructure tests. Stopped broad search after strong domain/persistence/API/native/UI pattern clusters; additional source is cross-cutting integration/test evidence, not alternative hunting.
**Tracked-source gate:** all source analog paths above were verified against non-empty repository `git ls-files` output; no install/runtime mirror, ignored build artifact or nested-submodule source is named as an analog.
**Evidence limits:** source reads and deterministic probe only. No full builds/tests, source edits, live migrations, branch changes, research/validation regeneration, plans or commits performed. Only this PATTERNS artifact is written. Existing dirty ROADMAP/STATE/state.json and untracked Phase 02.1 work are preserved.
**Pattern extraction date:** 2026-10-30.
