# Requirements: Helix Vision Mark II

**Defined:** 2026-10-29
**Core Value:** Reliably complete a traceable and safe scientific feedback loop from physical apparatus execution through sample collection and reviewed analytical evidence to the next validated experiment.

## v1 Requirements

Requirements for the initial Mark II release. Each maps to exactly one roadmap phase.

### Platform & Safety

- [ ] **SAFE-01**: The system dispatches device commands only through the local Helix Agent.
- [ ] **SAFE-02**: The Agent dispatches a command only after capability, safety, lease, resource, budget, and termination checks pass.
- [ ] **SAFE-03**: A scientist can pause, abort, or hold a campaign, and the Agent reconciles uncertain physical outcomes after restart.
- [ ] **SAFE-04**: The system records each command's intent, observation, correlation, and audit context.

### Apparatus & Procedures

- [ ] **APP-01**: A scientist can version an apparatus with typed components, ports, connections, roles, and device-instance bindings.
- [ ] **APP-02**: A scientist can associate capability constraints and calibration/inspection metadata with an apparatus device.
- [ ] **APP-03**: A scientist can version procedures and plans independently of apparatus topology.
- [ ] **APP-04**: The system freezes the apparatus, procedure, and policy revisions used by a campaign execution.

### Campaigns & Execution

- [ ] **CAMP-01**: A scientist can create and approve a bounded campaign policy with allowed operations, parameter limits, budgets, stop rules, and expiry.
- [ ] **CAMP-02**: The Agent durably runs campaign activities across UI closure and restart.
- [ ] **CAMP-03**: A scientist can view campaign state, approvals, commands, and outcomes in an evidence timeline.
- [ ] **CAMP-04**: The system can complete the initial end-to-end campaign proof using simulated devices.

### Samples & Analysis

- [ ] **SAMP-01**: A scientist can confirm actual sample collection separately from a planned activity.
- [ ] **SAMP-02**: The system records stable sample identities, custody, transformations, and parent/child lineage.
- [ ] **ANLY-01**: A scientist can create an analytical test request linked to a sample and method revision.
- [ ] **ANLY-02**: A scientist can preview and import structured CSV results with validation, sample matching, units, and duplicate detection.
- [ ] **ANLY-03**: The system quarantines invalid or ambiguous analytical data and prevents it from advancing a campaign.
- [ ] **ANLY-04**: A scientist can review immutable analytical-result revisions, and only accepted evidence is eligible for decisions.
- [ ] **ANLY-05**: The system preserves source artifacts, hashes, parser/mapping versions, and analytical-result provenance.

### Adaptive Science & Experience

- [ ] **AUTO-01**: The Agent selects a next simulated experiment only from accepted evidence and within the approved campaign policy.
- [ ] **AUTO-02**: An autonomous loop enforces bounded iterations, quality and escalation rules, and holds on uncertainty.
- [ ] **UX-01**: A scientist can use setup, experiment, plan, run, results, and samples/analysis views to understand current status and provenance.
- [ ] **EVID-01**: A scientist can export a self-contained campaign evidence bundle and reconstruct its history.

## v2 Requirements

### Hardware, Collaboration & Integration

- **HARD-01**: A qualified real device can be integrated after EHS, vendor, safety, and recovery validation.
- **COLL-01**: Multiple users/sites can coordinate campaigns through SiteServer/federation.
- **INTG-01**: Helix can integrate with selected LIMS/CDS systems beyond the CSV path.
- **DATA-01**: Helix can accept validated quantitative results from formats beyond structured CSV.
- **AUTO-03**: Helix can expand optimization methods beyond bounded, campaign-policy-controlled loops.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Mandatory SiteServer/federation dependency | A local fumehood campaign must continue when central or network services are unavailable. |
| Full LIMS, CDS, ELN, or inventory replacement | The first release focuses on the scientific feedback loop and integrates only where needed. |
| Arbitrary PDF measurement extraction | Uncontrolled parsing cannot be trusted in the initial decision path. |
| Broad device and laboratory-standard coverage | Simulated-device proof and a qualified hardware gate come first. |
| Formal regulatory/e-signature claims | Applicability and quality controls require separate intended-use assessment. |
| Open-ended autonomous optimization | Autonomy must remain bounded by an approved policy, deterministic validation, and escalation rules. |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| SAFE-01 | Phase 1 | Pending |
| SAFE-02 | Phase 1 | Pending |
| SAFE-03 | Phase 1 | Pending |
| SAFE-04 | Phase 1 | Pending |
| APP-01 | Phase 2 | Pending |
| APP-02 | Phase 2 | Pending |
| APP-03 | Phase 2 | Pending |
| APP-04 | Phase 2 | Pending |
| SAMP-01 | Phase 2 | Pending |
| SAMP-02 | Phase 2 | Pending |
| CAMP-01 | Phase 3 | Pending |
| CAMP-02 | Phase 3 | Pending |
| CAMP-03 | Phase 3 | Pending |
| ANLY-01 | Phase 4 | Pending |
| ANLY-02 | Phase 4 | Pending |
| ANLY-03 | Phase 4 | Pending |
| ANLY-04 | Phase 4 | Pending |
| ANLY-05 | Phase 4 | Pending |
| CAMP-04 | Phase 5 | Pending |
| AUTO-01 | Phase 5 | Pending |
| AUTO-02 | Phase 5 | Pending |
| UX-01 | Phase 6 | Pending |
| EVID-01 | Phase 6 | Pending |

**Coverage:**
- v1 requirements: 23 total
- Mapped to phases: 23
- Unmapped: 0 ✓

---
*Requirements defined: 2026-10-29*
*Last updated: 2026-10-29 after roadmap creation*
