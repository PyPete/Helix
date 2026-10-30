# Phase 02: Scientific Setup Foundation - Research

**Researched:** 2026-10-30
**Domain:** Local scientific setup revisioning, frozen execution references, sample custody and lineage
**Confidence:** LOW overall (official documents cited; design choices require confirmation)

**Provenance notation:** `[ASSUMED: A#]` marks an `[ASSUMED]` finding and links it to the corresponding confirmation group in the Assumptions Log. Repository `[VERIFIED: path:lines]` tags establish source observations, not passing implementation tests or provider confidence.

<user_constraints>
## User Constraints (from CONTEXT.md)

The following is copied verbatim from the phase context. [VERIFIED: .planning/phases/02-scientific-setup-foundation/02-CONTEXT.md:16-34,93-97]

<!-- DATA_k7Qm2R9a_START -->
### Locked Decisions

### Revision lifecycle
- **D-01:** Each apparatus, procedure, and plan has one active editable draft based on its latest published revision. Edits remain draft changes until the scientist explicitly publishes a named, immutable revision.
- **D-02:** When a campaign is assembled, default its apparatus, procedure, and plan references to the latest published revisions and show those exact versions. Pin them at assembly; later publications must not silently change an assembled campaign. The approved campaign policy is also frozen for execution under the existing project requirements.

### Procedure and experiment plan
- **D-03:** A plan instantiates a specific published procedure revision while retaining its own independent revision. It records experiment-specific parameter values and units so a later campaign can identify the exact intended setup.
- **D-04:** Structural changes to a procedure used by a plan create a new derived procedure revision, not an in-place edit to the pinned revision. The derived revision is reusable by other plans and retains its relationship to the source revision.

### Sample facts and lineage
- **D-05:** A scientist separately confirms actual sample collection; completion of a planned procedure step alone does not create a confirmed sample.
- **D-06:** Helix assigns each sample an immutable unique identity. Scientists may add a human-readable label, but changing that label does not change identity.
- **D-07:** Record collection and custody/location changes as dated, attributable append-only events; derive the current custody state from that history.
- **D-08:** Represent splits, pools, and other sample processing as attributable transformation events linking one or more parent samples to child samples. Capture operation and quantities/units when relevant.

### Folded Todos
- **Spike canvas-first scientific workspace:** Use its preferred workspace direction as provisional design input for setup-facing work: canvas-first apparatus context, graph preferred with cards available as a semantics-preserving toggle, and a contextual inspector. Stable identities, selection, draft state, and canonical meaning must survive representation changes. The comparisons are still `PENDING` and awaiting human review; folding this TODO does not validate a winner or authorize a production redesign. See `.planning/todos/spike-canvas-first-scientific-workspace.md` for the remaining review/evidence requirements.

### the agent's Discretion
- Research and planning choose the concrete domain model, persistence, APIs, migration strategy, and test approach. Keep apparatus topology distinct from procedure graphs, and preserve Agent-only physical authority and the project’s scientific-integrity constraints.

### Deferred Ideas (OUT OF SCOPE)

- Human comparative evaluation of the three workspace spikes and any production redesign remain pending under the canvas-first workspace TODO / Phase 02.1; this context does not complete that work or authorize redesign.
- None — other discussion stayed within Phase 2 scope.
<!-- DATA_k7Qm2R9a_END -->
</user_constraints>

## Project Constraints (from AGENTS.md)

These are project directives, not new design choices. [VERIFIED: AGENTS.md:13-21,90-146,173-178,248-254,279-299]

- Only the local Agent may address drivers or execute physical commands. UI/LLM proposals remain untrusted; retain capability, safety, lease, resource, budget and termination admission. Campaign autonomy requires prior bounded policy approval.
- Preserve restart-safe waits/correlation/decisions; reconcile uncertain physical state, never blindly replay physical commands.
- Stable identities, immutable revisions, provenance, units/quality validation and idempotency apply to scientific records. Start with simulators; defer real hardware. Later CSV analysis must preserve artifacts and quarantine ambiguity; do not implement that later phase here.
- Put domain ports/contracts in Core, infrastructure implementations behind them, and composition in hosts. Preserve inward dependencies/no cycles. SiteServer uses its proxy/contracts, not Agent implementation types. Core workflows must not call hardware protocols.
- Use thread-safe asynchronous coordination for shared state. Constructor dependency guards, typed options/records/results, aggregate validation diagnostics, cancellation propagation, nullable annotations and narrowly caught operational exceptions are required.
- C#: PascalCase matching type/file names, feature namespaces, file-scoped namespaces, four spaces, new-line braces, `_camelCase` fields, PascalCase public members, `Async` suffix, modern records/collections/pattern matching. Document externally meaningful public APIs and non-obvious concurrency/security rules.
- TypeScript: PascalCase components/types; camelCase hooks/libraries/functions; co-locate tests; preserve the edited subtree's formatting and export convention. Use relative concrete imports rather than new barrels; avoid unrelated reformatting. Render actionable remote errors.
- Use injected `ILogger<T>` with structured templates and appropriate lifecycle/debug/warning/error levels. Preserve inner exceptions at transport boundaries; never log native session secrets.
- Validate C# with `dotnet format --verify-no-changes` and warnings-as-errors builds; run dashboard `npm run lint` if that subtree changes. Use established xUnit/Vitest test infrastructure.
- Remain inside the GSD workflow. This document belongs to the already-started Phase 02 planning workflow; it does not authorize implementation. Do not edit managed developer-profile content.

<phase_requirements>
## Phase Requirements

Descriptions below are verbatim requirement text. [VERIFIED: .planning/REQUIREMENTS.md:19-22,33-34]

<!-- DATA_v4L8c1Np_START -->
| ID | Description | Research Support |
|----|-------------|------------------|
| APP-01 | A scientist can version an apparatus with typed components, ports, connections, roles, and device-instance bindings. | Typed topology, publication transaction, simulator binding validation and minimal editor. |
| APP-02 | A scientist can associate capability constraints and calibration/inspection metadata with an apparatus device. | Revision-owned device assertions, dimensional validation and provenance. |
| APP-03 | A scientist can version procedures and plans independently of apparatus topology. | Separate revision aggregates; plan pins a published procedure; derived procedures retain source reference. |
| APP-04 | The system freezes the apparatus, procedure, and policy revisions used by a campaign execution. | Durable assembly manifest, immutable revision resolution and execution-side consumption contract. |
| SAMP-01 | A scientist can confirm actual sample collection separately from a planned activity. | Explicit authenticated confirmation endpoint; no sample creation from workflow completion. |
| SAMP-02 | The system records stable sample identities, custody, transformations, and parent/child lineage. | Append-only event streams and atomic many-parent/many-child transformations. |
<!-- DATA_v4L8c1Np_END -->
</phase_requirements>

## Summary

Use the local Agent as the authenticated scientific-record API and PostgreSQL as its durable store; keep the desktop a proposal/editor client using the native session broker. This is a recommended design, not an already-implemented scientific-record service. [ASSUMED: A1] The current host already composes infrastructure and desktop authentication: `builder.Services.AddHelixInfrastructure(builder.Configuration);` and `app.UseMiddleware<DesktopSessionAuthenticationHandler>();` before endpoint mapping. [VERIFIED: Helix.Orchestration.Agent/AgentApplicationComposition.cs:272-281,320-335]

Build three independent draft/publication aggregates, a durable frozen-reference manifest, and sample/event/transformation records. Do not combine apparatus connectivity with procedure control flow, treat a mutable filename as a revision, or use sample labels as keys. [ASSUMED: A2] The existing workflow authoring model explicitly calls `SchemaVersion` a schema compatibility version: `public string SchemaVersion { get; init; } = "1.0";`; scientific publication needs a separate identity. [VERIFIED: Helix.Orchestration.Core/WSL/WslDocument.cs:15-16]

