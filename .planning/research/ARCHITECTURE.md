# Architecture Patterns

**Domain:** Local-first autonomous laboratory workstation with agent-mediated physical execution  
**Researched:** 2026-10-29  
**Confidence:** MEDIUM — recommendations are grounded in primary standards and the stated Helix context; implementation details require reconciliation with the existing C# codebase.

## Recommended Architecture

Build Mark II as a **local control-plane system with a single physical authority**. The fumehood-local Helix Agent owns all driver connections, leases, safety interlocks, command dispatch, device-state reconciliation, and durable execution. Desktop/dashboard clients and Site Server are clients of that authority: they submit signed/attributed *intent*, observe state, and review/approve scientific decisions; they never issue device-driver calls or cause physical side effects directly.

Use a modular monolith per deployable initially (Agent and Site Server), with explicit domain modules and durable integration boundaries—not premature microservices. PostgreSQL is the durable source of operational/scientific record; MQTT/gRPC are transports and live-observation channels, not a source of truth. The Agent must continue running a safe campaign and record its evidence when UI, Site Server, or network disappear.

```text
Desktop / Dashboard / optional Site Server
       |  intent commands + read models (gRPC/HTTP); observations (MQTT)
       v
┌────────────────── Fumehood-local Helix Agent ──────────────────┐
│ API boundary: authenticate, authorize, schema/units validation  │
│ Campaign coordinator + durable run state machine                │
│ Safety admission: capability, policy, lease, resource, budget   │
│ Procedure compiler / executor / reconciliation                  │
│ Apparatus registry | Samples & lineage | Analysis & review       │
│ Provenance/audit ledger | transactional outbox | projections     │
│                  Device capability adapters                     │
└───────────────────────┬────────────────────────────────────────┘
                        v
        simulated or physical instruments / PLCs / drivers
```

### Component Boundaries

| Component | Responsibility | Communicates With |
|---|---|---|
| Apparatus & capability registry | Versioned apparatus topology, ports, device-instance binding, roles, declared operating envelopes, capability descriptors | Procedure compiler, safety admission, UI read model |
| Procedure/plan module | Immutable authored procedure and experiment-plan revisions; compiles a bound plan against a specific apparatus revision | Campaign coordinator, Agent executor |
| Campaign policy & coordinator | Owns campaign lifecycle, iteration selection, budget/termination evaluation, approval gate, wait states and decision provenance | Executor, samples, analysis/review, policy evaluator |
| Agent execution kernel | Durable run/step state machine; reserves resources; dispatches only admitted commands; observes and reconciles device state after restart | Safety boundary, adapters, event journal |
| Safety admission boundary | Deterministic validation immediately before every physical side effect: identity, capability, parameter envelope, interlock, lease, resource, campaign budget and stop condition | Execution kernel; never bypassed by UI, Site Server, or AI |
| Device adapter layer | Normalizes vendor/simulator protocols into typed capabilities, reported state, commands, cancel/hold and reconciliation queries | Execution kernel and hardware only |
| Samples & lineage | Records *confirmed* collection, split/merge/transform/custody/location as immutable facts; distinguishes plans from material reality | Executor, analysis requests, provenance |
| Analysis ingest & review | Stores source artifact immutably, parses CSV into quarantine, matches against test request/sample, validates units/schema, creates revisioned results and approvals | Samples, campaign coordinator, UI |
| Provenance/audit module | Append-only attributable record of commands, observations, approvals, decisions, inputs/outputs and software/configuration versions | All write modules; query projections |
| Integration & projections | Transactional outbox, inbox/deduplication, gRPC command/read APIs, MQTT observation publication, rebuildable read models | External clients/Site Server |

### Data Flow

1. A scientist creates versioned apparatus, procedure, experiment plan and campaign policy. Approval freezes the exact revisions and explicit bounds for a campaign iteration; later editing creates a new revision, not a mutation.
2. The coordinator asks the policy for a **proposed** next experiment. It validates declared parameter ranges, information/budget limits, termination conditions, available capability/resource requirements, and required human approval. AI/optimizer output remains proposal data and is never executable authority.
3. The executor compiles the selected plan against the frozen apparatus/capability snapshot. At each physical step, it acquires a fenced lease, performs admission checks, records an intent/command identity durably, then invokes the adapter. It persists observed acknowledgement/state and emits domain events through an outbox.
4. On uncertain outcome, crash, timeout, or restart, execution moves to `Reconciling`, queries actual device state and operator-required checks, then explicitly resolves to resume, compensate/hold, complete, or fail. It does **not** replay a non-idempotent physical command merely because the prior acknowledgement was not written.
5. Sample collection is a human/device-confirmed event, producing a stable sample identity and lineage edge. Analysis imports preserve the original CSV and parser/validation revision. Only a reviewed/approved result revision becomes eligible evidence for calculations and policy selection.
6. UI projections and MQTT trends are derived from recorded facts. They may lag/rebuild; they must not decide whether a device command executed.

