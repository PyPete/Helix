# Helix Vision Mark II

## What This Is

Helix Vision Mark II is a local-first scientific workstation for a scientist operating a fumehood. It connects a live digital representation of laboratory apparatus with device capabilities, durable experiment execution, samples and analytical results, so a scientist can progress safely from experiment design to adaptive scientific campaigns.

The desktop UI helps construct and observe the workflow, while the local Helix Agent remains the durable authority for physical execution, safety, and recovery.

## Core Value

Reliably complete a traceable and safe scientific feedback loop from physical apparatus execution through sample collection and reviewed analytical evidence to the next validated experiment.

## Requirements

### Validated

- ✓ The platform has separate desktop/dashboard clients, a Site Server, and a fumehood-local Agent execution host — existing codebase.
- ✓ The Agent provides capability-based device execution, leasing/safety abstractions, runspaces, and workflow compilation — existing codebase.
- ✓ Simulated instruments and a simulation Agent proxy support development without physical hardware — existing codebase.
- ✓ Campaign, trend-streaming, PostgreSQL persistence, MQTT, and gRPC integration foundations exist — existing codebase.

### Active

- [ ] Scientists can compose and save a physical apparatus model with typed components, ports, device-instance bindings, roles, and operating constraints.
- [ ] Scientists can author procedures and experiment plans that remain distinct from apparatus topology and can be run through the local Agent.
- [ ] The Agent can execute durable, bounded adaptive campaigns with explicit budgets, termination criteria, capability checks, leases, resource checks, and restart reconciliation.
- [ ] Helix records confirmed sample collection, parent/child transformations, custody, and lineage separately from planned workflow steps.
- [ ] Scientists can create analytical test requests, import structured CSV analytical data, preview/match/validate it, and review immutable result revisions.
- [ ] Approved analytical evidence can safely drive calculations and the next experiment selected by an autonomous campaign policy.
- [ ] The setup, experiment, plan, run, results, and samples/analysis experiences present coherent execution status, provenance, and live observations.

### Out of Scope

- Mandatory SiteServer or federation dependency — local fumehood execution must continue when central/network services are unavailable.
- Full LIMS, CDS, ELN, or inventory replacement — integrate only where it supports the scientific feedback loop.
- Arbitrary PDF extraction as a quantitative-result source — initial analytical ingestion is controlled CSV with explicit validation and review.
- Unsupervised hazardous procedure approval — physical execution remains constrained by campaign policy, deterministic safety checks, and Agent authority.
- Broad external analytical-system integrations in the first slice — CSV import and review prove the ingestion model first.

## Context

Helix is an existing distributed C# laboratory-orchestration system. The codebase contains `Helix.Orchestration.Agent`, `Helix.Orchestration.SiteServer`, Core domain/workflow abstractions, device and infrastructure adapters, simulation support, recorder services, React desktop/dashboard clients, PostgreSQL durability, MQTT trend streaming, and protobuf/gRPC contracts.

The Mark II direction extends this foundation with first-class scientific activities, samples, analytical test requests/results, provenance, and adaptive campaign decisions. The primary initial user is one scientist at one local fumehood workstation. The first compelling end-to-end demonstration uses simulated devices: configure apparatus, execute a campaign, confirm and trace a sample, import/review a CSV result, then safely select and execute the next experiment.

## Constraints

- **Execution authority**: The local Helix Agent alone may address drivers or execute physical commands — UI and LLM outputs are untrusted proposals, not device authority.
- **Safety**: Every device command must pass deterministic capability, safety, lease, resource, budget, and termination validation — autonomous campaigns are bounded by an approved policy.
- **Autonomy**: A scientist approves the campaign policy up front — the Agent may execute validated iterations within that policy without approving every run.
- **Reliability**: Campaign waits, external-result correlation, and decisions must survive UI closure and Agent restart — recovery reconciles physical state rather than blindly replaying commands.
- **Scientific integrity**: Samples, results, calculations, decisions, and approvals require stable identities, immutable revisions, provenance, units/quality validation, and idempotent processing.
- **Hardware rollout**: Simulated devices are the initial execution target — real-device integration follows a proven end-to-end slice.
- **Analysis MVP**: Structured CSV import plus review is the first analytical-result path — preserve the source artifact and quarantine ambiguous or invalid results.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Plan the full Mark II vision as staged vertical slices | All major capabilities must converge on one scientific feedback loop | — Pending |
| Target one local fumehood scientist first | Prove dependable local value before lab-wide/federated coordination | — Pending |
| Use simulated devices first | Validate orchestration, safety, sample, and analytical workflows before hardware risk | — Pending |
| Start analytical ingestion with reviewed CSV import | Provides controlled, testable provenance without coupling the first slice to a LIMS/CDS | — Pending |
| Permit bounded autonomous loops after policy approval | Enables adaptive experimentation while retaining explicit scientific and safety limits | — Pending |
| Keep the Agent as the sole physical execution authority | Prevents UI or AI from bypassing deterministic safety and capability validation | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-10-29 after initialization*
