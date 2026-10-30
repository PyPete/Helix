# Project Research Summary

**Project:** Helix Vision Mark II  
**Domain:** Local-first, safety-bounded scientific workstation for a single fumehood  
**Researched:** 2026-10-29  
**Confidence:** MEDIUM

## Executive Summary

Helix Vision Mark II should be built as an evidence-first local laboratory control plane, not as a general LIMS, remote robot service, or open-ended AI agent. The fumehood-local Helix Agent is the sole authority for drivers and physical effects; the desktop, dashboard, Site Server, scripts, and future AI all submit attributed proposals and render derived state. A campaign is the durable unit of work: frozen apparatus/procedure/policy revisions, explicit approval envelope and budgets, confirmed samples, reviewed analytical evidence, and a bounded next-experiment decision.

Extend the existing .NET 10, PostgreSQL, gRPC/protobuf, MQTT, React/Tauri foundation. Use PostgreSQL for authoritative revisions, lifecycle/effect records, approvals, and an outbox; use S3-compatible storage for immutable CSV/artifact bytes; and treat MQTT as telemetry/notification rather than execution truth. Start with a fully simulated vertical slice and reviewed CSV import. Add only one narrow autonomous loop after the deterministic policy gate, reconciliation, evidence trail, and stop/hold behavior are proven.

The dominant risks are unsafe or duplicated physical work, scientifically unusable mutable provenance, and untrusted imports or AI gaining actuator authority. Mitigate them with independent physical interlocks/E-stop, fail-closed admission checks, durable command intent/observation/reconciliation records, immutable revisioned evidence, quarantine-plus-review CSV promotion, and least-privilege capability adapters. Never claim regulated compliance or automate real hardware until EHS, device, recovery, and quality requirements have been specifically validated.

## Key Findings

### Recommended Stack

Retain the existing C#/.NET 10 modular hosts rather than introducing a second execution runtime. Keep the Agent durable and local, isolate device specifics behind typed capability adapters, and introduce new dependencies only where they support the MVP evidence loop. Pin currently floating package versions and align mixed .NET 9/10 extension packages in a dependency-hardening task.

**Core technologies:**
- **C#/.NET 10 with ASP.NET Core hosted services:** Agent-local control boundary and cancellable/restart-aware campaign work — it matches the mature existing hosts, DI, workflow, device, and test seams.
- **PostgreSQL with Npgsql/Dapper:** authoritative operational and scientific records — transactions enforce idempotency, revisions, policy transitions, and the outbox.
- **S3-compatible object storage / MinIO:** immutable original CSVs and large artifacts — retain bytes, SHA-256, parser metadata, and object references outside relational rows.
- **gRPC/protobuf:** versioned typed command/read contracts — preserve compatibility and stable IDs; do not route raw CSV/artifacts through RPC.
- **MQTT/MQTTnet:** volatile trends and notifications — version topics and delivery policy, but never use it as the command log or scientific record.
- **Cleipnir.ResilientFunctions + PostgreSQL:** restart-aware waits and orchestration — retain it, while explicit domain records remain the audit model.
- **React/TypeScript/Vite and Tauri 2:** local operator UI — submit intent and display Agent state; neither owns safety nor drivers.
- **CsvHelper, UCUM/SI quantities, System.Text.Json source generation:** controlled CSV parsing, canonical units, and versioned DTO/payload handling — phase-pin exact versions and reject ambiguous values.
- **OPC UA client / serial, Modbus, TCP adapters:** supported device transports — keep all behind Agent-only capability adapters with health, abort/safe-stop, and reconciliation contracts.

### Expected Features

**Must have (table stakes):**
- Stable local identifiers, append-only provenance, immutable workflow/apparatus/policy snapshots, and a searchable campaign evidence timeline.
- Apparatus registry/setup binding with declared capabilities, constraints, calibration/inspection references, and a frozen campaign binding.
- Structured versioned workflows; sample, aliquot, and material lineage; and confirmed collection distinct from planned steps.
- Durable campaign lifecycle, explicit approval envelope, visible pause/abort, and safe restart/reconciliation behavior.
- Raw-preserving CSV import: versioned profile, validation/quarantine, preview/matching/units, scientist review/disposition, and immutable accepted result revisions.
- Result annotations plus self-contained evidence-bundle export (records, snapshots, artifacts, hashes, event log).

