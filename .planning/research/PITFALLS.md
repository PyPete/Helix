# Domain Pitfalls

**Domain:** Local-first autonomous laboratory workstation (Helix Vision Mark II)  
**Researched:** 2026-10-29  
**Evidence confidence:** LOW — sources are authoritative, but the configured confidence classifier rates the web-fetch provider LOW; validate the controls with the lab's EHS officer, instrument vendors, and applicable quality/regulatory owner.

## Critical Pitfalls

### Pitfall 1: Treating software control as a safety system
**What goes wrong:** An agent, queue worker, UI action, or retry starts or continues a physical operation after a hazardous state, a lost connection, or a human stop request. A purely software “E-stop” may be unavailable precisely when the workstation is partitioned or hung.

**Why it happens:** Device APIs expose imperative commands, which makes an apparently successful call look like a safe completed action. Physical state, interlocks, consumables, environmental limits, and operator presence are outside that API call.

**Consequences:** Injury, spill/exposure, damaged hardware or samples, and an unrecoverable discrepancy between the record and the bench.

**Prevention:** Keep independent hardware safety interlocks and a physical emergency stop outside Helix's control plane. Model every protocol step as a state transition guarded by device health, interlock status, calibrated operating envelope, sample compatibility, and explicit authorization. The executor must fail closed: loss of heartbeats, uncertain completion, out-of-range telemetry, or an emergency stop moves the run to `blocked`, not `retry`. Require a human release after any safety trip; never let an LLM waive a guard.

**Detection:** Continuously record heartbeat/interlock/telemetry freshness; alarm on stale telemetry and command/state disagreement. Exercise power-loss, unplug, and E-stop drills on a non-production rig before enabling a device class.

### Pitfall 2: Retrying non-idempotent physical work
**What goes wrong:** A timeout leads Helix to repeat an action that may already have occurred: dispense twice, move a plate twice, heat longer, or overwrite an instrument export.

**Why it happens:** Network success only means a response arrived; it does not prove whether the device accepted or completed the command. Generic job-queue retry semantics are unsafe for physical side effects.

**Consequences:** Invalid experiments, damage/contamination, and false provenance.

**Prevention:** Give every executable action a durable command ID, declared idempotency class, expected pre/postconditions, and compensating/reconciliation procedure. Persist `intent → accepted → observed` separately. Only replay actions proven idempotent by the device adapter; for all others query authoritative device state or require an operator disposition. Do not infer completion from elapsed time.

**Detection:** Reconciliation scans flag commands without a matching observation, duplicate command IDs, impossible state transitions, and measurements whose timestamps precede their claimed step.

### Pitfall 3: Mutable or incomplete provenance makes results scientifically unusable
**What goes wrong:** Results are linked only to a friendly sample name or current protocol. Later edits to sample metadata, run parameters, calibration, scripts, reagent lots, or result files erase the ability to reproduce or defend a conclusion.

**Why it happens:** LIMS-like labels are mistaken for immutable identities; imported CSV/PDF outputs are treated as facts before their origin and schema are established.

**Consequences:** Sample mix-ups, irreproducible optimization, unreliable analytical comparisons, and failure of review/audit obligations.

**Prevention:** Use immutable IDs and append-only event records for samples, aliquots, containers, transfers, protocol revisions, device/adapter versions, operator/agent identity, instrument configuration, calibration status, raw artifact hash, parser version, and review decision. Preserve originals in content-addressed storage; derived results must cite the exact raw artifact and transformation. Present a lineage graph, never a silently editable “current truth.” FDA guidance expects reliable, accurate records and risk-based controls; its Part 11 guidance emphasizes secure/reliable records, authorized access, device/operational checks, and preserving content and meaning during copying/retention.

**Detection:** Make a run non-reviewable when any lineage edge, raw hash, clock source, unit, or protocol revision is missing. Periodically attempt a cold reconstruction of a result from immutable artifacts.

### Pitfall 4: Letting imported data or an agent directly choose actuator calls
**What goes wrong:** Malformed exports, vendor notes, filenames, OCR text, or an LLM prompt cause tool selection or parameters that exceed the intended campaign. This includes prompt injection, parser confusion, unit manipulation, and exfiltration through connected tools.

**Why it happens:** “Agent-only execution” is misread as “agent has broad execution privileges.” Natural-language plans and untrusted analytical files cross the same boundary as physical actuation.

**Consequences:** Unauthorized device operation, data disclosure, unsafe recipes, and unbounded cost/time/sample consumption.

