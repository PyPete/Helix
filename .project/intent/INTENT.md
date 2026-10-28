# Intent — durable-local-campaign-skeleton

<!-- Written by gsd-path-define. Every downstream agent reads this first. -->

Lane: milestone

Review panel: off

Finding skeptics: off

Surfaces: local helix-desktop campaign workspace, Agent external-evidence ingress endpoint

## Summary

This first program milestone establishes the smallest local campaign slice for a scientist to start one bounded simulated activity from the Helix desktop. It extends the existing Agent, PostgreSQL, and Cleipnir path rather than introducing a second orchestration engine. The desktop must show the durable campaign and activity identity, lifecycle, audit history, and pending external-evidence state, while the Agent remains the only authority for commands. The slice proves restart recovery and a correlated, deduplicated completion signal without claiming automatic control or completion of an offline instrument.

## Problem

Helix has Agent execution, simulation, device capability, lease, safety, and partial campaign foundations, but no demonstrated durable, campaign-centred local workflow that can survive a desktop closure or Agent restart while awaiting external evidence. A scientist needs a trustworthy minimal path that makes authority, state, and recovery visible without duplicate physical commands or retained leases.

## Users

Scientists operating a local fumehood workstation use the local `helix-desktop` application as the primary surface. They need an auditable, recoverable path for one bounded simulated activity; the web dashboard is not a delivery surface for this milestone.

## Success criteria

1. In the local `helix-desktop` campaign workspace, a scientist starts the bounded simulated activity and observes its durable campaign/activity identity, pending state, and Agent audit history.
2. The Agent external-evidence ingress endpoint accepts one signal correlated to that pending activity and rejects or safely deduplicates a repeated signal without claiming that an offline instrument was controlled or completed automatically.
3. After the desktop is closed or the Agent restarts during the wait, the campaign workspace restores the pending or completed state; recovery neither repeats the physical command nor leaves a device lease held, and any later command follows renewed lease, capability, and safety checks.
4. The delivered documentation describes the supported local execution, ingress, recovery, and authority boundary and removes or labels conflicting historic claims.

## Edge coverage

| Edge | Criterion | Category | Disposition | Detail |
|------|-----------|----------|-------------|--------|
| E1 | SC1 | empty | held-out | Recommended: a missing/unknown campaign or activity identity must not create a second activity; a test must prove the request is rejected or resolved to the existing durable identity. |
| E2 | SC1 | ordering | held-out | Recommended: lifecycle/audit updates arriving after the activity is already pending must not regress the displayed state. |
| E3 | SC2 | idempotency | criterion SC2 | A repeated correlated completion signal is rejected or safely deduplicated and must not create a second completion or physical-command claim. |
| E4 | SC2 | adjacency | held-out | Recommended: a signal whose correlation differs by one identifier component is rejected and does not complete the pending activity. |
| E5 | SC3 | concurrency | criterion SC3 | Desktop closure or Agent restart during the external wait restores durable state without repeating the command or retaining a lease. |
| E6 | SC3 | ordering | held-out | Recommended: a completion signal that arrives during restart/recovery is reconciled once and does not leave an ambiguous second command. |
| E7 | SC4 | none | dismissed | Documentation criterion has no runtime data-shape edge; it is verified by the delivered documentation and contract checks. |

## Prohibitions

| Prohibition | Criterion | Must not | Disposition | Detail |
|-------------|-----------|----------|-------------|--------|
| N1 | SC1 | present the desktop state as proof that physical work completed | judgment | Review the campaign wording and audit trail: state remains a record of the bounded simulated activity, not proof of unobserved physical completion. |
| N2 | SC2 | claim control or automatic completion of an offline instrument from ingress | criterion SC2 | The endpoint contract and response/audit history state correlation and deduplication only; no command authority is implied. |
| N3 | SC3 | bypass Agent lease, capability, validation, or safety checks after recovery | criterion SC3 | Any later command must reacquire/revalidate through the Agent path. |
| N4 | SC4 | leave conflicting current documentation as supported behavior | criterion SC4 | Delivered documents either match the supported runtime contract or clearly label historic claims. |

## Scope: in

- A campaign-centred local desktop slice for one bounded simulated activity, its durable identity, lifecycle, command/audit history, and pending external-evidence state.
- Stable campaign, activity, flow, and command-deduplication identities in the existing Agent/PostgreSQL/Cleipnir execution path.
- A correlated external-evidence wait, restart/closure recovery, one completion signal, explicit uncertain physical outcomes, and revalidated subsequent commands.
- The Agent-only capability, lease, validation, safety, and audit boundary for the demonstrator.
- Documentation aligned with the shipped local execution, ingress, recovery, and authority contract.

## Scope: out (vetoes)

- Apparatus configuration or visual editing, sample collection/accessions, analytical-result revisions, calculations, decisions, proposals, and plan revisions — delivered by later milestones.
- General activity types, campaign-history replay, broad result-source support, and desktop redesign beyond the demonstrator — delivered by later milestones.
- Browser/dashboard work, direct AI or remote-driver device control, and any SiteServer, internet, LIMS, or external-analysis requirement for authorised local work.
- Treating durable records, the desktop display, or an ingress signal as proof of offline physical completion or device ownership.

## Constraints

- Build on the existing .NET Agent, PostgreSQL/Cleipnir durable-execution baseline, capability/lease/safety path, and local `helix-desktop`; avoid speculative rewrites.
- Pair usable desktop work with core functionality; the local desktop is primary and the web dashboard is parked.
- Device commands execute only through the Agent capability, lease, validation, safety, and audit path.
- Current runtime/code is authoritative for documentation by default; update documentation unless approved scope intentionally changes code.

## Current state (brownfield only)

- **What exists**: Helix is a .NET 10 multi-project system with ASP.NET Core hosts, gRPC, PostgreSQL/Npgsql/Dapper, and a PostgreSQL-backed Cleipnir function store. `helix-desktop` is a separate React/Tauri application; the Agent currently also contains explicitly in-memory lease and runtime services.
- **Must not break**: Existing Agent capability, lease, validation, safety, audit, simulation, and local desktop shell/connection/status surfaces remain infrastructure for this slice.
- **Doc-vs-code rulings**: Existing recorded user ruling is “Fix docs by default”; for audit queue rows 3, 17, 19, 21, 23–28, 30, 36, and 37, use `fix-doc` unless a later approved milestone intentionally changes the runtime contract.
- **Ground truth**: `.project/research/evidence-codebase.md`, `.project/research/DOCS-AUDIT.md`

## Risks

- Existing Cleipnir integration, in-memory runspace state, and in-memory leases may not provide the required end-to-end restart semantics.
- Durable correlation, deduplication, ordering, and explicit uncertain-physical-outcome handling may require a missing Agent contract.

## Open questions

- [RESEARCH] What durable ingress correlation, deduplication, ordering, and uncertain-physical-outcome contract governs the demonstrator and later analytical-result use?

## Corrections

- 2026-10-27: "Local desktop is still primary surface. Web dashboard, is parked for future."
- 2026-10-27: "Fix docs by default"
