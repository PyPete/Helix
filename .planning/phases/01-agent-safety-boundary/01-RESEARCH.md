# Phase 1: Agent Safety Boundary - Research

**Researched:** 2026-10-29
**Domain:** Authenticated local Agent control, deterministic simulated-command admission, durable effect evidence and reconciliation
**Confidence:** LOW for external documentation guidance (confidence seam); repository observations are source-grounded. Implementation proposals below are explicitly distinguished from existing contracts.

<user_constraints>
## User Constraints (from CONTEXT.md)

The following decisions and discretion are copied verbatim. Source for this entire section: [VERIFIED: .planning/phases/01-agent-safety-boundary/01-CONTEXT.md:14-39,93-96].

### Locked Decisions

### Local control access
- **D-01:** The simulated-device MVP uses a desktop-only loopback path to the local Agent; do not accept lab-network clients or require Site Server proxying. — **Reversibility:** costly — widening the listener and deployment boundary later requires an explicit network security model and client contract.
- **D-02:** The desktop supplies a validated desktop-session identity for the scientist on every Agent request; record that identity in the audit record. — **Reversibility:** costly — identity becomes part of persisted command/evidence records and API middleware.
- **D-03:** Every Agent API, including reads, requires the validated desktop-session identity. Requests without it fail closed and the attempt is audited. — **Reversibility:** costly — changing this later alters the security contract across all Agent routes.

### Admission outcomes
- **D-04:** Ordinary pre-dispatch validation failures (for example missing capability or out-of-bound parameter) reject the command immediately with an operator-facing explanation plus structured failed-check details.
- **D-05:** Unknown or unsafe safety state, lease ownership, or device health places the campaign on hold and requires scientist review before continuation.
- **D-06:** Run policy and safety admission checks at proposal/compile time for feedback and revalidate immediately before every Agent dispatch. — **Reversibility:** costly — safety semantics must be consistent across compilation and runtime execution.

### Restart uncertainty
- **D-07:** When intent is durable but the outcome is unknown after Agent restart, enter `Held pending reconciliation`; never blindly replay the command.
- **D-08:** For simulated devices, automatically query simulator state during reconciliation. If observed state matches the expected outcome, resume automatically and append a prominent recovery audit event.
- **D-09:** If simulator state cannot be read or does not match the expected outcome, retain the hold and require scientist review; do not retry automatically.

### Command evidence
- **D-10:** Present command evidence as a chronological campaign timeline covering proposals, admission decisions, dispatches, observations, recovery, holds, and aborts.
- **D-11:** Show outcome-first event summaries by default: action, device/capability, timestamp, actor, and outcome. Provide expandable parameters, validation checks, identifiers, and observations.
- **D-12:** Original command facts are immutable. Operator context is an attributable append-only annotation or later reconciliation/decision event. — **Reversibility:** costly — persisted evidence/audit semantics rely on append-only history.
- **D-13:** Visually emphasize rejected, held, aborted, safety-related, and reconciliation events; ordinary successful commands remain readable but quiet.

### the agent's Discretion
- Choose concrete desktop-session authentication/transport mechanics that satisfy the loopback-only, fail-closed, all-routes requirement and fit existing Helix patterns.
- Choose exact timeline UI composition and structured diagnostic schema, while preserving the outcome-first and append-only decisions above.

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

## Project Constraints (from AGENTS.md)

The actionable directives below come from the project instruction source, not external best-practice assumptions. [VERIFIED: AGENTS.md:13-21,90-146,173-178,248-254,279-291]

- Only the local Agent addresses drivers; UI/LLM outputs remain untrusted proposals. Deterministically check capability, safety, lease, resource, budget and termination. Execute only simulated devices in this phase; preserve the later real-hardware gate.
- Preserve restart/UI-closure independence, reconcile rather than replay, stable identities, immutable revisions, provenance, units/quality validation and idempotent processing. Preserve the approved-policy model; do not implement autonomous selection, analytical import or apparatus modeling here.
- Put domain contracts/interfaces in Core; concrete composition in hosts/registration extensions. Use logical capability ports, never hardware APIs from Core. Site Server must use proxy/protobuf contracts rather than Agent implementation types. Preserve inward dependencies without cycles.
- Shared mutable coordination must use thread-safe/async primitives, not unsafe collections.
- C# names: PascalCase files matching primary types, public members/types; interfaces prefixed I; async methods suffixed Async; camelCase locals/parameters; private fields _camelCase. TypeScript: PascalCase components/types, camelCase hooks/libraries/props, useX hooks; co-locate frontend tests.
- C#: four-space indentation, file-scoped namespaces, new-line braces, nullable annotations and modern collection/record/pattern constructs. Preserve the edited TypeScript subtree's formatting/export convention; no unrelated reformatting, new barrels or gratuitous documentation conventions.
- Guard constructor dependencies; aggregate expected validation diagnostics. Use typed result objects for expected failures and typed exceptions preserving inner exceptions at operational boundaries. Propagate CancellationToken through I/O; do not swallow unexpected exceptions.
- Inject readonly ILogger<T>; use structured named fields and lifecycle/debug/warning/error levels appropriately. Show remote failures in React state and visible error UI.
- Extract focused helpers; use typed options/requests/interfaces; Task/Task<T> for async APIs, deliberate nullable results. Explain safety/concurrency/protocol rules; XML-document externally meaningful public C# behavior.
- Run dashboard lint if dashboard changes; run C# formatting verification. CI treats warnings as errors: introduce none, and distinguish pre-existing baseline warnings from new violations.
- Start edits through GSD; this artifact is produced within the already initialized phase-research workflow. No application-source or branch changes are part of research.

## Summary

This is a boundary-closure phase, not merely an authentication middleware addition. The inspected default Agent runspace factory builds an in-memory graph traversal and directly invokes the registry; the separately registered workflow executor is not what that factory constructs. Verbatim anchors: `return new RunspaceManager(async (definition, ct) =>`, `var runspace = new InMemoryRunspace(definition, token => body!(token));`, `var result = await capRegistry.InvokeAsync(capName, substitutedParams, token);`, and `builder.Services.AddSingleton<IWorkflowExecutor, WorkflowExecutor>();`. [VERIFIED: Helix.Orchestration.Agent/Program.cs:108-112,181-186,247-248]

Plan one authoritative Agent dispatch service, simulator-only registration, authenticated native-desktop access, durable command intent/evidence before effects, and a durable control/reconciliation state machine. Compile-time diagnostics reuse the admission evaluator, but only a fresh runtime evaluation with reserved resources and an open termination fence may authorize dispatch. A database outage must stop new dispatch, not degrade to unaudited execution. These are proposed implementation patterns, not existing working guarantees. [ASSUMED: A1]

**Primary recommendation:** Deliver a thin, authenticated simulated execution vertical slice through the actual host composition, with crash-window tests and an append-only timeline, before adding broader campaign behavior. Do not try to repair every legacy executor independently. [ASSUMED: A1]

<phase_requirements>
## Phase Requirements

Descriptions are copied verbatim from the requirement definitions. [VERIFIED: .planning/REQUIREMENTS.md:12-15]

