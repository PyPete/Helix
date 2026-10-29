# Roadmap: Helix Vision Mark II

## Overview

Deliver one local-first, simulated scientific feedback loop in which the Helix Agent remains the only execution authority: a scientist configures revisioned apparatus and procedures, runs an approved bounded campaign, confirms samples, reviews CSV evidence, and allows a policy-constrained next simulated experiment. Real hardware remains out of scope for execution until a documented qualification gate is satisfied.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

- [ ] **Phase 1: Agent Safety Boundary** - Make local Agent-only simulation execution safe, attributable, and recoverable.
- [ ] **Phase 2: Scientific Setup Foundation** - Create revisioned apparatus, procedures, samples, and frozen execution bindings.
- [ ] **Phase 3: Approved Manual Campaigns** - Run approved, bounded simulated campaigns durably with reconciliation and evidence visibility.
- [ ] **Phase 4: Reviewed CSV Evidence** - Turn structured CSV imports into reviewed, immutable analytical evidence.
- [ ] **Phase 5: Policy-Bounded Autonomous Simulation** - Use accepted evidence to select and execute only permitted next simulated experiments.
- [ ] **Phase 6: Evidence Readiness & Hardware Gate** - Export reconstructable evidence and make hardware rollout a qualification decision, not execution scope.

## Phase Details

### Phase 1: Agent Safety Boundary
**Goal**: Scientists can rely on the local Helix Agent as the sole, safety-gated authority for simulated device commands and recovery.
**Mode:** mvp
**Depends on**: Nothing (first phase)
**Requirements**: SAFE-01, SAFE-02, SAFE-03, SAFE-04
**Success Criteria** (what must be TRUE):
  1. A scientist's simulated device action is dispatched only by the local Helix Agent, never directly by a UI, Site Server, or proposal source.
  2. A command is refused before dispatch when its capability, safety, lease, resource, budget, or termination check fails.
  3. A scientist can pause, abort, or hold an active campaign, and after an Agent restart can see uncertain simulated outcomes reconciled rather than blindly replayed.
  4. A scientist can inspect an attributable record of each command's intent, observation, correlation, and audit context.
**Plans**: TBD

### Phase 2: Scientific Setup Foundation
**Goal**: Scientists can define the revisioned scientific setup and sample facts that a campaign will freeze as its evidence base.
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: APP-01, APP-02, APP-03, APP-04, SAMP-01, SAMP-02
**Success Criteria** (what must be TRUE):
  1. A scientist can create and version an apparatus with typed components, ports, connections, roles, device bindings, capability constraints, and calibration or inspection metadata.
  2. A scientist can create and version procedures and experiment plans independently from apparatus topology.
  3. Before a campaign runs, the scientist can identify the immutable apparatus, procedure, and policy revisions frozen for that execution.
  4. A scientist can confirm an actual sample collection independently of a planned step and trace each sample's stable identity, custody, transformations, and parent/child lineage.
**Plans**: TBD
**UI hint**: yes

### Phase 3: Approved Manual Campaigns
**Goal**: Scientists can approve and durably operate a bounded simulated campaign through the Agent before autonomous selection is enabled.
**Mode:** mvp
**Depends on**: Phase 2
**Requirements**: CAMP-01, CAMP-02, CAMP-03
**Success Criteria** (what must be TRUE):
  1. A scientist can create and approve a campaign policy with explicit allowed operations, parameter limits, budgets, stop rules, and expiry.
  2. An approved simulated campaign continues as durable Agent work when the UI closes or the Agent restarts, subject to the safety boundary and reconciliation behavior.
  3. A scientist can view the campaign's current state, approval, commands, and outcomes in an evidence timeline.
**Plans**: TBD
**UI hint**: yes

### Phase 4: Reviewed CSV Evidence
**Goal**: Scientists can convert controlled CSV analytical data into reviewed, provenance-rich evidence that is safe to consider for a campaign.
**Mode:** mvp
**Depends on**: Phase 3
**Requirements**: ANLY-01, ANLY-02, ANLY-03, ANLY-04, ANLY-05
**Success Criteria** (what must be TRUE):
  1. A scientist can create an analytical test request linked to a collected sample and a method revision.
  2. A scientist can preview a structured CSV import, match it to samples, validate units and duplicates, and import only understood results.
  3. Invalid or ambiguous imported data is quarantined and cannot advance a campaign.
  4. A scientist can review immutable analytical-result revisions, with only accepted revisions eligible for downstream decisions.
  5. A scientist can inspect preserved source CSV artifacts, hashes, parser and mapping versions, and result provenance.
**Plans**: TBD
**UI hint**: yes

### Phase 5: Policy-Bounded Autonomous Simulation
**Goal**: Scientists can observe a bounded autonomous simulated campaign select the next valid experiment solely from accepted evidence and an approved policy.
**Mode:** mvp
**Depends on**: Phase 4
**Requirements**: CAMP-04, AUTO-01, AUTO-02
**Success Criteria** (what must be TRUE):
  1. The initial end-to-end campaign proof completes with simulated devices: approved setup and policy, execution, confirmed sample, reviewed CSV evidence, and a subsequent simulated experiment.
  2. The Agent selects a next simulated experiment only from accepted analytical evidence and within the approved campaign policy.
  3. The autonomous loop enforces its iteration bounds, quality and escalation rules, and places the campaign on hold when evidence or execution is uncertain.
**Plans**: TBD

### Phase 6: Evidence Readiness & Hardware Gate
**Goal**: Scientists can reconstruct the simulated campaign record locally and see that any real-hardware rollout is blocked behind explicit qualification rather than execution.
**Mode:** mvp
**Depends on**: Phase 5
**Requirements**: UX-01, EVID-01
**Success Criteria** (what must be TRUE):
  1. A scientist can use coherent setup, experiment, plan, run, results, and samples/analysis views to understand the campaign's status and provenance.
  2. A scientist can export a self-contained campaign evidence bundle and reconstruct the campaign history from it.
  3. A scientist can review a hardware qualification gate covering EHS, vendor, safety, recovery, and device validation; it authorizes no real-hardware command execution in this MVP.
**Plans**: TBD
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Agent Safety Boundary | 0/TBD | Not started | - |
| 2. Scientific Setup Foundation | 0/TBD | Not started | - |
| 3. Approved Manual Campaigns | 0/TBD | Not started | - |
| 4. Reviewed CSV Evidence | 0/TBD | Not started | - |
| 5. Policy-Bounded Autonomous Simulation | 0/TBD | Not started | - |
| 6. Evidence Readiness & Hardware Gate | 0/TBD | Not started | - |