## Patterns to Follow

### Pattern 1: Command/observation split with a physical-effect ledger
**What:** Represent desired actions as commands and actual hardware feedback as observations. Give every physical attempt a stable `CommandId`, idempotency key, lease/fencing token, planned parameter snapshot, timestamps, adapter correlation ID, and final reconciliation disposition.

**When:** Every driver action, including stop/hold and sample-affecting actions.

**Why:** A database transaction cannot atomically include a physical device. This record makes the “unknown outcome” explicit and auditable instead of fabricating exactly-once execution.

```csharp
// Agent-only internal application service; UI can submit a proposal, never this call.
await admission.ValidateAsync(command, capabilitySnapshot, policy, lease, cancellationToken);
await effects.RecordIntentAsync(command);       // durable before effect
var reply = await adapter.ExecuteAsync(command, lease.FencingToken, cancellationToken);
await effects.RecordObservationAsync(command.Id, reply);
```

### Pattern 2: Durable state machines with reconciliation, not replay
**What:** Model campaigns, runs, and steps as explicit persisted states: `Draft → Approved → Queued → Running → WaitingForEvidence → Reconciling → Completed|Failed|Held|Cancelled`. Persist transitions and the reason/actor; make each transition conditional on expected version.

**When:** Long-running procedures, result waits, restart recovery, and adaptive loops.

**Implementation:** Treat an in-flight physical operation after restart as `Unknown`, query device state/correlation evidence, and require operator resolution where state cannot be proved. Use retries only for operations whose adapter contract says they are idempotent under the same command ID.

### Pattern 3: Policy-as-data plus deterministic admission
**What:** Store campaign policy as a versioned, reviewed artifact: allowed procedures/apparatus versions, device capabilities, parameter domains, materials/resources, max iterations/time/cost/volume/energy, stop rules, evidence quality threshold, and escalation conditions. Evaluate it deterministically in the Agent both at campaign selection and immediately before each command.

**When:** Any bounded autonomy; especially optimizer/LLM-assisted selection.

**Instead of:** Encoding limits only in a UI wizard or trusting an autonomous planner. The policy snapshot is provenance for why an iteration was permitted.

### Pattern 4: Transactional outbox + idempotent inbox
**What:** Write domain state, immutable audit/provenance events, and an outbox record in one PostgreSQL transaction. A dispatcher publishes after commit; receivers store message ID/version before processing and make handlers idempotent. Consumers can rebuild projections from durable facts.

**When:** Publishing run status, sample/result availability, campaign decisions and Site Server synchronization.

**Caveat:** This gives at-least-once delivery across the broker boundary, not exactly-once physical execution. Keep physical dispatch inside the Agent effect ledger and reconciliation workflow.

### Pattern 5: Immutable scientific provenance graph
**What:** Use stable IDs and append-only revisions for scientific entities. Map Helix concepts to W3C PROV-style relationships: samples, CSV artifacts, result revisions, plan/policy/configuration snapshots are **entities**; execution, collection, import, validation, calculation and review are **activities**; scientist, Agent, instrument and software version are responsible **agents**. Record `used`, `wasGeneratedBy`, `wasDerivedFrom`, and association/approval roles.

**When:** Across every decision that can affect scientific interpretation or a physical action.

**Implementation:** Keep relational tables optimized for query plus a provenance-edge table; do not make a graph database a prerequisite. Hash original artifacts and immutable snapshots; retain correction as a superseding revision with a reason, never overwrite a result or lineage fact.

### Pattern 6: Capability adapters as an anti-corruption layer
**What:** Expose typed, narrowly scoped device capabilities (`TemperatureControl`, `MeteredAddition`, `Agitation`, `CollectSample`, `ReadState`, `EmergencyHold`) rather than a generic “send command” interface. Each adapter declares constraints, safe states, idempotency/reconciliation support, telemetry semantics, and simulator parity.

**When:** Integrating real and simulated instruments.

**Why:** It stops protocol/vendor concepts from leaking into procedures and gives safety admission a stable vocabulary. OPC UA’s emphasis on authentication, authorization, integrity, auditability and availability supports the same boundary principles; use secure, least-privilege adapter credentials and rate/connection limits where OPC UA is used.

## Anti-Patterns to Avoid

### Anti-Pattern 1: UI/API-to-driver shortcut
**What:** Desktop, dashboard, Site Server, script or LLM directly calls a driver or publishes a device command topic.

**Why bad:** It bypasses the one place that can atomically apply policy, lease, capability and safety checks; it also destroys a complete physical-effect audit.

**Instead:** Restrict driver credentials/network reachability to the Agent process. Other processes submit attributed proposals/commands to the Agent API and receive status.

### Anti-Pattern 2: “Retry until success” on physical commands
**What:** Generic background retry treats an acknowledgement timeout as a failed action.

**Why bad:** A valve may have opened or an aliquot may already have been dispensed; duplicate action can cause hazardous or scientifically invalid outcomes.