| ID | Description | Research Support |
|----|-------------|------------------|
| SAFE-01 | The system dispatches device commands only through the local Helix Agent. | Loopback/native-session boundary, route inventory, simulator-only registration and a single guarded dispatch port. |
| SAFE-02 | The Agent dispatches a command only after capability, safety, lease, resource, budget, and termination checks pass. | Shared typed admission evaluator, pre-dispatch revalidation, reservation transaction and termination fencing. |
| SAFE-03 | A scientist can pause, abort, or hold a campaign, and the Agent reconciles uncertain physical outcomes after restart. | Durable control intent, distinct hold/abort semantics, effect ledger and simulator-state reconciliation. |
| SAFE-04 | The system records each command's intent, observation, correlation, and audit context. | Persist-before-publish append-only evidence, actor/correlation identity and restart-safe timeline retrieval. |

Research Support entries describe proposed implementation, not current coverage. [ASSUMED: A1]
</phase_requirements>

## Architectural Responsibility Map

This map is the recommended ownership model, constrained by Agent-only execution authority. [ASSUMED: A1] [VERIFIED: AGENTS.md:15-20]

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Proposal editing and outcome-first timeline | Desktop renderer | Native desktop broker | Renderer proposes and displays; it never owns device authority or session secrets. |
| Scientist session and authenticated transport | Native desktop broker + Agent backend | OS identity boundary | Broker holds credential; Agent validates each call and attributes evidence. |
| Admission and command dispatch | Agent backend | Core domain evaluator | One service owns the final gate; Core defines deterministic rules without driver composition. |
| Resource/lease/budget/control coordination | Agent backend | PostgreSQL | Durable serialized decisions survive UI closure and restart. |
| Simulator command and state observation | Agent-owned simulator adapter | Durable simulator store | Device behavior is reached only after dispatch authorization; observed state is independently queryable. |
| Evidence durability/order | PostgreSQL | Agent timeline API/stream | Durable sequence is canonical; streams are notifications, not the source of truth. |
| Restart reconciliation | Agent backend | Simulator + PostgreSQL | Resume only after matching observations, never browser-driven retries. |
| Site Server | Outside Phase 1 execution path | — | No mandatory network/proxy dependency under D-01. |

## Standard Stack

**Use existing dependencies; do not upgrade or introduce packages merely for this phase.** Rows record declarations, not current registry versions or resolved lockfile versions. Package-install recommendations are intentionally deferred until a concrete missing dependency is demonstrated. [ASSUMED: A2]

| Existing component | Declared version / verbatim source | Use in phase |
|--------------------|------------------------------------|--------------|
| ASP.NET Core / C# | `<TargetFramework>net10.0</TargetFramework>` [VERIFIED: tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj:1-8] | Agent middleware, endpoints, DI, typed admission and host tests. |
| PostgreSQL access | `<PackageReference Include="Npgsql" Version="9.*" />`; `<PackageReference Include="Dapper" Version="2.*" />` [VERIFIED: Helix.Orchestration.Infrastructure/Helix.Orchestration.Infrastructure.csproj:12-13] | Transactional ledger, reservations, control state and evidence repositories. |
| Native desktop | `tauri = { version = "2", features = [] }`; `tokio = { version = "1", features = ["full"] }`; `tonic = "0.14"` [VERIFIED: helix-desktop/src-tauri/Cargo.toml:25-31] | Credential-bearing native transport broker. Generator feasibility is a Wave 0 gate, not a working integration claim. |
| React renderer | `"react": "^18.3.0"`; `"react-dom": "^18.3.0"` [VERIFIED: helix-desktop/package.json:20-21] | Session status, structured admission feedback, control actions and timeline. |
| Agent tests | `<PackageReference Include="xunit" Version="2.9.*" />`; `<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />` [VERIFIED: tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj:12-15] | Real route/middleware composition with spy simulator and durable-store seams. |
| Desktop tests | `"test": "vitest"`; `"vitest": "^4.1.7"`; `"@testing-library/react": "^16.3.2"` [VERIFIED: helix-desktop/package.json:11,27,36] | Broker client integration and timeline/control rendering. |

**Installation:** None proposed. Reuse project restore/lockfiles. Registry latest versions/publish dates were not established; do not treat the declaration ranges above as latest-version verification. Native protocol-code generation must be proven before locking that implementation. [ASSUMED: A2]

### Package Legitimacy Audit

Not applicable to an approved install: no new external packages are recommended for installation by this research. Existing declarations are verified only as repository declarations, never as `[VERIFIED: npm registry]`. No legitimacy verdict or registry freshness is claimed. If the native broker needs generator/runtime additions, first discover them in official documentation, run the ecosystem-specific legitimacy gate and registry verification, then add installation/checkpoint tasks. Do not guess package names. [ASSUMED: A2]

### Don't change the execution engine by accident

Retain existing durable-workflow infrastructure only if its actual registered execution path can be made to call the single dispatch service and preserve a restart cursor. Durable workflow persistence is not, by itself, proof of exactly-once external device effects. The current separate executor registration proves availability in DI, not authority in the default route. [ASSUMED: A1] [VERIFIED: Helix.Orchestration.Agent/Program.cs:108-112,247-248]

## Architecture Patterns

### System Architecture Diagram

Recommended conceptual flow; names here are proposed responsibilities, not existing API/type values. [ASSUMED: A1]

~~~text
Scientist
  -> Desktop renderer (untrusted proposal)
  -> Native broker (validated local session, fixed loopback destination)
  -> Agent authentication boundary
       -> failure: deny + security-attempt evidence
       -> accepted actor
            -> proposal validation / durable control request
            -> canonical command intent + policy snapshot
            -> serialized runtime admission and reservation
                 -> reject: failed checks + explanation
                 -> hold: durable block + review evidence
                 -> pass: durable dispatch intent / termination fence
                      -> Agent-owned simulator
                           -> observed outcome -> ledger commit
                           -> uncertain outcome -> durable hold
            -> evidence query / notification -> renderer timeline

Agent restart
  -> scan incomplete command intents
  -> hold pending reconciliation
  -> authenticated/internal authorized simulator observation
       -> matching expected effect AND no independent hold/abort:
            recovery evidence + resume at next command
       -> unreadable/nonmatching: hold + scientist review

PostgreSQL failure -> no new dispatch
No Site Server, lab network or real-device execution dependency
~~~

### Component Responsibilities and Recommended Structure

Place new domain contracts/evaluator in Core, dispatch/control/recovery orchestration in Agent, PostgreSQL repositories/migrations in Infrastructure, simulator persistence/observation behind Agent-owned ports, and a native broker plus one renderer client in desktop. Exact new filenames, namespaces, tables and routes are planner-owned proposals: do not infer their existence from this map. [ASSUMED: A1]

### Pattern 1: Credential-bearing native broker

**Use a native Tauri broker for all Agent communication, with a fixed loopback destination and an opaque expiring desktop-session credential kept out of renderer storage.** Prefer the already declared native gRPC stack if a small code-generation/build probe proves it viable; expose proposal/manual command, control, evidence and read operations through the same authenticated boundary. Do not retain renderer HTTP/WebSocket paths as fallback bypasses. A single renderer-facing typed client should route health/read/control/evidence calls through the broker. This concrete protocol choice remains subject to the build gate and scientist-session bootstrap confirmation. [ASSUMED: A3]

Agent validates session identity on every REST, gRPC and WebSocket request that remains exposed; denied attempts record claimed identity separately from verified actor. Use ASP.NET Core authentication/authorization infrastructure with a fail-closed fallback policy, explicit coverage tests, and authentication before WebSocket upgrade/stream subscription. Session validation is not a user-supplied actor header. CORS is not an authentication mechanism. [CITED: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/secure-data?view=aspnetcore-10.0] [CITED: https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0]