Phase 1 is a dependency with waived/unpassed acceptance, not proof of a clean baseline: its summary says "User-approved waivers close the remaining Phase 01 acceptance gates administratively; they do not constitute passed verification." It records `NU1603`, `NU1902`, and `MSB3277` as unresolved build diagnostics. Re-baseline affected builds and native/database acceptance before claiming Phase 02 verification; do not silently expand this phase into a dependency-remediation project. [VERIFIED: .planning/phases/01-agent-safety-boundary/01-08-SUMMARY.md:20-25,52-68]

**Primary recommendation:** Deliver thin end-to-end setup and sample slices on the existing Agent/native/PostgreSQL boundary, with publication/freeze/custody invariants enforced transactionally and verified against isolated PostgreSQL. [ASSUMED: A1, A2, A3]

## Architectural Responsibility Map

Recommended ownership; each row is a design proposal requiring planner confirmation. [ASSUMED: A1]

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Draft editing, topology visualization, exact-version display | Browser / Client | Native desktop broker | UI presents proposals; native broker protects credentials and transport. |
| Device discovery and binding admission | API / Backend (local Agent) | Core domain contracts | Agent owns live registry; discovery does not grant command authority. |
| Publication and scientific validation | API / Backend (Agent) | Database / Storage | Agent validates; SQL enforces uniqueness, references, atomicity and immutable facts. |
| Assembly/frozen execution references | API / Backend (Agent) | Database / Storage | Persist exact revision IDs before downstream execution; no runtime latest lookup. |
| Collection confirmation, custody and transformations | API / Backend (Agent) | Database / Storage | Server determines actor and recording order; client cannot assert verified identity. |
| Provenance/current-state projections | Database / Storage | API / Backend | Append-only facts are authoritative; caches/projections are reconstructible. |
| Physical dispatch | API / Backend (Agent safety boundary) | Devices adapters | Setup API must never invoke drivers or dispatch commands. |

## Standard Stack

Reuse existing dependencies; **no install, package upgrade, new graph database, unit-parser package or event-sourcing framework is recommended.** [ASSUMED: A1, A2, A5] Registry currency and publish dates were not queried because this is reuse, not version selection; installed versions are observations, not assertions that packages are latest or legitimacy-approved.

### Core

Package names below are existing manifest entries, not newly discovered packages. Manifest quotes establish only the existing declarations; no entry earns `[VERIFIED: npm registry]` or a new-install approval.