**Prevention:** Make the agent a proposer, not a privileged actuator. It emits a typed, schema-validated plan into a deterministic policy gate; only the executor can invoke allow-listed adapter capabilities. Bind capability tokens to one campaign, device, operation, parameter range, budget, and expiry. Treat every file, instrument response, and model output as untrusted data; isolate parsers and deny network/shell/file-system tools by default. Require a human approval tier for novel protocols, high-energy/high-risk operations, material transfers, and policy exceptions. NIST AI RMF supports continuous Govern/Map/Measure/Manage risk handling; OWASP specifically maintains guidance for agentic/LLM application security.

**Detection:** Log the complete plan, policy decision, tool arguments, identity, and artifact hashes; alert on denied/near-limit requests, capability reuse, unrecognized schemas, and attempts to invoke undeclared tools.

### Pitfall 5: Autonomous campaigns optimize an artifact rather than science
**What goes wrong:** The optimizer exploits a noisy assay, a parser default, a stale calibration, selection bias, or a proxy metric; it confidently proposes more runs in the wrong region.

**Why it happens:** Closed-loop optimization assumes measurements are comparable and uncertainty is represented. Laboratory drift, censored outcomes, failed runs, batch effects, and limited material violate those assumptions.

**Consequences:** Wasted samples and instrument time, misleading “best” conditions, and an apparently well-documented but false result.

**Prevention:** Start campaigns with declared objective, units, acceptance criteria, bounded parameter domains, hard safety/material/time budgets, controls, replicates, randomization/blocking rules, and stop conditions. Version and validate the analysis pipeline separately from the agent. Feed the optimizer quality flags and uncertainty; quarantine outliers for review instead of silently dropping them. Require a review gate before expanding the search region or promoting a candidate conclusion.

**Detection:** Dashboard drift/control charts, replicate disagreement, missingness by treatment, optimizer boundary-hugging, and a holdout confirmation run all trigger pause/review.

## Moderate Pitfalls

### Pitfall 1: Assuming vendor integrations are transactional
**What goes wrong:** Drivers differ in units, coordinate frames, status vocabulary, authentication, export formats, firmware behavior, and whether a command can be cancelled. A generic adapter hides these differences until a run fails.

**Prevention:** Define an explicit adapter contract: capability discovery, unit-bearing typed parameters, command correlation, state/health vocabulary, cancellation semantics, raw evidence capture, and simulator/conformance tests. Certify each adapter/device/firmware combination; support read-only import before actuation.

### Pitfall 2: Failure recovery resumes from the wrong reality
**What goes wrong:** After a crash, power event, manual intervention, or device restart, the scheduler resumes from its database state although a plate/sample/device has changed in the real world.

**Prevention:** Use durable checkpoints only at verified physical boundaries. On restart, require reconciliation against device state and barcode/vision/operator attestation where physical identity matters. Make “unknown” a first-class terminal state with a deviation workflow, never an implicit retry.

### Pitfall 3: Audit logs that can be edited, deleted, or detached from data
**What goes wrong:** Standard application logs are rotated or mutable, timestamps vary by workstation, and approvals are represented as a checkbox rather than attributable action.

**Prevention:** Use append-only, access-controlled audit events with synchronized time, actor/service identity, reason-for-change, before/after values, and links to immutable artifacts. Separate audit-log write privileges from normal administration; test export, retention, and human-readable reconstruction. FDA Part 11 guidance notes that audit trails and other controls should be selected through documented risk assessment, particularly where users create/modify/delete records.

### Pitfall 4: Local-first is confused with offline-safe
**What goes wrong:** A local machine runs while disconnected but cannot verify identity, resolve approvals, access keys, back up raw data, or reconcile concurrent edits. Later sync overwrites provenance.

**Prevention:** Define offline modes explicitly: observation-only, pre-authorized execution within a signed local envelope, or blocked. Use local durable storage and encrypted backups, monotonic event IDs, conflict detection (not last-write-wins) and a visible “not yet synchronized” state. Restore drills must include raw artifacts and audit records, not only the database.

### Pitfall 5: Dangerous UX hides uncertainty and irreversible actions
**What goes wrong:** Operators see a polished “running” state although telemetry is stale, do not distinguish planned vs executed vs observed, or approve a batch without seeing material, device, and risk impact.

**Prevention:** Render safety/reconciliation state prominently; show the exact next physical action, units, sample/container lineage, device target, and remaining campaign budget. Use deliberate confirmation for irreversible actions and an immediately visible stop/hold control. Never rely on color alone; preserve an operator timeline and handoff checklist.