**Should have (differentiators):**
- Evidence-first apparatus-binding diff before execution and typed capability-only adapters.
- An executable campaign contract: deterministic allow-list, parameter bounds, budgets, stop rules, and expiry enforced independently of a planner.
- Review-first result promotion and a provenance replay view generated from the same facts used for export.
- One safe bounded feedback loop that selects only an allowed next experiment from accepted observations.

**Defer (v2+):**
- Multi-user/cloud synchronization, federation, generalized ELN/LIMS/inventory, formal e-signature/Part 11 claims, and broad integrations.
- Universal device control, full SiLA/Allotrope/AnIML implementation, arbitrary PDF ingestion, automatic data cleaning, and rich collaboration/dashboard scope.
- Open-ended/self-modifying or fully unattended optimization; a planner may propose but cannot expand its authority.

### Architecture Approach

Use a modular monolith per deployable, centered on the fumehood-local Agent. PostgreSQL contains the durable facts; object storage preserves source artifacts; transactional outbox and idempotent consumers disseminate derived events. UI read models and MQTT trends are rebuildable projections. Separate desired commands from observed hardware outcomes and reconcile uncertainty rather than retrying non-idempotent effects.

**Major components:**
1. **Apparatus/capability registry and procedure compiler** — version topology, device roles/envelopes, and compile plans against frozen bindings.
2. **Campaign policy/coordinator and safety admission** — own lifecycle, approvals, budgets/termination, deterministic pre-dispatch validation, and proposal selection.
3. **Agent execution kernel/effect ledger** — acquire fenced resources, record intent, dispatch admitted commands, observe outcomes, and reconcile restart uncertainty.
4. **Device capability adapters** — normalize simulator/vendor protocols into narrow typed capabilities, health/state, safe stop, idempotency class, and reconciliation queries.
5. **Samples, analysis, and provenance modules** — immutable lineage, artifact quarantine/review, revisioned results, decisions, approvals, and evidence edges.
6. **Integration/projections** — PostgreSQL outbox/inbox, gRPC command/read APIs, MQTT observation publication, and rebuildable views.

**Key patterns:** policy-as-data plus repeated admission; durable state machines with `Reconciling`/`Held` as first-class outcomes; command/observation effect ledger; immutable W3C-PROV-style entities/activities/agents; and simulator parity/conformance tests for every adapter.

### Critical Pitfalls

1. **Treating software as the safety system** — retain independent hardware interlocks and physical E-stop; fail closed on stale telemetry, unknown state, safety trip, or lost heartbeat; require human release.
2. **Retrying non-idempotent physical work** — persist command ID, intent, acceptance, observation, pre/postconditions, and reconciliation procedure; replay only adapter-proven idempotent commands.
3. **Mutable/detached provenance** — retain immutable snapshots, hashes, source artifacts, lineage edges, software/configuration versions, and attributable review/approval events; test cold reconstruction.
4. **Letting CSV, UI, or AI select actuator calls** — treat all as untrusted proposals; schema-validate a typed plan and apply campaign/device/operation/budget/expiry-scoped deterministic policy before Agent dispatch.
5. **Optimizing measurement artifacts rather than science** — version analyses, represent quality/uncertainty, use controls/replicates/stop rules, quarantine anomalies, and require review before search expansion.

## Implications for Roadmap

Based on the dependency chain, build one simulated end-to-end evidence loop in six vertical phases rather than separately building a generic LIMS or autonomy platform.

### Phase 1: Platform Hardening and Safety Boundary
**Rationale:** No downstream evidence or campaign is credible while control APIs, package versions, in-memory ownership, and recorder loss semantics are unresolved.
**Delivers:** Agent-only driver reachability; authenticated/local-private control boundary; dependency/version pinning; durable fencing/ownership direction; simulator baseline; safety-state vocabulary; command identity and audit correlation conventions.
**Addresses:** Local workspace identity, pause/abort foundation, one physical authority.
**Avoids:** UI-to-driver shortcuts, software-only safety, untrusted tool authority, and unsafe generic retries.