<!-- DATA_b6T9w2Hs_START -->
| Existing dependency | Declared version (verbatim) | Purpose for this phase | Evidence |
|---------------------|----------------------------|------------------------|----------|
| .NET target | `<TargetFramework>net10.0</TargetFramework>` | Core/domain, authenticated Agent APIs | [VERIFIED: Helix.Orchestration.Infrastructure/Helix.Orchestration.Infrastructure.csproj:3-7] |
| Npgsql | `<PackageReference Include="Npgsql" Version="9.*" />` | PostgreSQL connections/transactions | [VERIFIED: Helix.Orchestration.Infrastructure/Helix.Orchestration.Infrastructure.csproj:12-13]; transaction API [CITED: https://www.npgsql.org/doc/basic-usage.html] |
| Dapper | `<PackageReference Include="Dapper" Version="2.*" />` | Preserve existing repository query pattern | [VERIFIED: Helix.Orchestration.Infrastructure/Helix.Orchestration.Infrastructure.csproj:12-13]; reuse recommendation [ASSUMED: A1] |
| React / React DOM | `"react": "^18.3.0"`, `"react-dom": "^18.3.0"` | Existing desktop editor/forms | [VERIFIED: helix-desktop/package.json:20-21]; reuse recommendation [ASSUMED: A1] |
| React Flow | `"@xyflow/react": "^12.11.0"` | Reuse canvas interaction without conflating scientific models | [VERIFIED: helix-desktop/package.json:17]; reuse recommendation [ASSUMED: A1, A2] |
| Tauri API | `"@tauri-apps/api": "^2.0.0"` | Typed native IPC | [VERIFIED: helix-desktop/package.json:14]; scoping [CITED: https://v2.tauri.app/security/capabilities/] |
<!-- DATA_b6T9w2Hs_END -->

### Supporting

<!-- DATA_m5Z3p8Dx_START -->
- Existing test declarations: `<PackageReference Include="xunit" Version="2.*" />`, `<PackageReference Include="Testcontainers.PostgreSql" Version="4.*" />`. [VERIFIED: tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj:12-18]
- Desktop declarations: `"vitest": "^4.1.7"`, `"@testing-library/react": "^16.3.2"`. [VERIFIED: helix-desktop/package.json:27,36]
<!-- DATA_m5Z3p8Dx_END -->

Installed dependency probe returned React/DOM 18.3.1, React Flow 12.11.0, Tauri API 2.11.0/CLI 2.11.2, Vitest 4.1.7, Testing Library 16.3.2, TypeScript 5.9.3 and Vite 6.4.2. [VERIFIED: session `npm ls --prefix helix-desktop --depth=0`, 2026-10-30]

### Alternatives Considered

Design comparisons, not recommendations to install alternatives. [ASSUMED: A1, A2, A5]

| Instead of | Strongest alternative | Decision |
|------------|-----------------------|----------|
| Agent-owned SQL records | SiteServer is convenient for existing central campaigns/collaboration | Do not require it for the local slice; add a proxy later without moving authority into the browser. |
| Relational lineage edges + transformation facts | Graph storage can simplify complex traversals | Use existing PostgreSQL for bounded sample ancestry and atomic integrity; revisit only with measured query pressure. |
| Domain-specific draft/publication repositories | Generic event-sourcing can reconstruct every edit | Keep editable drafts mutable and publications/facts immutable; avoid rebuilding the entire application around an event framework. |
| Bounded unit vocabulary | Full UCUM parsing/conversion supports broad instruments | Defer broad interoperability; publish the exact limited supported catalog and reject unknowns. |

**Installation:** None. If planning introduces any external package, run legitimacy, correct-registry version/date and postinstall checks before installing; an existing manifest is not a substitute for that gate.

## Package Legitimacy Audit

Not applicable to the proposed phase: no new external package installation. No legitimacy verdict was requested or implied. Existing declarations/installed versions above are not registry trust verdicts. If the planner changes this premise, add the required audit and human verification for suspicious or non-authoritatively discovered packages.

## Architecture Patterns

### System Architecture Diagram

Proposed data flow. [ASSUMED: A1, A2, A3]

```text
Scientist edits / explicitly publishes / confirms a collection
                      |
                      v
Desktop typed forms + topology/procedure projections
                      |
                      v
Native broker: local window -> session -> fixed destination -> bounded IPC
                      |
                      v
Agent: verified actor + typed request + operation identity
                      |
                      v
Validate references / units / topology / publication / sample sequence
              invalid |                         | valid
                      v                         v
              actionable diagnostics     PostgreSQL transaction
                                          locks -> facts -> commit
                                                   |
                        +--------------------------+------------------+
                        v                                             v
              Revision/history readback                    Frozen assembly manifest
                        |                                             |
                        v                                             v
               Desktop exact-ID view                    Future campaign execution
                                                        resolves only frozen refs
                                                                  |
                                                                  v
                                                      Existing Agent safety admission
                                                                  |
                                                                  v
                                                        Simulator capability adapter
```

There is deliberately no arrow from setup editing/publication to dispatch. Publishing a scientific record must not start a runspace. [ASSUMED: A1; constrained by D-01 and Agent authority]

### Component Responsibilities / Recommended Structure

All additions and paths in this table are **proposed**, not existing source-truth paths. [ASSUMED: A2, A3, A6]

| Component | Proposed location | Responsibility |
|-----------|-------------------|----------------|
| Scientific setup domain | `Helix.Orchestration.Core/ScientificSetup/` | Topology types, revision references, draft tokens, plan/procedure constraints, freeze manifest and validation. |
| Sample domain | `Helix.Orchestration.Core/Samples/` | Stable identities, event/transformation types and deterministic projections. |
| SQL adapters | `Helix.Orchestration.Infrastructure/Persistence/` | Dedicated setup/sample repositories; transactions and role-aware immutable protections. |
| Agent application service | `Helix.Orchestration.Agent/ScientificSetup/` | Verified actor, request validation, assembly/publication orchestration; never dispatch. |
| Agent endpoints | `Helix.Orchestration.Agent/Api/` | Bounded resource-specific setup/sample APIs and mutation-result lookup. |
| Native IPC / typed client | `helix-desktop/src-tauri/src/commands/`, `helix-desktop/src/lib/` | Dedicated scientific operations, credential confinement and uncertainty handling. |
| Minimal desktop entry points | `helix-desktop/src/components/ScientificSetup/`, `helix-desktop/src/components/Samples/` | Draft/publish/version inspection and confirmation/history UI; no workspace redesign. |

### Pattern 1: Separate aggregate identity, draft token and publication identity

Recommended model shape. All field/type names and catalog choices here are provisional, not existing schema. [ASSUMED: A2]

- Each apparatus/procedure/plan has a stable aggregate ID and exactly one active draft. Each published revision has its own immutable revision ID, parent/source reference, scientist-supplied name, server actor/time, schema version, and content hash. Use a distinct draft concurrency token, not a display label or schema version.
- Explicit publication locks the aggregate, checks the caller's expected token, validates the entire draft, inserts immutable content and advances latest/draft base in one transaction. Stale edits/publications produce a conflict with reload/compare UI; never silently overwrite/rebase another edit.
- Treat layout/selection as a projection of stable scientific IDs; changing graph/cards presentation must not create new identities or alter canonical scientific content. Decide whether coordinates are persisted as draft-only presentation metadata before implementation.
- Plans own parameter values/units and pin a published procedure revision. Procedure structural edits fork a derived procedure with source revision provenance; only an explicit plan draft edit adopts it.

PostgreSQL row locks serialize conflicting writers; acquiring locks consistently reduces deadlock risk. Its `CHECK` constraints are not a mechanism for checking other rows. Use unique keys/FKs for identities/references and transactional validation for cross-record invariants. [CITED: https://www.postgresql.org/docs/16/explicit-locking.html] [CITED: https://www.postgresql.org/docs/16/ddl-constraints.html]

### Pattern 2: Typed apparatus is not executable WSL

Recommended topology contains stable component IDs, explicit component/port types, roles, connection endpoints and device-instance bindings. Validate unique IDs, existing endpoints, port compatibility/cardinality, role requirements, and binding/capability constraints on the Agent. Do not impose procedure-DAG semantics on physical plumbing: permitted loops/directionality require an approved apparatus vocabulary. [ASSUMED: A2, A5]

The discovery contract is exactly the following; it describes logical discovered devices rather than granting arbitrary hardware-instance creation. [VERIFIED: Helix.Orchestration.Core/Devices/DeviceDescriptor.cs:11-16]

<!-- DATA_r9F2a7Jc_START -->
```csharp
public sealed record DeviceDescriptor(
    string DeviceId,
    string DeviceAlias,
    string DeviceType,
    IReadOnlyList<string> CapabilityNames,
    IReadOnlyDictionary<string, object?> Metadata);
```
<!-- DATA_r9F2a7Jc_END -->

Current simulator registration supplies `"simulator", "simulator", "simulator"` for the descriptor identity/alias/type. Do not treat UI duplicates of that adapter as independent discovered instruments. [VERIFIED: Helix.Orchestration.Agent/AgentApplicationComposition.cs:253-265] Store stable binding identity separately from editable labels; binding publication validates discovery but does not execute an inspection/calibration command. [ASSUMED: A2]

Associate revision-owned capability constraints and calibration/inspection assertions with the bound component/device: measured/allowed values, units, dates, source references, attribution and validity interpretation. These are recorded assertions, not proof the Agent performed calibration. The vocabulary, acceptable date rules, expiry and runtime enforcement require scientist confirmation; do not invent laboratory policy. [ASSUMED: A5]

### Pattern 3: Freeze references at assembly, consume them at execution

Implement a durable assembly manifest now, with exact apparatus, procedure, plan and approved policy revision references, source hashes/schema versions and actor/time. Default from latest published records **once**, atomically resolve/validate them, persist, and display those IDs. All future reads/resume/execution resolve that manifest, never latest. [ASSUMED: A3]

**Consistency edge case:** Latest plan may pin an older procedure. Respect that pin: show it and require explicit adoption/publication of a new plan to choose the newer procedure; never create an assembly with a plan and an incompatible procedure simply to satisfy a latest default. This needs a visible planner decision/checkpoint. [ASSUMED: A3]

Current canonical experiment definition contains only the following fields; it is not itself a durable scientific freeze manifest. [VERIFIED: Helix.Orchestration.Agent/Experiments/IExperimentManager.cs:45-50]

<!-- DATA_h3N6s8Vk_START -->
```csharp
public sealed record CanonicalExperimentDefinition(
    RunspaceDefinition Definition,
    string PolicyRevision,
    string DefinitionHash,
    string ActorSid,
    DateTimeOffset AcceptedAtUtc);
```
<!-- DATA_h3N6s8Vk_END -->

Integrate the manifest at the canonical execution-binding seam; preserve the existing Agent compiler/admission boundary. A fake policy revision string or freeform metadata is insufficient for APP-04. Since policy creation/approval and durable campaign execution are later-phase work, define and test the consumer port with an authoritative policy-reference provider; report a missing provider as an explicit integration gate rather than inventing an approval. Include a test proving execution reads the frozen manifest after later publication and repository reconstruction, not just that a freeze API returns IDs. [ASSUMED: A3]

### Pattern 4: Confirmed samples and append-only transformation provenance

Recommended sample/event rules; event names and schema are new design choices. [ASSUMED: A4]

1. Collection confirmation is an explicit authenticated scientist mutation. The Agent assigns the immutable sample ID and records confirmation, actor, scientific occurrence time, server recording time, location, and optional planned-step/execution reference atomically. Workflow completion may offer a confirmation proposal, but cannot create that fact.
2. Keep the human label outside identity. Record relabeling/custody corrections as attributable appended changes rather than rewriting history. Use the server-allocated sequence for current-state projection; preserve backdated occurrence time as evidence without silently reordering custody.
3. A custody mutation carries expected stream position. Lock the sample stream, check expected state, validate transition, append, and commit. Multiple children/parents use deterministic lock order; return conflicts rather than applying two contradictory location changes.
4. A transformation is one immutable activity with one or more parent links, one or more child links, operation, actor, time, quantities/units when relevant, and provenance. Create the activity, children and all edges atomically. Children created by transformation have transformation provenance, not a fabricated independent collection confirmation.
5. Reject missing parents, self-links, duplicate edges and cyclic derivation. Prefer newly created children for the initial slice; if linking pre-existing samples is allowed, serialize graph validation across all affected lineage writes. Concurrent transactions must not each validate a partial graph and jointly introduce a cycle.
6. Do not assume parents are depleted or quantities conserved: aliquots may leave remainder; processing can change mass/volume. Require positive finite quantity/dimensional compatibility where supplied, but let the scientist define relevant balance/yield rules. Corrections append reason and supersession reference; retention, deletion, regulated signatures and inventory semantics are not silently locked.

W3C PROV-DM describes entities, activities, agents, derivation and revision. Mapping samples/revisions to entities, transformations to activities and authenticated scientists to agents is a useful provenance vocabulary; the standard does not require this application to introduce RDF storage. [CITED: https://www.w3.org/TR/prov-dm/]

UCUM defines case-sensitive codes and distinct full/limited conformance; special units have nontrivial semantics. Do not lowercase units or claim universal conversion from simple arithmetic. [CITED: https://ucum.org/ucum] Use a bounded scientist-approved code/dimension catalog, exact units on plans/constraints/transformations, invariant numeric serialization, and explicit unknown-unit diagnostics. Do not build a general UCUM parser. [ASSUMED: A5]

### Pattern 5: Typed operations, actor-bound idempotency, bounded native IPC

Proposed API capabilities below are operation families, **not verified endpoint names**. [ASSUMED: A2, A3, A4, A7]

| Operation family | Request admission | Persistent result / UI behavior |
|------------------|-------------------|---------------------------------|
| Create/read/save setup draft | Typed payload, expected draft token, validated references | One active draft; stale save conflicts visibly. |
| Publish/list/read revision | Named revision, operation key, expected draft token | Immutable revision ID/hash; exact-version readback and history. |
| Derive procedure/adopt in plan | Published source reference, explicit structural draft edits | Reusable source-linked procedure; plan keeps its pin until explicit adoption. |
| Assemble/read frozen manifest | Published refs and authoritative policy revision | Persisted immutable manifest; consistency checks and exact-ID UI. |
| Confirm collection | Operation key, verified server actor, bounded occurrence/location/source data | Stable sample ID and initial fact, never inferred from procedure completion. |
| Relabel/change custody/record transformation | Expected stream state, bounded quantities/links, operation key | Attributable append-only event and reconstructible state/lineage. |
| Look up mutation result | Operation key plus authenticated actor/scope | Resolve lost acknowledgement without manufacturing a second fact. |

For each mutation, bind idempotency to actor, operation kind and canonical validated request. Same identity/same content returns the original committed result; same identity/different content is rejected. Commit the mutation result and all domain effects in one transaction. Store only necessary audit/session reference, never a bearer token. A disconnected desktop displays uncertain outcome and queries by the original operation key; it must not auto-replay with a new identity. [ASSUMED: A7]

The existing command ledger provides a useful transaction pattern: identity lock before stream ordering, request equality check, and commit before returning. Its SQL uses `SELECT pg_advisory_xact_lock(hashtextextended(@CommandId::text, 0));`; mismatched reuse throws `"Command identity was reused with different immutable intent content."` Reuse the pattern, not physical-command tables or physical admission semantics for scientific facts. [VERIFIED: Helix.Orchestration.Infrastructure/Persistence/PostgresCommandLedger.cs:25-66,79-105]

Current native broker constants are verbatim below (the unrelated named-pipe constant between them is omitted). They are transport bounds, not a promise that a full apparatus document fits. [VERIFIED: helix-desktop/src-tauri/src/commands/agent.rs:9-15]

<!-- DATA_c8W2n4Qa_START -->
```rust
const MAIN_WINDOW: &str = "main";
const AGENT_HOST: &str = "127.0.0.1";
const AGENT_PORT: u16 = 5080;
const MAX_REQUEST_BYTES: usize = 4 * 1024;
const MAX_RESPONSE_BYTES: usize = 64 * 1024;
const MAX_SESSION_RESPONSE_BYTES: u64 = 2 * 1024;
```
<!-- DATA_c8W2n4Qa_END -->

Add resource-specific native commands with operation-specific bounded payloads, bounded pagination and Agent-side corresponding limits. Choose limits after representative topology/lineage fixtures, not arbitrary unbounded increases. Retain main-window/session checks, fixed destination and redirect denial; do not add an arbitrary URL/verb proxy or renderer-visible token. [ASSUMED: A7]

Tauri custom commands registered with the invocation handler are available by default unless app-manifest restrictions scope them; capabilities are not a replacement for Rust boundary checks. Inspect generated permissions/registration when adding commands and test rejection from other windows. [CITED: https://v2.tauri.app/security/capabilities/]

### Pattern 6: Additive SQL schema, immutable facts, mutable projections

Proposed persistence design and naming are provisional. [ASSUMED: A2, A3, A4, A8]

- Use separate aggregate/draft tables and immutable publication rows for apparatus, procedure and plan. Unique aggregate identity enforces one draft; unique aggregate/revision sequence and immutable revision IDs support deterministic history. Published plans reference published procedure rows with FKs, never a mutable draft.
- Use a durable frozen-manifest table with FK revision references; immutable policy content/hash must be resolvable through the policy contract. Do not store only display versions or opaque unverified metadata.
- Use sample identities, append-only events, transformation activities, parent/child links, stream heads and mutation-result records. Current labels/custody and lineage views are projections, not editable evidence. Constraints enforce identity/reference uniqueness; domain services enforce graph/transition semantics transactionally.
- Store typed indexed identity/reference columns; use versioned JSONB for validated topology/procedure content where practical. Compute canonical content hashes on the Agent from deterministic serialization; never trust renderer hashes. Exact canonicalization/schema evolution needs a specified test vector before execution.
- Separate migration owner from least-privileged runtime role. Runtime can mutate drafts/projections and append facts, but cannot update/delete/truncate immutable publication/event/manifest tables, disable triggers or own the schema. Test actual role grants and attempted forbidden statements; do not claim that a trigger alone defeats an owner.

The existing command safeguards contain `BEFORE UPDATE OR DELETE ON command_intents`, `BEFORE UPDATE OR DELETE ON command_evidence`, and `REVOKE UPDATE, DELETE, TRUNCATE ON command_intents, command_evidence FROM PUBLIC;`. These are patterns to inspect, not evidence that the new runtime role has correct privileges. [VERIFIED: postgres/migrations/004_agent_ledger_constraints.sql:13-21,42]

The migration script creates `helix_schema_migrations (migration_id text PRIMARY KEY, checksum text NOT NULL, applied_at timestamptz NOT NULL DEFAULT now())`, checks SHA256 against applied files, and enumerates `Get-ChildItem 'postgres/migrations' -File -Filter '*.sql' | Sort-Object Name`. Its exact mode is `[ValidateSet('CheckAndApply')]`; parameters are `$MigrationUser = $env:HELIX_MIGRATION_USER` and `$RuntimeUser = $env:HELIX_RUNTIME_USER`. It rejects identical identities. [VERIFIED: scripts/verification/phase01-migrate.ps1:3-8,15-19,29-40]

**Migration planning:** Allocate the next unused additive filename at execution; never edit an already-applied migration. Extend the migration/grant gate for new facts and drafts. The current script executes migration SQL and later records the checksum through separate invocations; do not assume migration application and tracking are already atomic. Require an isolated fresh-install, upgrade, repeat-apply and crash-between-steps test, then improve the runner or make additive operations demonstrably repeat-safe. Do not invoke the live migration script during research or against the user's database without explicit approval. [ASSUMED: A8; runner evidence VERIFIED: scripts/verification/phase01-migrate.ps1:24-40]

Existing file save/load creates the convenience directory with `await mkdir("workflows", { baseDir: BaseDirectory.AppData, recursive: true });` and writes ``writeTextFile(`workflows/${safe}.json`, JSON.stringify(doc, null, 2),``. Preserve it as legacy convenience data; do not silently treat overwritten JSON as published scientific revisions or migrate it automatically. [VERIFIED: helix-desktop/src/lib/workflowLibrary.ts:14-27] An optional explicit validated import may be deferred; no existing-record migration is required by this recommended greenfield domain slice. [ASSUMED: A8]

### Anti-Patterns to Avoid

These are design prohibitions derived from the locked integrity/authority decisions. [ASSUMED: A1-A8]

- One graph for apparatus plumbing and WSL control flow: separate types and validators, share only view primitives.
- Updating published rows or relabeling by changing identity: append a new revision/event instead.
- Re-resolving latest at execution/restart: consume the stored assembly manifest.
- Trusting client actor, policy approval, capability availability, canonical hash or scientific status: resolve/validate on the Agent.
- Treating completed collection steps as collected samples: explicit scientist confirmation only.
- Sorting custody solely by reported occurrence timestamp: project by validated recorded sequence.
- Per-process locking as database concurrency protection: enforce invariants across transactions/processes.
- Silent conflict retries with new mutation IDs, unbounded native proxies or silent successful database-test skips: reject these at verification.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why / source |
|---------|-------------|-------------|--------------|
| SQL transaction/locking | In-memory imitation of atomic multi-record writes | Existing PostgreSQL transactions, FKs, unique constraints and row locks | Database controls have cross-process scope. [CITED: https://www.postgresql.org/docs/16/explicit-locking.html] [CITED: https://www.postgresql.org/docs/16/ddl-constraints.html] |
| SQL escaping | String-concatenated scientist names/labels/units | Existing parameterized Npgsql/Dapper pattern | Parameters separate data from SQL. [CITED: https://www.npgsql.org/doc/basic-usage.html] |
| Full unit interoperability | Ad hoc universal UCUM parser/converter | Explicit bounded approved catalog now; vetted library only if future scope demands it | UCUM special-unit and conformance semantics exceed naive parsing. [CITED: https://ucum.org/ucum]; bounded catalog [ASSUMED: A5] |
| Native session/auth | New renderer token storage or generic HTTP bridge | Existing native broker + Agent verified actor | Keep scientific operations within the established trust boundary. [ASSUMED: A1, A7] |
| Graph interaction | New canvas/gesture implementation | Existing desktop canvas primitives with separate models | Minimize UI scope without authorizing redesign. [ASSUMED: A1, A2] |
| Cryptographic identities/hashes | Custom crypto or client assertions | Platform identity/hash primitives; server canonicalization | Precise hashing representation is a contract, not a novel algorithm. [ASSUMED: A8] |

**Key insight:** Implement the scientific invariants, not replacements for the database, transport, cryptography or broad laboratory standards. [ASSUMED: A1-A8]

## Runtime State Inventory

Included because additive database migration planning affects an existing installation. This is **not** a rename/rebrand or approval to migrate production data. Items not inspected are explicitly unknown; source inspection cannot prove their absence. [ASSUMED: A8]

| Category | Items Found / observation | Action Required |
|----------|---------------------------|-----------------|
| Stored data | Existing PostgreSQL service responded ready; user data contents were not audited. Convenience workflow files are created by the quoted mkdir above; their actual contents were not enumerated. [VERIFIED: session PostgreSQL readiness probe; helix-desktop/src/lib/workflowLibrary.ts:14-27] | Add schema in isolation first; preserve all old rows/files. No silent JSON-to-revision conversion. [ASSUMED: A8] |
| Live service config | Docker PostgreSQL is available; external UI-only service configuration was not inspected. [VERIFIED: session Docker/PostgreSQL probes] | No rename/config migration proposed; scientist/deployment owner verifies runtime grants and connection identity before apply. [ASSUMED: A8] |
| OS-registered state | Not audited; no OS registration rename is proposed. [ASSUMED: A8] | Keep existing Agent/session registration intact; native integration smoke remains a gate. [ASSUMED: A7, A8] |
| Secrets/env vars | Script requires `HELIX_MIGRATION_USER` and `HELIX_RUNTIME_USER`; availability/secret values not queried. [VERIFIED: scripts/verification/phase01-migrate.ps1:7-19] | Confirm distinct configured roles without exposing secrets; no secret rename. [ASSUMED: A8] |
| Build artifacts / installed packages | Installed desktop dependencies and SDKs were probed, but stale build artifacts were not audited. [VERIFIED: session version/dependency probes] | Rebuild affected assemblies/native host; do not infer a passing baseline from presence. [ASSUMED: A6] |

## Common Pitfalls

### 1. Latest defaults violate the plan's published procedure pin
**Failure:** Assembly displays the newest procedure but plan parameters target an older revision. **Avoid:** Validate transitive references and explicitly publish/adopt before changing the plan. **Test:** Publish a new procedure after assembling; old plan and manifest remain unchanged. [ASSUMED: A3]

### 2. Publication or transformations partially commit
**Failure:** A published revision exists without head advancement, or children exist without all parent links. **Avoid:** One connection/transaction and committed idempotency receipt; inject failure at each write boundary. **Warning:** Retries create extra revisions/samples. [ASSUMED: A2, A4, A7]

### 3. Apparatus assertion becomes false safety authority
**Failure:** UI-marked calibration or a cached capability authorizes execution. **Avoid:** Keep recorded assertions separate from live admission; revalidate at the Agent execution boundary. **Test:** Setup gestures/publication produce zero driver dispatches. [ASSUMED: A1, A5]

### 4. Concurrent custody/lineage creates contradictory history
**Failure:** Two moves both claim the same starting state; two independently checked edges create a cycle. **Avoid:** Expected sequence plus transactional locks and graph serialization. **Test:** Race writes on separate connections, not only mocked repositories. [ASSUMED: A4]

### 5. Native broker bounds fail only in real desktop use
**Failure:** Mocked UI accepts a topology that native request limits reject. **Avoid:** Boundary-size fixtures and operation-specific limits, real native smoke, paginated histories. **Warning:** A component test passes while Tauri publication fails. [ASSUMED: A6, A7]

### 6. Administrative completion is mistaken for acceptance
**Failure:** Planner assumes Phase 1 build/migration/native gates passed. **Evidence:** Summary explicitly records "Waived by user; not run." and "Waived by user; not passed." [VERIFIED: .planning/phases/01-agent-safety-boundary/01-08-SUMMARY.md:52-60] **Avoid:** Separate baseline failure, new regression and human acceptance; never turn a waiver or unexecuted test into green evidence. [ASSUMED: A6]

### 7. Append-only SQL appears protected but owner can mutate
**Failure:** Runtime owns tables or can truncate facts despite update/delete triggers. **Avoid:** Distinct migration/runtime roles and explicit negative SQL tests for update/delete/truncate, trigger changes and unauthorized cross-record access. [ASSUMED: A8]

## Code Examples

### Verified existing port shapes

The verbatim discovery and canonical-definition records above are the safe starting points for integration. New scientific revision/manifest/event types must be designed separately; do not guess existing enum members or endpoint values. [VERIFIED: Helix.Orchestration.Core/Devices/DeviceDescriptor.cs:11-16; Helix.Orchestration.Agent/Experiments/IExperimentManager.cs:45-50]

### One-connection transaction pattern

Adapted from Npgsql's official transaction/parameter examples; **illustrative new schema and identifiers, not runnable repository code**. Table/column/parameter names below are proposed and require the new migration/contracts. Existing-package reuse is not a new-install approval. [CITED: https://www.npgsql.org/doc/basic-usage.html] [ASSUMED: A2, A8]

```csharp
await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
await using var command = new NpgsqlCommand(
    "INSERT INTO scientific_publications (revision_id, payload) VALUES ($1, $2)",
    connection, transaction);
command.Parameters.AddWithValue(revisionId);
command.Parameters.AddWithValue(payload);
await command.ExecuteNonQueryAsync(cancellationToken);
// Also validate/lock the draft, advance the head, and persist the operation receipt here.
await transaction.CommitAsync(cancellationToken);
```

This skeleton only illustrates transaction scope. It deliberately omits the full publication implementation; hash/canonicalization, optimistic-token checks, actor attribution, JSONB typing, locks and receipt persistence must be tested together. [ASSUMED: A2, A7, A8]

## State of the Art

| Established reference / old application pattern | Phase approach | Date / impact | Source |
|------------------------------------------------|----------------|---------------|--------|
| Convenience workflow JSON overwrite | Distinct immutable scientific publications and mutable drafts | Application change proposed now; preserve legacy files | Existing overwrite quote above; [ASSUMED: A2, A8] |
| Provenance as freeform metadata | Explicit entity/activity/agent and derivation relationships | PROV-DM Recommendation 2013-04-30; vocabulary, not a mandated serialization | [CITED: https://www.w3.org/TR/prov-dm/] |
| Naive unit normalization | Bounded exact UCUM codes with declared limited scope | UCUM 2.2, 2024-06-17; special units/case sensitivity matter | [CITED: https://ucum.org/ucum] |
| Older ASVS chapter numbering in generic templates | Version-qualified ASVS 5.0.0 controls | Released 2025-05-30; authentication/session/access chapter numbers changed | [CITED: https://owasp.org/www-project-application-security-verification-standard/] [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/docs_en/OWASP_Application_Security_Verification_Standard_5.0.0_en.csv] |

No dependency deprecation or compatibility exclusion is inferred from missing metadata. SDK presence is not a passing build. No latest-package or formal regulatory claim is made.

## Environment Availability

Read-only probes were run on 2026-10-30. Availability is an observation, not a compatibility guarantee or acceptance result. [VERIFIED: session runtime/dependency/Docker probes]

| Dependency | Required By | Available | Observed version | Fallback / gate |
|------------|-------------|-----------|------------------|-----------------|
| .NET SDK | Core/Agent/Infrastructure tests | Yes | Selected `11.0.100-rc.3.26522.148`; SDK list also contains `10.0.100-rc.1.25451.107` | Establish actual warnings-as-errors baseline; approve SDK pin/installation if needed, do not infer unsupported from preview metadata. |
| Node / npm | Desktop tests/build | Yes | `v24.12.0` / `11.6.2` | Run affected tests/build. |
| Cargo | Native broker tests/build | Yes | `1.95.0` | Real Windows native integration still needs acceptance. |
| Docker | Isolated SQL tests | Yes | `29.9.2` | Verify Testcontainers actually starts the isolated fixture. |
| PostgreSQL | Scientific persistence | Yes, existing service ready | `16.15` | Do not use the live user database as a destructive test fixture. |
| Desktop dependencies | UI unit tests | Yes | Versions listed above | Existing install only; no package changes assumed. |
| Distinct deployment database roles | Live migration/immutable grants | Unverified | — | Owner confirms roles before deployment; isolated role tests first. |
| Authenticated native Agent/session flow | Real desktop publication/confirmation | Not exercised in research | — | Explicit native acceptance checkpoint; unit tests do not substitute. |

**Missing dependencies with no approved fallback:** No missing executable was observed, but deployment roles, actual clean builds, and native acceptance remain unverified gates. [VERIFIED: session probes; .planning/phases/01-agent-safety-boundary/01-08-SUMMARY.md:52-68] **Fallback:** Isolated Testcontainers databases for SQL development; mocked clients for unit tests only. Do not weaken authentication or immutability to make smoke tests run. [ASSUMED: A6, A8]

## Validation Architecture

Validation is enabled: configuration contains `"nyquist_validation": true`. [VERIFIED: .planning/config.json:7-13]

### Test Framework

<!-- DATA_u7P9b3Me_START -->
| Property | Value / evidence |
|----------|------------------|
| .NET framework | Existing `<PackageReference Include="xunit" Version="2.*" />` and `<PackageReference Include="Testcontainers.PostgreSql" Version="4.*" />`. [VERIFIED: tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj:12-18] |
| Desktop framework | Existing `"test": "vitest"`, `"vitest": "^4.1.7"`; installed 4.1.7. [VERIFIED: helix-desktop/package.json:6-12,36; session npm probe] |
| Desktop configuration | `environment: "jsdom"`, `setupFiles: ["./src/test/setup.ts"]`. [VERIFIED: helix-desktop/vitest.config.ts:4-10] |
| SQL fixture | `public sealed class PostgresFixture : IAsyncLifetime`; migrations: `Path.Combine(AppContext.BaseDirectory, "migrations")`, `Directory.EnumerateFiles(migrationsPath, "*.sql")`, `.OrderBy(Path.GetFileName, StringComparer.Ordinal)`. [VERIFIED: tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs:8,67-81] |
| Migration copy | `<None Include="..\..\postgres\migrations\*.sql" Link="migrations\%(Filename)%(Extension)" CopyToOutputDirectory="Always" />`. [VERIFIED: tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj:29-31] |
<!-- DATA_u7P9b3Me_END -->

The fixture throws on unavailable initialization: `"PostgreSQL fixture initialization failed; required SQL tests cannot run."` Older test bodies still contain `if (_fixture.ConnectionFactory == null || !_fixture.IsAvailable) return;`. New scientific SQL tests must fail visibly on missing infrastructure and assert availability; they cannot copy silent-return behavior. [VERIFIED: tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs:47-88,120-124]

### Phase Requirements → Test Map

All new suite names, test paths and filters below are **proposed**, not existing APIs/tests. Use these runnable command templates after Wave 0 creates the suites. Warm unit subsets should target under 30 seconds; timing has not been measured, and container startup/full integration is outside that quick-test target. [ASSUMED: A6]

| Req ID | Behavior | Test Type | Automated command | File exists? |
|--------|----------|-----------|-------------------|--------------|
| APP-01 | Typed endpoints/roles/bindings; one draft; explicit immutable publication; stale-token rejection | Unit + SQL integration | `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --filter FullyQualifiedName~ScientificSetup` | Proposed suite — Wave 0 |
| APP-02 | Capability/inspection assertions, exact units and provenance; invalid bounds rejected; zero dispatch | Unit + Agent integration | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter FullyQualifiedName~ScientificSetup` | Proposed suite — Wave 0 |
| APP-03 | Independent revisions; plan procedure pin; derived source revision; graph/cards identity stability | Unit + UI | `npm test --prefix helix-desktop -- --run src/components/ScientificSetup/ScientificSetup.test.tsx` | Proposed file — Wave 0 |
| APP-04 | Frozen transitive refs/policy, later publication unchanged, repository reconstruction and execution consumer | SQL + Agent integration | `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --filter FullyQualifiedName~ScientificSetup` | Proposed suite — Wave 0 |
| SAMP-01 | Only explicit verified scientist confirmation creates collection fact; repeats return same sample | Agent + UI | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter FullyQualifiedName~Samples` | Proposed suite — Wave 0 |
| SAMP-02 | Stable label-independent IDs, ordered custody, atomic many/many lineage, cycle/race rollback | SQL integration + unit | `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --filter FullyQualifiedName~Samples` | Proposed suite — Wave 0 |

### Required fault/security scenarios

Test specification, not completed evidence. [ASSUMED: A6-A9]

- Two concurrent publications of the same expected draft token; exactly one succeeds, and the loser reports conflict.
- Concurrent identical/mismatched idempotency-key requests; committed result is stable; actor/scope mismatch cannot disclose another scientist's receipt.
- Crash/failure before commit produces no publication/sample/partial lineage; lost acknowledgement after commit resolves to the original fact.
- Reconstruct repositories/projections after simulated process restart; no cache-only latest/head/custody authority.
- Publishing a later apparatus/procedure/plan cannot alter prior assembly or execution resolution; mismatch between plan and procedure rejected.
- Wrong actor, expired/terminated session, another window, unauthorized reference, oversized/deep/too-many-edge JSON, unknown unit and non-finite quantity rejected.
- Physical dispatch spy observes zero calls from save/publish/collection/custody operations.
- Runtime database role cannot update/delete/truncate immutable facts or disable safeguards; migration identity can apply additive schema separately.
- Separate SQL connections exercise custody and lineage races. Ensure test filters discover a nonzero test count.
- Native broker rejects wrong-window/destination/redirect/oversize requests and handles lost acknowledgement without a fresh mutation identity.

### Sampling Rate / Commands

Proposed execution commands; no test/build was run during research. Paths/filter names for new suites remain provisional. [ASSUMED: A6]

**Per task commit:** Run the relevant requirement-table subset (Core unit or specific desktop test for fast feedback); run SQL/native subset whenever its boundary changes.

**Per wave merge / full affected suite:**

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

**Phase gate:** Full affected suite green, distinct-role SQL invariants proven in isolation, exact-version/sample flows demonstrated in authenticated Tauri, followed by `/gsd-verify-work`. A failing prior baseline must be named and separately dispositioned, not hidden by weakening warnings or skipping assertions. [ASSUMED: A6, A8]

### Wave 0 Gaps

Proposed files/contracts; the planner should finalize these names and confirm discovery. [ASSUMED: A2-A9]

- [ ] Establish affected restore/build/test baseline and nonzero test discovery; record SDK choice and Phase 1 dependency failures separately.
- [ ] `tests/Helix.Orchestration.Core.Tests/ScientificSetup/` — topology/revision/plan/freeze unit suites for APP-01–APP-04.
- [ ] `tests/Helix.Orchestration.Core.Tests/Samples/` — event projection/quantity/lineage unit suites for SAMP-01–SAMP-02.
- [ ] Infrastructure scientific setup/sample suites — isolated migrations, reconstruction, concurrency, rollback and distinct-role mutation-denial tests; reuse the existing fixture without successful skips.
- [ ] Agent scientific setup/sample integration suites — authentication, verified attribution, idempotency lookup, zero dispatch and frozen execution-reference consumer.
- [ ] `helix-desktop/src/components/ScientificSetup/ScientificSetup.test.tsx` and `helix-desktop/src/components/Samples/Samples.test.tsx` — conflict/error/exact-version/confirmation UI.
- [ ] Native scientific command validation tests and a documented authenticated Windows smoke gate; mocked React/canvas tests are insufficient native acceptance.
- [ ] Approve bounded scientific vocabulary, event correction rules, reference consistency rule, payload sizes and canonical hash test vectors before fixing schema/contracts.
- [ ] No framework install anticipated; any newly introduced package triggers the legitimacy protocol.

## Security Domain

Keep security enforcement enabled. Recommended assessment target is **ASVS 5.0.0 L1 plus explicit high-severity threat gates** for immutable scientific evidence and Agent authority; adopt transaction/locking controls additionally, without claiming formal certification. Confirm this assessment scope in planning. [ASSUMED: A9]

The ASVS 5.0.0 official CSV uses different categories from older generic templates. In this version authentication is V6, sessions V7 and authorization V8; use version-qualified IDs. [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/docs_en/OWASP_Application_Security_Verification_Standard_5.0.0_en.csv]

### Applicable ASVS Categories

Control descriptions/levels are cited from the official CSV; applicability and mitigations are phase design recommendations. [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/docs_en/OWASP_Application_Security_Verification_Standard_5.0.0_en.csv] [ASSUMED: A9]

| ASVS 5.0.0 category | Applies | Controls / phase implementation |
|---------------------|---------|---------------------------------|
| V1 Encoding and Sanitization | Yes | `v5.0.0-1.2.4` (L1): parameterized SQL for names, labels, units and filters. |
| V2 Validation and Business Logic | Yes | `2.2.1`, `2.2.2`, `2.3.1` (L1): allowlists/ranges, trusted Agent validation, valid operation sequence. Add `2.3.3`, `2.3.4` (L2) for atomic multi-step operations/locking; these are not claimed L1 obligations. |
| V3 Web Frontend Security | Yes | `3.2.2` (L1): render scientist labels/metadata as text; no raw HTML interpolation. Native renderer remains untrusted. |
| V4 API and Web Service | Yes | `4.1.1` (L1): correct JSON content types; resource-specific authenticated bounded endpoints. |
| V5 File Handling | Conditional | `5.2.1`, `5.3.2` (L1) if legacy file import is introduced: bound size and avoid user-controlled paths. CSV analytical import remains out of scope. |
| V6 Authentication | Yes, existing boundary reused | Authenticate verified scientist through existing native/Agent flow; setup cannot introduce bypasses or client-provided actor trust. |
| V7 Session Management | Yes | `7.2.1`, `7.4.1` (L1): backend token verification and rejection after session termination; native-only secrets. |
| V8 Authorization | Yes | `8.1.1` (L1): enforce documented authorization rules on revisions, samples, mutation receipts and assembly refs, not only collection creation. |
| V11 Cryptography | Supporting, no new algorithm | Platform cryptographic primitives; canonical hashes are integrity references, not replacements for authorization. |

The shortened numeric IDs in the table all refer to **ASVS 5.0.0**, not older releases. No compliance/retention/e-signature requirement is invented; formal regulatory/e-signature claims are explicitly out of scope: `Formal regulatory/e-signature claims`. [VERIFIED: .planning/REQUIREMENTS.md:60-66]

### Known Threat Patterns for ASP.NET / PostgreSQL / Tauri

Proposed controls and required tests. [ASSUMED: A7-A9]

| Pattern | STRIDE | Standard mitigation |
|---------|--------|---------------------|
| Renderer invents scientist/policy/device assertions | Spoofing / Elevation | Agent-resolved actor, authoritative policy provider, registry validation; no setup dispatch. |
| Replayed or altered mutation identity | Tampering / Repudiation | Actor+operation+canonical request bound receipt; mismatched reuse rejected; acknowledgement lookup. |
| Fact mutation by overprivileged DB role | Tampering | Distinct owner/runtime, least privileges, immutable guards and explicit denial tests. |
| Lost or falsely attributed custody/lineage | Repudiation | Attributed append-only history, server recording sequence, scientific occurrence time and correction provenance. |
| Arbitrary native destination/window/token access | Information disclosure / Elevation | Fixed local transport, denied redirects, window/session checks, registered-command scoping, no renderer token. |
| Oversized graph/history, deep JSON or expensive ancestry query | Denial of service | Per-operation byte/count/depth limits, bounded pages/traversal, cancellation and SQL timeouts. |
| Cross-record guessed IDs | Information disclosure | Server authorization for every record/reference/receipt, not UI hiding. |
| Unsafe imported filenames or interpolated metadata | Tampering / Elevation | Typed data, parameterized SQL, text rendering and fixed safe import scope if introduced. |

**Security gate:** No unmitigated high-severity authority/evidence threat, no session secret disclosure, and real negative authorization/SQL-role/native-boundary tests. A localhost endpoint or passing CORS check is not authentication. [ASSUMED: A9]

## Assumptions Log

All `[ASSUMED]` claims are grouped below. These are recommendations requiring confirmation, not locked user decisions; exact proposed names/paths/codes in examples and tests are included in their groups.

| # | Claim group | Sections | Risk if wrong / confirmation |
|---|-------------|----------|------------------------------|
| A1 | Local Agent scientific API + PostgreSQL ownership, existing-package/native/canvas reuse, no SiteServer requirement, no dispatch from setup | Summary, responsibility map, stack, architecture, don't-hand-roll | Wrong deployment/authority boundary; confirm local service scope. |
| A2 | Concrete topology/draft/publication model, types/fields/catalog semantics, separate WSL, plan derivation, layout projection and proposed paths | Architecture, APIs, SQL, code example, tests | Schema rewrite or accidental scientific mutation; approve model and draft lifecycle details. |
| A3 | Durable assembly manifest, transitive plan/procedure consistency, authoritative policy provider and execution consumer | Freeze pattern, API/test map | APP-04 false completion or incompatible plan binding; confirm provider and integration gate. |
| A4 | Sample/event/label/custody semantics, new child preference, lineage serialization, correction and quantities | Samples, SQL, pitfalls/tests | Invalid provenance or inventory assumptions; approve correction/processing rules. |
| A5 | Bounded typed apparatus/unit vocabulary, calibration metadata interpretation, no universal conversion/conservation claims | Stack, topology, samples, don't-hand-roll | Rejected valid science or unsafe asserted status; scientist approves catalog/dimensions/date rules. |
| A6 | Proposed test paths/suites/filters, command templates, sampling target, native/manual acceptance, baseline disposition | Structure, environment, validation | Undiscovered tests, unavailable integration or false green; create Wave 0 and measure warm subset. |
| A7 | Scientific actor-bound idempotency/lookup, bounded resource-specific native commands/pagination and transport tests | APIs/native, pitfalls, security | Duplicate facts or trust-boundary regression; specify and approve contract/limits. |
| A8 | Additive schema/runner changes, role design, deterministic hash contract, legacy preservation and no automatic data migration | SQL/migrations, inventory, example, environment/tests | Data loss, non-repeatable migration or mutable evidence; isolated migration/role tests and deployment approval. |
| A9 | ASVS 5.0.0 L1 assessment plus high-severity threat gate, category applicability and phase mitigations | Validation, Security Domain | Misstated assurance/compliance or missing threats; approve threat/control matrix, not certification. |

## Research Resolution — six bounded decisions

All six research questions have a planning disposition below. RESOLVED means the approach and its blocking proof/approval gate are specified, not that human approval, implementation, baseline repair, or acceptance occurred. Original A1–A9 assumptions and probe classifications remain honestly flagged; the pending scientist checkpoint is not replaced with a guessed approval.

1. **RESOLVED — vocabulary (A2/A5):** Use the concrete simulator-compatible component/role/port table, ten exact case-sensitive unit codes, observation/expiry interpretation, and representative numerical bounds in `02-PLAN-CONTRACT.md` §§Scientific contract/Common operation and authorization contract. These are proposed model choices under `02-CONTEXT.md` §the agent's Discretion, not laboratory safety rules. `02-01` obtains explicit scientist acceptance of exact catalog/schema/vectors/bounds before the tracer persists scientific evidence. Rejection halts execution and revises affected contracts; no approval is claimed here. Catalog extensions require versioning; unknown semantics fail closed. Source: CONTEXT D-01/discretion; CONTRACT catalog/units/device assertions/canonical vectors; 02-01 checkpoint.
2. **RESOLVED — plan-consistent assembly (A3):** Latest published apparatus and plan default once; use the exact procedure pinned by that plan, show any newer procedure separately, and require explicit compatible adoption and plan publication to change it. Compare snapshot tokens at freeze; conflict requires renewed confirmation. This resolves the apparent latest/pin conflict without changing D-02/D-03. Source: CONTEXT D-02/D-03/D-04; CONTRACT §Authorized policy foundation, assembly-default paragraph; downstream frozen binding plans. Scientist checkpoint presents the interpretation, not a new claim that approval already occurred.
3. **RESOLVED — policy authority (A3):** Implement only the user-authorized immutable content/reference provider plus separate `IPolicyApprovalAuthority` contract. Production default is unavailable and fails closed; content registration never approves. Trusted authority exists only in tests for successful real SQL freeze/reconstruction/canonical binding proof and negative provenance cases. Phase 3 issues and wires actual scientist approval and exercises campaigns; neither missing-provider UI nor test authority constitutes production approval. Source: CONTEXT authorized P-01–P-03; CONTRACT §Authorized policy foundation; `IExperimentManager.cs:45-50` establishes the existing consumer seam only.
4. **RESOLVED — bounded provenance (A4/A5):** Use authenticated explicit collection, immutable UUIDs, append-only events projected by server sequence, reported backdates flagged without reorder, future times rejected, corrections with reason and same-stream prior-event supersession preserving original evidence, and new-child-only Split/Pool/Processing activities. Optional relevant quantity/unit pairs must be positive finite dimension-compatible; no depletion, conservation, retention, signature or inventory policy is inferred. Source: CONTEXT D-05–D-08/discretion; CONTRACT sample/transformation paragraphs. Exact persisted semantics remain subject to 02-01's explicit one-way checkpoint; this resolution does not invent that response.
5. **RESOLVED — baseline/acceptance approach (A6/A8):** 02-01 records fresh SDK/tool/test discovery/exits and representative budgets; failed diagnostics and prior NU1603/NU1902/MSB3277 remain failures. Before expansion the tracer requires actual authenticated native/Agent/isolated SQL proof with nonzero discovery/checked exits. Final acceptance additionally requires isolated roles and actual Tauri human evidence. Missing infrastructure fails the explicit integration gate; waivers and unperformed checks are never green. Present baseline failures for explicit disposition; no remediation expansion or new waiver is authorized. Source: Phase 1 `01-08-SUMMARY.md:52-68`; STATE §Blockers/Concerns; CONTRACT §Dependency and verification contract; 02-01 and final acceptance gates. This is a resolved verification approach, not executed acceptance.
6. **RESOLVED — ordering/state preservation:** Phase 2 precedes 02.1; Phase 3 depends on 02.1. ROADMAP §§Phase 02.1 and Phase 3 explicitly encode these dependencies, corroborated by STATE §Roadmap Evolution. STATE's current-position text is inconsistent, not authority to execute 02.1 first. Orchestrator reconciles position before dispatch without overwriting unrelated STATE/state.json/02.1 edits; planner revises only Phase 2 artifacts and its ROADMAP plan list. CONTEXT deferred redesign remains pending. Source: ROADMAP Phase 02.1 `Depends on: Phase 2`, Phase 3 `Depends on: Phase 02.1`; STATE lines 76–77; CONTEXT canonical state reference and deferred fence.

## Sources

### Primary repository evidence
- Phase context, requirements, AGENTS, infrastructure/desktop/test manifests, discovery/WSL/canonical experiment definitions, Agent composition, native broker, workflow file writer, SQL fixture/command ledger/migration safeguards/runner, and prior acceptance summary — opened source files; line-specific citations and verbatim discrete values are included above.
- Session read-only runtime/npm/Docker/PostgreSQL probes — tool availability and installed version observations only.

### Official documentation (cited; provider confidence LOW)
- Locks, transaction scope, deadlocks/order. [CITED: https://www.postgresql.org/docs/16/explicit-locking.html]
- FKs/uniqueness and cross-row CHECK limitations. [CITED: https://www.postgresql.org/docs/16/ddl-constraints.html]
- Parameters, connections and transactions. [CITED: https://www.npgsql.org/doc/basic-usage.html]
- Entity/activity/agent, revision and derivation; Recommendation 2013-04-30. [CITED: https://www.w3.org/TR/prov-dm/]
- UCUM 2.2, dated 2024-06-17; code case/conformance/special units. [CITED: https://ucum.org/ucum]
- Custom-command permission/default behavior; page updated 2026-09-03. [CITED: https://v2.tauri.app/security/capabilities/]
- ASVS 5.0.0 release date. [CITED: https://owasp.org/www-project-application-security-verification-standard/]
- Versioned category/control text and L1/L2 levels. [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/docs_en/OWASP_Application_Security_Verification_Standard_5.0.0_en.csv]

### Research seam and limitations

The six-question research plan selected Jina, but no usable Jina/Context7 tools or ctx7 CLI were found in the available surface. Fallback search was unhelpful; direct official webfetch supplied the documents listed above. The confidence seam returned `"confidence": "LOW"` for both webfetch and verified webfetch. Six official-source digests were cached with provider webfetch and LOW confidence; no HIGH provider tier was invented. [VERIFIED: session research-plan/tool-discovery/classify-confidence/research-store outputs]

No package registry/legitimacy check, build, automated test, migration apply, native launch or comparative human workspace evaluation was performed during research. A failed official documentation URL returned 404 and contributed no observation. Cited sources establish documentary statements; new architecture, limits and laboratory semantics remain assumptions to confirm.

## Metadata

| Area | Confidence | Reason |
|------|------------|--------|
| Standard stack | LOW for selection claims | Existing manifests/install observations verified, but no registry currency/legitimacy or build compatibility gate run; reuse only. |
| Architecture | LOW | Grounded integration seams and locked decisions; concrete domain/persistence contracts remain proposed. |
| Pitfalls/security | LOW provider tier | Official documents cited and repository caveats read; mitigations/acceptance require implementation tests. |

**Research date:** 2026-10-30
**Revalidation:** Before execution; reopen changing source seams and recheck tools/acceptance. Suggested documentary review by 2026-11-29 is a planning estimate, not a guarantee. [ASSUMED: A6]