**Instead:** Reconcile actual state by command/device correlation; use adapter-specific idempotency, then stop/hold and escalate unknown outcomes.

### Anti-Pattern 3: Mutable current-state as provenance
**What:** Update a sample/result/plan row in place and call it history.

**Why bad:** It cannot answer what evidence, limits, configuration and person/software authorized a decision at the time.

**Instead:** Immutable revisions and directed provenance edges; build a convenient current-state projection separately.

### Anti-Pattern 4: Broker/event stream as execution truth
**What:** Let MQTT retained messages, UI state, or an eventually consistent consumer decide run completion or safety state.

**Why bad:** Streams can be duplicated, delayed, lost or reordered; telemetry is observational, not a transaction coordinator.

**Instead:** Agent-owned durable state and effect ledger decide execution; MQTT is for trend/observation fan-out.

### Anti-Pattern 5: Campaign policy evaluated only once
**What:** Validate a campaign when it starts, then let generated iterations execute unchecked.

**Why bad:** Device availability, leases, consumed resources, stop conditions and policy budgets can change between planning and action.

**Instead:** Revalidate at selection, at compilation/binding, and directly before every physical side effect.

## Scalability Considerations

| Concern | At 100 users / one fumehood | At 10K users / many hoods | At 1M users / fleet scale |
|---|---|---|---|
| Physical execution | One Agent serializes conflicting resources with local leases and Postgres durability | One Agent per safety zone; Site Server routes intent, never commands hardware | Regional/fleet control planes; retain autonomous local cells and independent safety domains |
| Events/telemetry | Transactional outbox + bounded MQTT retention; store selected time series | Partition streams by site/device; archive raw telemetry separately from execution ledger | Dedicated streaming/time-series platform and tiered retention; never couple control loop to analytics throughput |
| Provenance/query | PostgreSQL tables + indexes by sample/run/result and revision | Partition by site/time; materialized read models and artifact object storage | Federated immutable archives, signed export bundles, graph/warehouse projection for cross-site analysis |
| Recovery | Reconcile a small device set synchronously at Agent boot | Per-device reconciliation queues with safety-first hold policy | Fleet health/supervision, but local Agent remains able to hold/recover when disconnected |

## Source-Grounded Design Notes

- W3C PROV-DM defines provenance around entities, activities and responsible agents, including derivation, attribution, association, plans and named provenance bundles. This directly supports separating planned procedure/policy revisions from executed activity and generated sample/result revisions. **Confidence: HIGH (primary standard).**
- OPC UA Part 2 (v1.05.06) identifies authorization, integrity, auditability and availability as security objectives; it specifically calls out message replay and resource exhaustion. Helix should therefore use least-privilege adapter identities, command correlation/replay resistance, bounded queues/leases, and an audit trail even where drivers do not use OPC UA. **Confidence: HIGH (primary standard, applicable by analogy).**
- The transactional outbox/inbox approach is a reliability pattern rather than a solution to atomic physical effects. The design recommendation to reconcile non-idempotent operations follows directly from the project’s restart requirement and should be verified against each existing adapter contract. **Confidence: MEDIUM.**

## Sources

- [W3C PROV-DM: The PROV Data Model](https://www.w3.org/TR/prov-dm/) — Recommendation, 2013; primary source for entities, activities, agents, derivation, plans, and provenance bundles. **HIGH**
- [OPC UA Part 2: Security Model, v1.05.06](https://reference.opcfoundation.org/Core/Part2/v105/docs/4) — current reference; security objectives, replay/resource-exhaustion threats, sessions, auditing and defence in depth. **HIGH**
- [NIST SP 800-82 Rev. 3, Guide to Operational Technology Security](https://csrc.nist.gov/pubs/sp/800/82/r3/final) — OT security and safety boundary guidance. **HIGH** (identified for phase-specific threat modeling; PDF extraction was unavailable in this research runtime.)
- [FDA, Data Integrity and Compliance With Drug CGMP: Questions and Answers](https://www.fda.gov/media/97005/download) — authoritative laboratory data-integrity guidance. **HIGH** (identified for audit/revision requirements; PDF extraction was unavailable in this research runtime.)

## Open Architecture Questions for Phase Planning

1. Inventory existing Agent workflow persistence and adapter contracts before introducing a new campaign state machine; determine whether leases already carry fencing tokens and whether adapters can query/reconcile individual commands.
2. Define the exact Agent-local persistence/replication topology when Site Server is absent: is PostgreSQL local, remotely reachable but cacheable, or must the Agent have a local durable store/outbox? This is a critical availability decision.
3. Specify domain unit/quantity library, uncertainty/quality flags, controlled vocabularies and CSV schemas before calculations can influence campaign policy.
4. Threat-model emergency stop/hold semantics, human override, stale lease fencing, device clock/correlation capabilities, and what operations require two-person or explicit scientist approval before real-hardware rollout.