### Pitfall 6: Scope prematurely claims regulated compliance
**What goes wrong:** Product messaging or design assumes “Part 11 compliant” because it has logins and logs, while intended use, predicate rules, validation evidence, signatures, supplier controls, retention, and procedures are unresolved.

**Prevention:** Describe Mark II as “designed to support integrity controls,” not compliant, until a quality owner performs an intended-use and applicability assessment. Build traceable requirements, risk assessment, test evidence, change control, access training, and signed-review workflows from the first regulated deployment. FDA states Part 11 remains in effect and stresses risk-based validation based on effect on quality, safety, and record integrity.

## Minor Pitfalls

### Pitfall 1: Unit, time, and locale ambiguity
**What goes wrong:** °C/K, µL/mL, local time/UTC, decimal commas, or plate orientation are silently coerced.

**Prevention:** Store canonical SI values plus displayed unit, timezone-aware instants, typed quantities, and explicit coordinate/plate conventions. Reject ambiguous import values; test locale and daylight-saving boundaries.

### Pitfall 2: Unreviewed parser and model changes alter historical interpretation
**What goes wrong:** A parser update reinterprets raw vendor files, or a new model changes campaign proposals, without a comparable record.

**Prevention:** Version parsers/models/prompts/policies; pin them per run, retain test fixtures, and require differential review before release. Reprocessing creates a new derived-result version rather than overwriting the prior one.

### Pitfall 3: Alert fatigue turns safety alarms into background noise
**What goes wrong:** Every transient warning pages the operator; eventually meaningful interlock or provenance alerts are ignored.

**Prevention:** Tier alerts by hazard and required response, deduplicate correlated symptoms, make ownership/escalation explicit, and review false-positive rates after pilot runs.

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|---|---|---|
| Safety foundation | Software-only interlocks and unsafe retry | Establish hardware/physical controls, executor state machine, safety envelope, and fail-closed semantics before live actuation. |
| Device adapters | “Universal” abstraction erases device semantics | Ship one certified adapter + simulator + conformance tests; add capabilities incrementally. |
| Provenance/results | Editable records and detached raw files | Implement immutable event lineage, hashes, original-artifact retention, import quarantine, and reviewer disposition. |
| Recovery | Restart automatically continues an unknown physical state | Implement durable intent/observation records, reconciliation UI, and operator deviation workflow. |
| Agent boundary | Agent or imported content gains direct tool authority | Typed plans, deterministic policy gate, least-privilege expiring capabilities, and full decision logging. |
| Autonomous campaigns | Optimizer expands scope or overfits noisy results | Parameter/budget bounds, controls/replicates, quality flags, stop rules, and human expansion gate. |
| Local-first operations | Offline/sync silently loses integrity | Signed offline envelopes, encrypted backup/restore drills, conflict surfacing, and sync status UX. |
| Quality/regulatory deployment | Compliance claim lacks validation evidence | Intended-use assessment, risk-based validation plan, access/signature/audit controls, and quality-owner signoff. |

## Sources

- [FDA — Data Integrity and Compliance With Drug CGMP: Q&A (2018)](https://www.fda.gov/regulatory-information/search-fda-guidance-documents/data-integrity-and-compliance-drug-cgmp-questions-and-answers-guidance-industry) — **LOW classifier confidence**; official guidance says data must be reliable/accurate and advocates risk-based integrity strategies.
- [FDA — 21 CFR Part 11: Scope and Application (current page 2018; guidance issued 2003)](https://www.fda.gov/regulatory-information/search-fda-guidance-documents/part-11-electronic-records-electronic-signatures-scope-and-application) — **LOW classifier confidence**; primary source on electronic records, authorized access, checks, audit trails, risk-based validation, and preserving records.
- [NIST — AI Risk Management Framework](https://www.nist.gov/itl/ai-risk-management-framework) — **LOW classifier confidence**; primary framework for trustworthy-AI risk management; page notes ongoing revision, so phase work must recheck current material.
- [OWASP GenAI Security / LLM Top 10](https://genai.owasp.org/llm-top-10/) — **LOW classifier confidence**; community security guidance for LLM and agentic systems.

## Research Gaps Requiring Phase Discovery

- EHS requirements, hazard classes, required physical interlocks, and emergency procedures depend on the exact instruments and facility; obtain written owner approval before actuator integration.
- Vendor command/recovery semantics and raw-data formats must be tested on each model/firmware, not inferred from marketing/API documentation.
- Whether FDA/GLP/GxP/Part 11 rules apply depends on Helix's intended use and user workflows; get a quality/regulatory applicability assessment.
- Campaign statistical methods, controls, and acceptable uncertainty depend on each scientific assay and should be specified per campaign template.