Tauri custom application commands are allowed to windows by default unless constrained with the application command manifest; scope native commands to the trusted local window/capability, validate native command arguments and never expose arbitrary URL forwarding or return secrets to JavaScript. Capabilities do not replace Rust-side scope enforcement. [CITED: https://v2.tauri.app/security/capabilities/]

Code/configuration Kestrel endpoints are cumulative; adding a loopback endpoint does not establish that wildcard listeners disappear. HTTP/2-only cleartext gRPC and mixed-protocol cleartext listeners behave differently. Verify the real binding/protocol and remove conflicting configuration rather than adding another listener. [CITED: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0]

**Bootstrap decision checkpoint:** Confirm how an OS desktop session is associated with a scientist identity, who provisions/revokes sessions and whether OS-account compromise is inside the threat model. Recommended MVP: operator-approved pairing to an OS-user-scoped native desktop session, short-lived opaque credential, revocation and reauthentication, never renderer-declared identity. Do not silently substitute a machine-wide static API key or assume OS username establishes scientific identity. No exact lifetime, pairing ceremony or storage API is locked. [ASSUMED: A3]

The Rust manifest already declares the native protocol dependency, but also contains `tonic-build = "0.14"` and the directive `# Do NOT add prost explicitly — let tonic pull its required version transitively`. This does not prove working generated clients; honor the directive or explicitly resolve it before adding dependencies. [VERIFIED: helix-desktop/src-tauri/Cargo.toml:20-22,31-32]

### Pattern 2: One command-admission and dispatch service

The proposed service receives trusted actor context, canonical intent, frozen policy/limits, stable command identity and the current control fence. Every manual, compiled-workflow, raw-runspace, gRPC and recovery mutation must use it. The raw registry/adapter must not be injectable as an alternate mutation port into execution callers. Read capabilities require explicit read-only classification: variable substitution cannot choose an arbitrary mutating capability. Reject unsupported workflow constructs rather than accepting guards the active executor does not enforce. [ASSUMED: A1]

Use one deterministic evaluator at proposal time and runtime, with a fresh context at runtime. Aggregate structured checks for feedback; runtime admission authorizes nothing until all required checks pass and reservations/fencing are established. Variable-substituted values must be normalized and revalidated, including finite numeric values, units, capability bounds and resource references. [ASSUMED: A4]

| Check | Required runtime evidence | Nonpass disposition |
|-------|---------------------------|---------------------|
| Capability | Registered simulator device; allowed operation; typed finite parameters and explicit bounds | Missing/disallowed/malformed/out-of-bound: reject. |
| Safety | Current known safe state and required interlocks, not historical warnings alone | Unsafe/unknown: hold; scientist review. |
| Lease | Durable owner, validity/expiry, fencing generation; no stale competing owner | Unknown/conflicting ownership: hold. |
| Resource | Explicit requested resource set and atomic capacity reservation | Known invalid/overcapacity proposal: reject; uncertain allocation: hold. |
| Budget | Durable remaining command/time/resource budget plus reservation | Known exhausted policy: reject; inconsistent/unknown accounting: hold. |
| Termination | Current durable control revision, stop rules/expiry, no pause/hold/abort fence | Do not dispatch; honor control state and explain the block. |
| Device health | Fresh readable simulator/adapter health | Unsafe/unknown: hold. |

Table entries are proposed detailed semantics; D-04–D-06 lock the ordinary-reject/uncertain-hold split, not a complete diagnostic enum. [ASSUMED: A4] [VERIFIED: .planning/phases/01-agent-safety-boundary/01-CONTEXT.md:22-24]

Phase 1 needs minimal explicit limits and a test policy envelope, not the full later campaign-policy authoring feature. Unknown limits cannot mean unlimited. Confirm numerical bounds, observation freshness, time/budget accounting and release/hold review semantics before locking defaults. [ASSUMED: A4]

### Pattern 3: Durable effect ledger plus serialized control

Persist immutable intent and admission evidence before dispatch. Serialize control revision, lease/resource/budget reservation and intent commitment per execution/device using database transactions and fencing; a final fenced dispatch check prevents a control request racing the next command. Do not keep a transaction open for a long device operation. Represent dispatch authorization and outcome separately; the external effect and database outcome cannot be assumed to commit atomically. [ASSUMED: A5]

PostgreSQL row locks can serialize conflicting updates; they last until transaction end and competing lock orders can deadlock. Use consistent acquisition order and bounded transactional retries that never retry an uncertain external effect. [CITED: https://www.postgresql.org/docs/current/explicit-locking.html]

Proposed ledger fields: stable command/campaign/run/step/attempt and correlation identities, verified actor/session reference, policy/definition revision, canonical parameters/units, expected postcondition, check results and context revision, control fence, reservation/debit references, intent/dispatch/observation timestamps, durable sequence, observed state and reconciliation disposition. Names and schema are proposals, not existing definitions. Authentication credentials themselves must never enter evidence. [ASSUMED: A5]

Use unique constraints for command deduplication and evidence identity. A retry with the same canonical identity/content returns the existing result; reuse of the identity with different content rejects. Acknowledgement loss never creates a new effect attempt automatically. Publish timeline notifications only after evidence commit; reconnecting clients query durable history by cursor. [ASSUMED: A5]

### Pattern 4: Pause, hold, abort and reconciliation are durable controls

Existing runspace values are exactly `Idle`, `Running`, `Paused`, `Completed`, `Faulted`, `Disposed`; the declaration documents fault as `/// <summary>Workflow faulted or was aborted.</summary>`. There is no existing held/aborted runspace value in this definition. Do not insert assumed enum strings into existing clients or schemas; add a separate durable control/reconciliation contract or deliberately migrate every consumer. [VERIFIED: Helix.Orchestration.Core/Runspace/RunspaceStatus.cs:6-24]

Recommended semantics: pause stops future dispatch at a safe boundary; hold persists a review/uncertainty block; abort persists terminal termination before cancellation and never resumes. Cancellation means a stop was requested, not that an in-flight effect was undone. Resume continues from a durable cursor only after fresh admission; never starts the graph from the beginning. Review release must be attributable and must not bypass fresh safety/lease/health checks. [ASSUMED: A6]

On restart, every durable intent without a trustworthy outcome enters the locked `Held pending reconciliation` disposition. Query simulator state through a read-only internal observation port, compare to the recorded expected postcondition and append the observation/recovery evidence. A matching effect advances past that command without issuing it again; nonmatching/unreadable state retains hold. Automatic resume clears only the reconciliation hold, not an independent operator/safety hold or abort. [ASSUMED: A6] [VERIFIED: .planning/phases/01-agent-safety-boundary/01-CONTEXT.md:27-29]

Persist simulator state independently enough that a restarted Agent can observe the effect rather than recreate a default device. Simulator setters must actually modify state; model at least one deterministic, observable setter. A state value matching by coincidence is weak evidence: include simulator generation/effect identity where possible, or document this limitation and use a command whose expected outcome can be unambiguously reconciled. Matching tolerance and state freshness require explicit decisions. [ASSUMED: A6]

### Pattern 5: Evidence first, timeline second

Use an append-only per-execution sequence as canonical ordering; timestamps remain display evidence. Fetch durable history on load/reconnect and merge notification events by stable identity. Include unattributed/manual/security attempts in an appropriate Agent audit stream, not only active-experiment events. Keep completed histories navigable and never edit original command facts when adding operator context. [ASSUMED: A7]

Render outcome-first summary with action, device/capability, time, actor and outcome; expandable parameters, checks, identities and observations. Emphasize rejection/hold/abort/safety/recovery with accessible text/icons as well as color. Avoid per-event animation/noise for normal successes. D-10–D-13 prescribe these display/evidence properties; exact components and accessibility composition are proposed. [VERIFIED: .planning/phases/01-agent-safety-boundary/01-CONTEXT.md:32-35] [ASSUMED: A7]

### Existing Bypass and Integration Audit

The following observations were made by opening application sources during this research. These are grounded findings, not a claim that legacy paths are safe. Source citations identify behavior; exact new routes/types must be selected during planning. [CITED: Helix.Orchestration.Agent/Program.cs] [CITED: Helix.Orchestration.Agent/Api/DeviceEndpoints.cs] [CITED: Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs]

| Observed seam | Finding and required planning response | Source |
|---------------|-----------------------------------------|--------|
| Host default executor | In-memory traversal directly invokes the registry; route the actual factory through guarded dispatch/durable cursor, not only a separate executor class. | [VERIFIED: Helix.Orchestration.Agent/Program.cs:108-112,181-186] |
| Manual dispatch | Direct invocation precedes manual event emission; move admission and intent commitment ahead of effect. | [CITED: Helix.Orchestration.Agent/Api/DeviceEndpoints.cs] |
| Raw runspace REST/gRPC | Client definitions can enter execution without the intended compile/admission path; recompile/canonicalize on Agent or reject unsupported raw submission. | [CITED: Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs] [CITED: Helix.Orchestration.Agent/gRPC/Server/AgentGrpcService.cs] |
| Device registration/bridge | Real-device adapters are registered alongside simulator; simulator-only composition must remove reachability rather than hide UI. | [CITED: Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs] [CITED: Helix.Orchestration.Agent/Adapters/Devices/HelixAdapterCapabilityBridge.cs] |
| Registry exceptions | Broad exception-to-failure conversion loses cancellation and uncertain-outcome distinctions; preserve effect uncertainty at dispatch boundary. | [CITED: Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs] |
| Compiler safety | Node/type validation and historical safety injection are not complete parameter/admission enforcement; active traversal does not establish guard execution. | [CITED: Helix.Orchestration.Core/WSL/WslCompiler.cs] [VERIFIED: Helix.Orchestration.Agent/Program.cs:141-144,181-203] |
| Variable capability reads | Read helper invokes the supplied capability and silently defaults on failed invocation/conversion/exception; restrict to read-only observation and fail closed where admission relies on it. Verbatim anchors: `await invoker.InvokeAsync(capabilityName, parameters, cancellationToken)`, `return default;`. | [VERIFIED: Helix.Orchestration.Core/WorkflowEngine/CapabilityReadExtensions.cs:24-39,55-73] |
| Alternate WSL executor | Separate compilation/lease path starts a durable runspace and releases acquired leases only in its catch block. Verbatim anchors: `var definition = document.Compile();`, `var runspace = new DurableRunspace(definition, _workflowExecutor);`, `await lease.DisposeAsync().ConfigureAwait(false);`. Integrate or retire this path rather than add a second policy gate. | [VERIFIED: Helix.Orchestration.Agent/Execution/WslExecutionAdapter.cs:39-68] |
| Durable runspace controls | Pause cancellation/resume restart and abort-as-fault are insufficient for durable cursor/hold semantics. | [CITED: Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs] |
| Simulator | Inspected setters return/log success without updating their setpoint fields; process-local state cannot prove restart matching. | [CITED: Helix.Orchestration.Devices/Drivers/SimulatorDevice.cs] |
| Desktop execution integration | Compiled definition is checked but not included in submission. Verbatim anchors: `const runspaceDef = await handleCompile();`, `body: JSON.stringify({ name: definition.name ?? "Experiment" }),`. Test a real nonempty workflow through the native contract; separately align shell lifecycle actions. | [VERIFIED: helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx:93-101] [CITED: helix-desktop/src/shell/HelixShell.tsx] |
| Evidence pipeline | Post-broadcast best-effort experiment persistence can drop audit events; replace canonical evidence ownership with synchronous durable commit plus notifications. | [CITED: Helix.Orchestration.Agent/Experiments/ExperimentEventPersistenceWorker.cs] |
| Timeline | Volatile arrays/timestamp sorting and active-experiment clearing cannot establish durable history/order; use durable identities/cursor and completed-history selection. | [CITED: helix-desktop/src/shell/panels/ExperimentTimeline.tsx] |

File mapping in the broad audit is navigation evidence, not a guaranteed generated-path contract. Before assigning edits, the planner must locate/open the definitions. No proposed filesystem output is asserted to exist. [ASSUMED: A8]

### Anti-Patterns to Avoid

- Protecting only writes, HTTP routes or the UI while gRPC/read/upgrade/health/config paths remain anonymous.
- CORS/origin/loopback as identity, renderer-stored bearer credentials, arbitrary native URL proxying or machine-wide identity headers.
- Safety warnings without enforced runtime checks; injected graph guards ignored by the active executor.
- Stale lease/budget checks followed by unconstrained dispatch; treating cancellation as confirmed physical stop.
- Broad workflow replay or automatic retry after uncertain dispatch.
- In-memory leases/control/evidence as canonical state, broadcast-before-persist, timestamp-only ordering or mutable original evidence.

These are proposed prohibitions justified by the locked boundary/recovery requirements, not claims that every legacy subsystem has every defect. [ASSUMED: A1]

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| All-route authorization | Endpoint-by-endpoint identity-header checks | ASP.NET Core authentication handlers/policies plus complete route tests | Centralizes identity and default-deny behavior. [CITED: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/secure-data?view=aspnetcore-10.0] |
| Native HTTP framing / gRPC wire format | Socket protocol parsing or browser credential workarounds | Existing native gRPC stack after generator gate | Avoid custom transport and renderer secrets. [ASSUMED: A3] |
| Resource concurrency | Process-local dictionaries as durable locks | PostgreSQL transactions/row locks + application fencing | Serializes durable updates; locks alone are not external-effect atomicity. [CITED: https://www.postgresql.org/docs/current/explicit-locking.html] [ASSUMED: A5] |
| Credentials / cryptography | Custom signatures, token algorithms or encryption | Framework/OS credential primitives selected during bootstrap design | Custom crypto is outside phase scope; concrete OS integration still needs confirmation. [ASSUMED: A3] |
| Durable device effects | Generic workflow retry assumed exactly-once | Explicit command/effect ledger and simulator reconciliation | A durable function cannot establish an unobserved device outcome. [ASSUMED: A5] |

**Key insight:** The domain admission/ledger must be explicit, but authentication, transport and database concurrency should use maintained platform mechanisms. [ASSUMED: A1]

## Runtime State Inventory

This phase refactors execution authority and introduces durable state; source edits alone cannot establish runtime migration readiness. Inventory below explicitly distinguishes inspected definitions from unobserved installations. No live-state absence is asserted. [ASSUMED: A9]

| Category | Items Found / Observation | Action Required |
|----------|---------------------------|-----------------|
| Stored data | Existing experiment event persistence and PostgreSQL migration definitions inspected; no reachable running database was established. Live records/schema are unknown. [CITED: Helix.Orchestration.Agent/Experiments/ExperimentEventPersistenceWorker.cs] [CITED: postgres/migrations/002_experiment_events.sql] | Add ledger/control/evidence migrations; decide treatment of legacy records with unknown actor/correlation, preserve them without fabricating identities. Verify against a real database. [ASSUMED: A9] |
| Live service config | Repository container/listener configuration exists; installed service/container configuration and external broker state were not queried. [CITED: docker-compose.yml] | Audit effective listeners/overrides and health probes; remove lab-network/wildcard Agent exposure for this MVP. No Site Server migration is required unless actually deployed against this Agent. [ASSUMED: A9] |
| OS-registered state | No service/task-registration audit performed; installed Windows/system service state is unknown. | Confirm launched Agent executable, listener arguments, OS-user ownership and restart policy before acceptance. [ASSUMED: A9] |
| Secrets/env vars | No session bootstrap exists in the inspected host flow; untracked secrets/process environment were not enumerated. Do not infer absence. [CITED: Helix.Orchestration.Agent/Program.cs] | Inventory only relevant key names and overrides without exposing values; provision/revoke new desktop sessions and document upgrade behavior. [ASSUMED: A9] |
| Build artifacts / installed packages | Managed/frontend focused tests run; native generator/client packaging not demonstrated. Installed desktop/service binaries may remain stale. | Build native/managed artifacts and deploy coherently; ensure obsolete renderer routes and old executable registrations cannot bypass the new boundary. [ASSUMED: A9] |

## Common Pitfalls

### 1. Securing the wrong execution implementation
**What goes wrong:** Unit tests pass against an admission-enabled executor while the default host still instantiates the direct registry traversal.
**Avoid:** Host-composition route tests with a spy simulator and denied commands must demonstrate zero effects on every input path.
**Warning signs:** New safety service registered but not used by manual/raw-runspace/default factory paths. [VERIFIED: Helix.Orchestration.Agent/Program.cs:108-112,181-186,247-248] [ASSUMED: A1]

### 2. Durable intent is confused with an observed outcome
**What goes wrong:** Crash after effect but before outcome commit causes duplicate replay; cancelling the call is misreported as no effect.
**Avoid:** Persist effect identity/postcondition, mark uncertain commands held and query simulator state before moving the cursor.
**Warning signs:** Generic retry logic wraps dispatch; resume starts the entire graph; missing crash injection between effect and outcome commit. [ASSUMED: A5, A6]

### 3. The simulator cannot actually demonstrate recovery
**What goes wrong:** Setter success does not change state, or state resets on restart; reconciliation tests then test mocks rather than effect recovery.
**Avoid:** Implement deterministic observable state and restart-surviving simulator state; test independent process recreation against the same durable store.
**Warning signs:** Logging-only setters, untouched fields, default state manufactured on restart. [CITED: Helix.Orchestration.Devices/Drivers/SimulatorDevice.cs] [ASSUMED: A6]

### 4. All-routes authentication breaks overlooked clients
**What goes wrong:** Health/read/WebSocket/gRPC clients fail; someone adds an anonymous exception, violating D-03.
**Avoid:** Update every desktop client and deployment probe or make non-API process health checks; test denied reads/upgrades, not just write routes.
**Warning signs:** Query-string credentials, anonymous health endpoints, old direct renderer fetches. [VERIFIED: .planning/phases/01-agent-safety-boundary/01-CONTEXT.md:17-19] [ASSUMED: A3]

### 5. Database-unavailable tests silently pass
**What goes wrong:** Repository integration tests return without assertions when their fixture cannot connect.
**Avoid:** Make required integration setup fail visibly or explicitly skip with an acceptance-blocking report; full safety gate requires real PostgreSQL restart tests.
**Warning signs:** Green suite while Docker/database is unavailable. [CITED: tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs] [ASSUMED: A10]

### 6. Timeline evidence is best effort
**What goes wrong:** Broadcast events look convincing but disappear after restart; manual/security events lack experiment attribution and never persist.
**Avoid:** Canonical committed ledger, per-stream sequence and stable identity; display only persisted facts, append review annotations.
**Warning signs:** Logging-and-dropping persistence errors; no durable reconnect cursor; duplicate event arrays. [CITED: Helix.Orchestration.Agent/Experiments/ExperimentEventPersistenceWorker.cs] [ASSUMED: A7]

## Code Examples

### Existing contract that must not be assumed to support new control states

Verbatim enum value quote: `Idle`, `Running`, `Paused`, `Completed`, `Faulted`, `Disposed`. [VERIFIED: Helix.Orchestration.Core/Runspace/RunspaceStatus.cs:6-24]

~~~csharp
// Existing values only; new hold/abort semantics need a deliberate contract.
public enum RunspaceStatus
{
    Idle,
    Running,
    Paused,
    Completed,
    Faulted,
    Disposed
}
~~~

### Proposed dispatch algorithm — not drop-in API code

The skeleton intentionally avoids invented project enums/routes/error codes. Operations and ordering below are a proposed design requiring concrete contracts and tests. [ASSUMED: A1, A5, A6]

~~~text
validate authenticated actor and canonical command identity
resolve substituted parameters using read-only observation ports
persist immutable proposal and compile-time diagnostics
serialize execution control, lease, resource and budget decisions
    obtain fresh safety/health and policy context
    reject invalid proposal OR persist review hold
    otherwise reserve limits and commit dispatch intent/fence
recheck termination fence at dispatch serialization boundary
invoke Agent-owned simulator once
persist observed outcome and resulting accounting/cursor
publish notification of committed evidence
if dispatch outcome is uncertain:
    persist/recover hold; do not automatically invoke again
~~~

### PostgreSQL serialization pattern

Official locking documentation supports acquiring a row lock with a select-for-update operation inside a transaction; table/key names must come from the new migration, not this document. [CITED: https://www.postgresql.org/docs/current/explicit-locking.html]

~~~sql
-- Conceptual shape only: identifiers are deliberately not invented here.
BEGIN;
-- SELECT the execution-control row FOR UPDATE.
-- Check control revision and reserve resources/budget; append intent evidence.
COMMIT;
-- External dispatch happens outside the transaction under the dispatch fence.
~~~

## State of the Art

| Existing approach observed | Phase target | Impact |
|----------------------------|--------------|--------|
| In-memory graph traversal + direct registry dispatch [VERIFIED: Helix.Orchestration.Agent/Program.cs:108-112,181-186] | One guarded dispatch service and durable cursor [ASSUMED: A1] | Tests cover actual host authority rather than nominal service registration. |
| Runspace abort represented in fault lifecycle [VERIFIED: Helix.Orchestration.Core/Runspace/RunspaceStatus.cs:20-21] | Distinct durable control intent/reconciliation semantics [ASSUMED: A6] | Scientist stop/hold remains distinguishable from execution failure. |
| Best-effort post-broadcast event persistence [CITED: Helix.Orchestration.Agent/Experiments/ExperimentEventPersistenceWorker.cs] | Persist-before-publish command evidence [ASSUMED: A7] | Restart-safe, attributable history. |

This is a repository modernization comparison, not a claim about dated industry deprecations. No external-library deprecation or release transition was established.

## Environment Availability

Actual probes are research observations, not deployment compatibility guarantees. [VERIFIED: session shell availability probes, 2026-10-29]

| Dependency | Available / observed version | Required By | Fallback / action |
|------------|------------------------------|-------------|-------------------|
| .NET SDK | Yes; `11.0.100-preview.2.26159.112` | Existing net10.0 managed build/tests | Focused tests succeeded; verify supported project SDK/toolchain and warnings-as-errors separately. |
| Node.js / npm | Yes; `v24.12.0` / `11.6.2` | Desktop tests/build | Existing focused tests succeeded; no latest-version claim. |
| Rust / Cargo | Yes; `rustc 1.95.0 (59807616e 2026-04-14)` / `cargo 1.95.0 (f2d3ce0bd 2026-03-21)` | Native broker build | Native build/generation still a validation gate. |
| Protobuf compiler | Command discovery produced no available command in this shell | Native gRPC generation | Confirm official supported generation path or provision dependency after legitimacy audit. |
| Docker daemon | Unavailable; `failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine` | Container-backed PostgreSQL/crash integration | Start Docker Desktop or supply a real local PostgreSQL instance. |
| Agent / PostgreSQL / MQTT service | No listening service established by inspected local probes | Live acceptance tests | Start simulator-only Agent and PostgreSQL; no real devices. MQTT is not canonical safety evidence and need not block the thin slice. |
| Context7 / ctx7 | No available tool/CLI discovered | Curated documentation lookup | Official documentation fetched directly; confidence seam reported LOW for actual fallback provider. |

**Blocking acceptance dependency:** A real durable database and native broker build must be available before declaring the phase implemented/verified. Unit fakes can support early tasks but cannot substitute for restart acceptance. Research/planning itself is not blocked. [ASSUMED: A10]

## Validation Architecture

Validation is enabled: configuration contains the exact value `"nyquist_validation": true`. [VERIFIED: .planning/config.json:7-12]

### Test Framework

| Property | Value / grounding |
|----------|-------------------|
| Managed framework | Existing `xunit` declaration `Version="2.9.*"`, host-test dependency `Microsoft.AspNetCore.Mvc.Testing` `Version="10.*"`. [VERIFIED: tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj:12-15] |
| Renderer framework | Existing `"test": "vitest"`, `"vitest": "^4.1.7"`. [VERIFIED: helix-desktop/package.json:11,36] |
| Configuration | Existing test projects and desktop Vitest config were inspected; planner must reuse them, not introduce a second framework. [CITED: .planning/codebase/TESTING.md] |
| Quick managed command | Existing focused safety command below; new tests require separate filters before it becomes a Phase 1 quick gate. [VERIFIED: session focused test execution, 2026-10-29] |
| Full managed command | Run all affected Core, Agent and Infrastructure test projects plus format/build checks; commands below are proposed acceptance commands, not executed full-suite results. [ASSUMED: A10] |
| Native tests | Add broker scope/session tests and a real packaged/native transport smoke test; no native success is claimed. [ASSUMED: A10] |

**Observed baseline:** focused Core safety tests: 9 passed. Focused desktop health/manual-control/timeline tests: 14 passed in 3 files. These do not cover authenticated route closure, durable control, command intent/outcome windows or restart reconciliation. Existing build emitted dependency/compiler/platform warnings, including simulator-field and OPC-UA dependency warnings; passing tests does not prove warnings-as-errors compliance. [VERIFIED: session focused test output, 2026-10-29]

Executed quick commands (paths identify actual invocation targets, not a new filesystem-generation claim): [VERIFIED: session focused test execution, 2026-10-29]

~~~powershell
dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~SafetyEngineTests --verbosity minimal
# Working directory: helix-desktop
npm test -- --run src/hooks/useAgentHealth.test.ts src/components/DeviceManualControl/DeviceManualControl.test.tsx src/shell/panels/ExperimentTimeline.test.tsx
~~~

### Phase Requirements → Test Map

New filters below are deliberately proposed test-class names, not existing values; all map entries require implementation. Commands assume the existing corresponding test project, opened during research. Keep quick unit/host tests under 30 seconds where practical; database cold-start/restart suites are separate slower integration gates. [ASSUMED: A10]

| Req ID | Behavior | Test Type | Proposed automated command / gate | Exists? |
|--------|----------|-----------|------------------------------------|---------|
| SAFE-01 | Anonymous/expired/revoked read/write/gRPC/upgrade denied and audited; wrong native scope denied | Host + native | Agent test filter AgentAuthenticationTests; native cargo test plus packaged smoke | Wave 0 |
| SAFE-01 | Manual, raw/compiled workflow, variable-read and alternate-executor paths cannot dispatch around gate; no real-device registration | Host/architecture | Agent test filter DispatchBoundaryTests, spy simulator count | Wave 0 |
| SAFE-02 | Each of six checks independently blocks effect; ordinary invalid rejects, uncertain unsafe state holds | Unit/host | Core test filter CommandAdmissionTests; Agent filter DispatchBoundaryTests | Wave 0 |
| SAFE-02 | State changes between compile/dispatch, lease expiry, competing reservations, budget exhaustion and abort fence cannot leak dispatch | Concurrency + PostgreSQL | Agent filter DispatchRaceTests; Infrastructure filter CommandLedgerTests | Wave 0 |
| SAFE-03 | Pause safe boundary, hold persists, abort terminal, resume never replays previous commands | Unit + database | Agent filter ExecutionControlTests and ReconciliationTests | Wave 0 |
| SAFE-03 | Crash before intent, after intent/before effect, after effect/before outcome, after outcome/before acknowledgement | Process/repository integration | PostgreSQL-backed ReconciliationTests with recreated host/simulator and injected barriers | Wave 0 |
| SAFE-03 | Matching simulator auto-resumes once; nonmatching/unreadable retains hold; independent holds/abort survive matching | Integration | Agent filter ReconciliationTests; native control smoke | Wave 0 |
| SAFE-04 | Intent/checks precede effect; actor/correlation/observation retained; duplicate identity idempotent; immutable evidence and annotation | Repository/host | Infrastructure filter CommandLedgerTests; Agent filter CommandEvidenceTests | Wave 0 |
| SAFE-04 | Completed/reconnected timeline loads canonical order, deduplicates events and emphasizes critical outcomes | Renderer | npm test -- --run after adding timeline/broker-client cases | Partial existing display tests only |

Example proposed commands using newly proposed filters; these are not runnable coverage claims until Wave 0 creates tests. [ASSUMED: A10]

~~~powershell
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter "FullyQualifiedName~AgentAuthenticationTests|FullyQualifiedName~DispatchBoundaryTests" --verbosity minimal
dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --verbosity minimal
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --verbosity minimal
dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --verbosity minimal
dotnet format --verify-no-changes
# Desktop renderer, working directory helix-desktop:
npm test -- --run
npm run build
# Native broker, working directory helix-desktop/src-tauri:
cargo test
cargo check
~~~

### Sampling Rate
- Per task commit: affected focused admission/host/renderer/native tests; the existing safety baseline alone is insufficient. [ASSUMED: A10]
- Per wave merge: affected managed/renderer/native suites and formatting/build checks; database waves require non-skipping real PostgreSQL tests. [ASSUMED: A10]
- Phase gate: all automated checks plus real listener/native authentication and process-restart reconciliation proof before verify-work. [ASSUMED: A10]

### Wave 0 Gaps
- Authentication bootstrap confirmation and native code-generation/build proof; establish a scoped credential-bearing transport seam before changing all desktop calls.
- Slim host factory that uses actual production middleware/routes/execution composition, with deterministic spy simulator and store/crash barriers.
- Admission truth table, canonical parameter fixtures, deterministic clock and lease/resource/budget/control race tests.
- PostgreSQL fixture that cannot silently pass without connection; apply migrations and recreate host/simulator for crash-window tests.
- Durable ledger/evidence/control repository tests, append-only/idempotency constraints and persistence-failure no-dispatch cases.
- Desktop broker-client mocks, rejected/held/aborted/recovery fixtures and completed/reconnect history tests.
- Packaged/native all-request smoke test and actual IPv4/IPv6 loopback listener verification; enumerate all exposed transports/routes.

All gaps are proposed work, not existing framework absence claims. [ASSUMED: A10]

## Security Domain

Security must be covered: project instructions and D-01–D-03 require execution authority and authenticated access. No explicit security-disable value was observed in the opened configuration; the workflow default therefore applies, but absence is not evidence of a deployed security feature. [VERIFIED: AGENTS.md:15-20] [VERIFIED: .planning/phases/01-agent-safety-boundary/01-CONTEXT.md:17-19]

### Applicable ASVS Categories

The official project page identifies ASVS 5.0.0 as the stable release published 30 May 2025. After failed guessed URLs, the official release tree was queried and the actual chapter files fetched successfully. **Use ASVS 5.0 numbering, not the older template's category numbers.** Chapter titles below are quoted verbatim from the version-pinned official sources; applicability and proposed controls remain the phase threat-model judgment. No ASVS certification/conformance claim is made. [CITED: https://owasp.org/www-project-application-security-verification-standard/] [CITED: https://api.github.com/repos/OWASP/ASVS/git/trees/v5.0.0?recursive=1] [ASSUMED: A11]

| ASVS 5.0 category (verbatim title) | Applies to phase | Recommended control |
|-------------------------------|------------------|---------------------|
| `V6 Authentication` [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x15-V6-Authentication.md] | Yes, every Agent read/write/stream | Validated expiring scientist desktop session; failed attempts audited. |
| `V7 Session Management` [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x16-V7-Session-Management.md] | Yes | Native-only credential storage, revocation, logout/expiry and reconnect identity validation. |
| `V8 Authorization` [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x17-V8-Authorization.md] | Yes | Default deny on all endpoints; scoped native IPC; Agent-only simulator mutation. |
| `V2 Validation and Business Logic` [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x11-V2-Validation-and-Business-Logic.md] | Yes | Typed bounded canonical parameters, read-only observation classification and six-check runtime admission. |
| `V11 Cryptography` [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x20-V11-Cryptography.md] | Yes, credential generation/storage | Maintained framework/OS primitives; no homegrown crypto or token logging. |
| `V4 API and Web Service` [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x13-V4-API-and-Web-Service.md] | Yes | All API/stream entry points share authentication and admission; validate protocol configuration. |
| `V13 Configuration` [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x22-V13-Configuration.md] | Yes | Exclusive loopback binding, no wildcard/config bypass, secret management and documented service dependencies. |
| `V16 Security Logging and Error Handling` [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x25-V16-Security-Logging-and-Error-Handling.md] | Yes | Append-only attributable evidence, bounded denied-attempt logging and no secret disclosure. |
| `V3 Web Frontend Security` [CITED: https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x12-V3-Web-Frontend-Security.md] | Yes, renderer webview; adapt browser-specific controls | Untrusted renderer, no credential return, scoped native commands and no remote-content privilege. |

Applicability/control recommendations are a phase threat-model proposal; version-pinned chapter names were fetched, but individual requirement compliance was not tested. RESOLVED planning disposition: use ASVS level 1 as security guidance, with the phase-unique STRIDE threat-to-task/test register and blocking high/critical acceptance gate. Individual-control compliance enumeration is not an additional Phase 1 deliverable; no certification or conformance claim is authorized. Any later compliance claim requires separate version-pinned control-ID selection and evidence. See [SKELETON.md](SKELETON.md#acceptance-and-handoff), [01-VALIDATION.md](01-VALIDATION.md#per-task-verification-map), and REQUIREMENTS.md Out of Scope. [ASSUMED: A11 — implementation/security evidence remains unverified]

### Known Threat Patterns for This Stack

| Pattern | STRIDE | Mitigation |
|---------|--------|------------|
| Arbitrary local client impersonates scientist | Spoofing | Validated native desktop session; loopback alone insufficient. |
| Renderer/native arbitrary-destination forwarding | Elevation of privilege | Fixed destination and typed IPC commands, trusted window scope, server-side admission. |
| Manual/raw-runspace/read capability bypass | Tampering / elevation | Single guarded dispatch port; read-only classification and simulator-only registration. |
| Lease/control/budget time-of-check race | Tampering | Transactional reservations, durable revisions and fenced final dispatch. |
| Crash/retry duplicates effect | Tampering | Stable identity, durable uncertainty hold, no replay, observed-state reconciliation. |
| Audit deletion or unattributed command | Repudiation | Persist-before-dispatch append-only facts, actor/correlation, annotation events. |
| Credential leak through logs/URLs/timeline | Information disclosure | Native-only secrets, redaction and verified actor references rather than tokens. |
| Failed-auth flooding / evidence store outage | Denial of service | Bounded attempts/logging and visible fail-closed degraded state; no unaudited dispatch. |

Threat classification and mitigations are proposed security reasoning. No penetration-test result or formal laboratory/regulatory assurance is claimed. [ASSUMED: A11]

## Assumptions Log

Every proposed design/unknown tagged ASSUMED above is grouped here. These require confirmation or implementation validation before being treated as locked decisions.

| # | Claim / decision group | Section | Risk if wrong |
|---|------------------------|---------|---------------|
| A1 | One dispatch authority, thin vertical slice, layer ownership and replacement/integration of legacy paths | Summary, Architecture, Requirement support | Bypass remains or work expands into unrelated executor rewrite. |
| A2 | Reuse declared dependencies without upgrades; defer unproven native additions behind official discovery/legitimacy/build gate | Standard Stack | Missing native dependencies or unearned package trust blocks execution. |
| A3 | Native credential-bearing broker, preference for existing native gRPC, OS-user-scoped pairing and expiring session | Authentication, Don't Hand-Roll | Scientist identity/bootstrap or native packaging does not satisfy intended trust boundary. |
| A4 | Exact admission data, freshness, bounds, budget/resource accounting and invalid/unknown disposition details | Admission matrix | Unsafe defaults or incompatible operator semantics. |
| A5 | Transactional reservations/fencing, idempotent command ledger and persist-before-effect/publication schema | Ledger, examples | Race, duplicate effect, loss of evidence or incorrect accounting. |
| A6 | Durable control contract/cursor, simulator persistence/postconditions and matching reconciliation semantics | Controls/recovery | Restart replays commands, false reconciliation or unwanted resume. |
| A7 | Durable sequence/cursor, manual/security streams and timeline composition/accessibility | Evidence/timeline | Missing, duplicated or misleading history. |
| A8 | Broad audit filename mapping requires planner source confirmation | Bypass audit | Edits target stale or mislocated files. |
| A9 | Installed runtime/data/config/secrets state remains unobserved; migration/provisioning actions required | Runtime inventory | Source changes leave deployed bypasses, legacy data or stale binaries. |
| A10 | Proposed new test filters, crash/host fixtures, acceptance commands and infrastructure gates | Environment/Validation | Green baseline masks missing safety/restart coverage. |
| A11 | ASVS applicability and stack threat-model mitigations; chapter numbering is cited, individual requirement selection/compliance remains untested | Security | Overstated security assurance or omitted applicable controls. |

## Open Questions — RESOLVED planning dispositions

All six questions have a selected contract or explicit execution precondition. RESOLVED means planning disposition, not a passed live test, proven compatibility, observed installation or compliance evidence. Earlier recommendations remain research alternatives superseded where specified below; assumptions retain their unverified evidence status.

1. **RESOLVED — Scientist identity/bootstrap:** Use the scientist's Windows SID, same account/elevation Agent and native desktop, CurrentUserOnly/FirstPipeInstance local named pipe with verified client identity/process locality; opaque 256-bit token, 15-minute expiry, maximum four sessions/SID, revocation/logout and startup revocation. No renderer-declared identity or per-command reapproval. Disclose malicious same-OS-user code as a residual limitation, not resistance claimed. Contract: [SKELETON transport/enrollment](SKELETON.md#transport-and-enrollment-decision-planner-discretion), Plan 01 tracer and session tests. Execution precondition: configured allowed scientist SID and same-user Windows launch; LocalSystem/different-user fails. Platform reference: https://learn.microsoft.com/en-us/dotnet/api/system.io.pipes.pipeoptions?view=net-10.0. [ASSUMED: A3 — native/session proof pending]
2. **RESOLVED — Native protocol build:** Select maintained Windows WinHTTP HTTP transport through native Rust FFI instead of the unproven generated gRPC client preference. No package changes; retain Cargo prost instruction. Fixed 127.0.0.1:5080, no proxy/redirect, bounded RAII handles and typed main-window IPC; existing C# gRPC stays authenticated compatibility only. Contract: [SKELETON validation](SKELETON.md#validation-strategy-and-known-limits), Plans 01/05/08. Execution precondition: Windows native/WebView2/toolchain available and real Tauri tracer passes before expansions; unavailable build fails, not fallback browser transport. Platform reference: https://learn.microsoft.com/en-us/windows/win32/winhttp/winhttp-sessions-overview. [ASSUMED: A2/A3 — build proof pending]
3. **RESOLVED — Policy defaults and observability:** Canonical operations are SetFlow/GetFlow, SetTemperature/GetTemperature, SetMode/GetMode. Plan 03 deliberately replaces legacy SetPumpMode registration/discovery with SetMode and adds both missing getters; no mutation alias. Names remain device-qualified only at the bridge. Preserve immutable historical legacy strings; old proposals receive actionable rejection. Scientist-approved defaults: flowRate 0–10 mL/min by 0.1; temperature 20–80 °C by 1; mode Isocratic/Gradient/Purge; one exclusive simulator resource, 30-second renewable lease, 100 mutations, 10-minute wall deadline, 2-second safety/health freshness and observation deadline. Exact canonical value/unit plus command/generation identity matches, no tolerance. Contract: [SKELETON execution/effect](SKELETON.md#execution-contracts-this-phase-produces), Plans 02/03/10/06/09. Execution precondition: approved frozen policy and fresh readable independent state; absence holds/rejects, never unlimited. [ASSUMED: A4/A6 — implementation pending]
4. **RESOLVED — Legacy runtime/data:** Do not assert installations or data absent. Preserve legacy evidence with Not recorded actor/correlation, never fabricated provenance. Inventory effective listeners, launched executable/service identity, relevant override/secret names without values; deploy same-user Windows profile and apply versioned 003 plus additive 004 to existing and new databases with checksum/runtime-grant gates. Contract: [SKELETON deployment/schema](SKELETON.md#deployment-and-schema), Plan 08. Execution precondition: actual runtime inventory and migration privileges; destructive migration is unauthorized and incompatible schema halts. [ASSUMED: A9 — installed state unobserved]
5. **RESOLVED — Verification infrastructure:** Require real disposable local PostgreSQL via PGHOST/PGPORT/PGDATABASE/PGUSER/PGPASSWORD or started Docker Desktop plus Windows native build/WebView2. Tracer starts red with actual main-webview IPC/Agent/SQL/effect/history and complementary fast native/admission tests; later crash gates recreate Agent/simulator over same database. Contract: Plans 01/03/09/08 and [01-VALIDATION.md](01-VALIDATION.md). Execution precondition: reachable migrated database and toolchain; missing setup, zero tests or skipped assertions fail acceptance. Original unavailable Docker observation remains unchanged; no new live proof is claimed. [ASSUMED: A10]
6. **RESOLVED — ASVS individual controls:** Adopt ASVS1 guidance and all applicable chapter-level mitigations through unique STRIDE threats and concrete task/test mappings; unresolved high/critical risks block acceptance. Individual-control certification/compliance-detail enumeration is outside the requested safety-boundary deliverable, consistent with REQUIREMENTS.md formal-claims exclusion, and is not required to execute these plans. No ASVS compliance verdict is asserted. A later intended-use compliance assessment must select exact ASVS 5.0 control IDs and gather evidence before claiming conformance. Contract: Security Domain applicability table, every PLAN threat_model, [01-VALIDATION.md](01-VALIDATION.md) threat refs. Official reference: https://owasp.org/www-project-application-security-verification-standard/. [ASSUMED: A11 — threat mitigations still require execution proof]

## Sources

### Repository / executed evidence
- Opened context, requirement definitions, roadmap, project, state, canonical research summary and codebase architecture/concerns/testing; phase constraints are sourced to the context excerpts above.
- Agent host, API/gRPC, capability bridge/registration, Core registry/compiler/safety/leasing/execution, runspace controls, simulator and evidence worker were inspected during this research. Broad observations have source navigation citations; exact enum/declaration claims include verbatim source quotes.
- Desktop renderer/native configuration, transport calls and timeline/tests inspected; manifest values quoted above.
- Focused managed safety and desktop health/manual/timeline tests executed successfully; full/native/database suites not claimed.
- Shell availability probes and Docker daemon error recorded in Environment Availability.

### Official documentation fetched (confidence seam: LOW for actual webfetch fallback)
- ASP.NET Core 10 authorization: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/secure-data?view=aspnetcore-10.0
- ASP.NET Core 10 CORS: https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0
- Kestrel endpoint configuration/protocols: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0
- Tauri capability and native command boundary: https://v2.tauri.app/security/capabilities/
- PostgreSQL explicit locking: https://www.postgresql.org/docs/current/explicit-locking.html
- OWASP ASVS project/release: https://owasp.org/www-project-application-security-verification-standard/
- OWASP ASVS 5.0 version-pinned chapters: official raw URLs in the Security Domain table; chapter headings and relevant sections fetched successfully after discovering filenames from the official release tree.
- Existing tonic interceptor API: https://docs.rs/tonic/latest/tonic/service/trait.Interceptor.html — request metadata hook, not scientist identity/bootstrap validation.

### Research Method / Limitations

The research-plan seam assigned curated documentation providers but Context7/ctx7 were unavailable; official pages were fetched using webfetch fallback, with a direct HTTP fetch of the official ASVS release tree/chapters to resolve incorrect guessed filenames. The classify-confidence seam returned LOW for webfetch both with and without verified, and LOW for curated with verified. This document does not promote those responses to HIGH because a source looks authoritative. External guidance is CITED and must be implementation-checked; repository quotes describe opened source definitions, not external compatibility guarantees. The initially failed ASVS lookups were superseded by successful chapter fetches, not interpreted as missing metadata. No registry freshness, new-package legitimacy, real-device safety or regulatory conformance is asserted. [VERIFIED: session research-plan/classify-confidence tool responses, 2026-10-29]

## Metadata

| Area | Confidence disposition | Reason |
|------|------------------------|--------|
| Standard stack | Source-grounded declarations; LOW external freshness | Opened manifests, no recommended upgrades/new installs; native generation unproven. |
| Architecture | LOW proposals; repository bypasses source-grounded | Locked authority/recovery goals constrain design, but bootstrap/coordination need checkpoints. |
| Pitfalls | Source-grounded local observations; LOW generalized guidance | Active composition and focused tests inspected; crash/native integration not run. |

**Research date:** 2026-10-29
**Review before execution:** Recheck session/bootstrap, native build and live database availability; retain source quotes as code contracts only while definitions remain unchanged. No fixed validity interval or compliance target is assumed.