### Phase 2: Scientific Evidence and Apparatus Foundation
**Rationale:** Frozen apparatus/workflow/policy snapshots and sample facts must exist before approving or executing a campaign.
**Delivers:** Versioned apparatus registry/capability manifest, procedure/plan revisions, canonical quantity handling, stable IDs, append-only provenance ledger, sample/lineage facts, object-artifact store, and searchable timeline projection.
**Addresses:** Apparatus binding, structured workflows, samples/lineage, provenance/navigation.
**Avoids:** Mutable-current-state provenance, free-text units, and evidence detached from configuration.

### Phase 3: Approved Manual Campaign and Reconciliation
**Rationale:** Prove the same constrained execution path manually before an optimizer can select iterations.
**Delivers:** Campaign lifecycle and frozen approval envelope; deterministic admission at selection/compile/dispatch; fenced resource reservation; effect ledger (`intent → observation`); pause/abort/hold; durable state machine and operator reconciliation/deviation flow using simulated devices.
**Addresses:** Durable campaign execution, approval/controls, reviewable command/run record.
**Avoids:** automatic restart/replay, policy-only-at-start validation, and broker-as-execution-truth.

### Phase 4: Reviewed CSV Analytical Evidence Loop
**Rationale:** Autonomous decisions require trusted, reviewable observations; raw CSV must never directly become campaign input.
**Delivers:** CSV artifact upload/checksum/storage, versioned import profiles, streaming validation/quarantine, sample/request matching, explicit preview/disposition, immutable result/calculation revisions, and outbox publication of accepted evidence.
**Addresses:** CSV ingestion, result review/annotations, accepted observations, evidence export prerequisites.
**Avoids:** parser/locale/unit ambiguity, silent repair, unreviewed results steering execution, and raw-file detachment.

### Phase 5: One Policy-Bounded Autonomous Simulated Campaign
**Rationale:** The first autonomy proof must consume only accepted evidence and use the already-proven manual executor.
**Delivers:** Deterministic campaign-policy artifact (allow-list, numeric bounds, budgets, termination/quality/escalation rules); constrained proposal interface; accepted-result-driven next-iteration selection; simulator/dry-run; controls/replicates/quality flags; hold/review on uncertainty.
**Addresses:** Executable campaign contract and safe bounded feedback loop.
**Avoids:** LLM/optimizer authority expansion, artifact optimization, unbounded cost/material/time use, and unsafe recovery.

### Phase 6: Evidence Bundle, Operational Readiness, and Hardware Qualification Gate
**Rationale:** Demonstrate independent reconstruction and operational safety before extending beyond simulation.
**Delivers:** Portable campaign evidence bundle, reconstruction/export tests, backup/restore drills, telemetry-loss policy, alert/handoff UX, adapter conformance suite, and a written EHS/vendor/site qualification gate for one real device.
**Addresses:** Portable local evidence and credible local-first operation.
**Avoids:** local-first mistaken for offline-safe, alert fatigue, unsupported regulatory claims, and uncertified adapter rollout.

### Phase Ordering Rationale

- Phase 1 establishes the non-bypassable authority and reliability baseline; Phase 2 creates the frozen records that a campaign must reference.
- Phase 3 validates physical-effect semantics with simulation before CSV-derived results or autonomous proposals can influence execution.
- Phase 4 makes evidence admissible; Phase 5 can therefore consume only reviewed, immutable observations through an existing policy gate.
- Phase 6 proves that the record survives outside the workstation and makes real hardware a deliberate qualification decision, not a feature toggle.

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 1:** inventory current Agent authentication, leases/fencing, durable workflow persistence, recorder backpressure, and exact local PostgreSQL/absent-Site-Server topology.
- **Phase 3:** inspect existing adapter contracts for per-command reconciliation, safe-stop, identity, and idempotency; define recovery and human-deviation UX with the scientist.
- **Phase 4:** validate assay-specific CSV schemas, sample identifiers, units, uncertainty/replicate rules, retention/encryption/backup expectations, and CsvHelper version/configuration.
- **Phase 5:** select and evaluate the proposal method (rules, statistical optimizer, or LLM); specify campaign-specific scientific controls, objective, uncertainty, and expansion criteria.
- **Phase 6:** mandatory EHS/site and vendor discovery for hazards, interlocks, E-stop, firmware semantics, calibration, facility procedure, and regulatory applicability before any actuator integration.

Phases with standard patterns (skip research-phase unless codebase inspection changes scope):
- **Phase 2:** relational immutable revisions, content-addressed artifact storage, append-only events, UCUM quantities, and searchable projections are established patterns.
- **Outbox/projection portions of Phases 3–4:** transactional outbox/inbox and idempotent read-model consumers are well-established; focus planning on Helix-specific contracts rather than rediscovering the pattern.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Strong repository evidence confirms the existing .NET, PostgreSQL, gRPC, MQTT, React/Tauri, simulator, and device seams; exact new package versions need phase pinning. |
| Features | MEDIUM | Consistent with FAIR/ELN, SiLA, and autonomy practice, but exact laboratory procedure and data needs remain unknown. |
| Architecture | MEDIUM | Primary standards strongly support provenance/security boundaries; existing persistence and adapter capabilities need direct code reconciliation. |
| Pitfalls | LOW | Controls are safety-critical and sources authoritative, but research classifier confidence was low and each must be validated with EHS, vendors, and quality owners. |

**Overall confidence:** MEDIUM

### Gaps to Address

- **Exact physical environment:** obtain documented hazard analysis, hardware interlocks/E-stop behavior, approved safe states, operating procedures, and accountable owner approval before real-device work.
- **Adapter and recovery truth:** test command correlation, cancellation, status, reboot/reconnect, and firmware-specific semantics on each supported device; do not infer them from APIs.
- **Local durability topology:** decide and test Agent-local PostgreSQL/object-storage/backup behavior when Site Server or network is absent; define explicit offline modes and conflict behavior.
- **Scientific data contract:** define initial assay CSV templates, controlled vocabularies, unit conversions, quality flags, uncertainty/replicate/controls, and result-to-policy eligibility.
- **Security and quality scope:** threat-model Agent control APIs, credentials, imported artifacts, AI proposals, emergency override, and stale leases; obtain a quality/regulatory intended-use assessment before any compliance statement.

## Sources

### Primary (HIGH confidence)
- `.planning/PROJECT.md` and repository/codebase mapping — existing architecture, constraints, simulated-device proof scope, and current risks.
- W3C PROV-DM — entity/activity/agent provenance model and revision/derivation relationships: https://www.w3.org/TR/prov-dm/
- OPC UA Part 2 Security Model — authorization, integrity, auditability, availability, replay and resource-exhaustion concerns: https://reference.opcfoundation.org/Core/Part2/v105/docs/4
- NIST SP 800-82 Rev. 3 — operational-technology security guidance: https://csrc.nist.gov/pubs/sp/800/82/r3/final
- PostgreSQL documentation; OASIS MQTT 5.0; UCUM; NIST SI units — durability, messaging, and units foundations.

### Secondary (MEDIUM confidence)
- FAIR Guiding Principles — identifiers, rich metadata, and detailed provenance: https://www.nature.com/articles/sdata201618
- SiLA and Allotrope documentation — capability-oriented devices and contextual analytical data: https://sila-standard.com/standards ; https://docs.allotrope.org/Allotrope%20Data%20Format.html
- ADePT autonomous-laboratory framework and ELN practice sources — staged human-supervised autonomy, workflow and record expectations.

### Tertiary (LOW confidence; validate before implementation)
- FDA data-integrity / Part 11 guidance, NIST AI RMF, and OWASP GenAI guidance — safety, data-integrity, and AI security controls; applicability depends on intended use and local quality process.

---
*Research completed: 2026-10-29*  
*Ready for roadmap: yes*
