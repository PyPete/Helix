# Plan — Helix durable-local-campaign-skeleton

<!-- Written by the planner role. Executed wave-by-wave by gsd-path-build. -->

Project verify: `dotnet restore Helix.Orchestration.slnx && dotnet build Helix.Orchestration.slnx --no-restore && dotnet test Helix.Orchestration.slnx --no-build && npm install --prefix helix-desktop && npm test --prefix helix-desktop -- --run && npm run build --prefix helix-desktop && npm install --prefix docs-site && npm run build --prefix docs-site`

## Config

- max_review_cycles: 3
- wave_budget: none
- review_panel: off
- finding_skeptics: off

## Wave 1 — durable-state and recovery risk burn-down

Goal: Prove the durable pending-activity and external-evidence recovery contract before UI delivery. T002 supplies the landed E6 implementation/test prerequisite; T020 owns the plan-level SC3/E6 recovery and post-wait Agent proof.
Review depth: deep

## Wave 2 — local campaign walking skeleton

Goal: Deliver the usable local desktop path for starting one bounded activity and viewing its durable monotonic projection and Agent audit, without making the desktop a device-command authority.
Review depth: full

## Wave 3 — supported-contract documentation and accepted audit repairs

Goal: Publish the bounded local execution contract and correct each accepted historical documentation claim against the checked-in runtime, with no runtime scope expansion.
Review depth: verify-only

## Wave 4 — fix wave 1 cycle 1 review findings

Goal: Retain the landed repairs and their historical review lineage for T001/T002.
Review depth: deep

## Wave 5 — fix wave 1 cycle 2 review findings

Goal: Retain the landed T002 route-contract repair and its historical review lineage.
Review depth: deep

## Wave 6 — Agent lease and post-recovery command proof

Goal: Prove SC3/N3's Agent lease release and later-command revalidation using an actually supported Agent path. If no supported path exists, stop before route implementation and obtain one precise orchestrator answer; do not waive SC3/N3 or claim proof.
Review depth: deep

## Surface contract

### local helix-desktop campaign workspace — T003

Criteria: SC1
Entry: Open the Campaign workspace in `helix-desktop` with a configured Agent URL and select the start affordance for the one bounded simulated activity.
States: Empty shows the start affordance and no invented activity; loading shows that the durable Agent projection is being retrieved; error reports that the projection could not be loaded or that an identity is missing/unknown without fabricating state; success shows campaign/activity/flow identities, a revisioned pending-or-completed lifecycle, physical outcome `unknown`/`uncertain` where applicable, and Agent audit history. Closing and reopening restores that same durable projection. The desktop remains projection-only and issues no direct device command; T020 separately owns Agent lease and later-command proof for SC3/N3.
Walkthrough:
1. Start the bounded simulated activity and record the displayed durable campaign/activity IDs, pending external-evidence state, physical-outcome uncertainty, and Agent audit entry (SC1/T003).
2. Close and reopen the workspace while pending or after completion; verify the same durable projection is restored and an older response cannot regress its displayed lifecycle/audit. Verify the desktop makes no direct device command; the composed SC3 Agent lease/revalidation proof is owned by T020.
3. Read the supported local-campaign documentation and verify that it describes shipped local execution, ingress/deduplication, recovery, and Agent-only authority without claiming offline physical completion or control (SC4/T004).

### Agent external-evidence ingress endpoint — T002

Criteria: SC2
Entry: POST the documented Agent ingress route with the complete campaign/activity/flow-or-awaiting-step tuple, expected pending revision, `source`, `eventId`, and payload for the pending activity.
States: Loading shows that the ingress HTTP request is in flight; error returns a non-transition disposition for an unknown, partial, one-component near-match, changed-payload, stale, or non-pending signal; success returns the persisted accepted disposition and updated projection, or the stored disposition for an identical replay, with no second transition, physical-command claim, or offline-instrument control/completion claim.
Walkthrough:
1. Submit one event whose complete tuple and expected revision match the pending activity and inspect the accepted transition and audit disposition (SC2/T002).
2. Repeat the exact event and verify the stored disposition is returned with no second transition or command claim (SC2/T002).
3. Submit a one-component mismatch and verify rejection with no activity completion (SC2/T002).

## Intent coverage

| Criterion | Task | Acceptance |
|-----------|------|------------|
| SC1 | T003 | AC1 |
| SC2 | T002 | AC1 |
| SC3 | T020 | AC1 |
| SC4 | T004 | AC1 |

## Held-out checks

| Edge | Task | Test |
|------|------|------|
| E1 | T003 | helix-desktop/src/campaign/CampaignWorkspace.test.tsx::does_not_create_an_activity_for_an_unknown_identity |
| E2 | T003 | helix-desktop/src/campaign/CampaignWorkspace.test.tsx::does_not_regress_pending_projection_from_an_older_revision |
| E4 | T002 | tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs::Rejects_one_component_subject_mismatch |
| E6 | T020 | tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs::Reconciles_recovery_time_signal_once |

## Dependency notes

- T002 depends on T001 because it consumes the `PendingActivity`, `ExternalEvidenceReceipt`, and `IExternalEvidenceStore` transaction contract to validate and transition the exact durable subject.
- T003 depends on T002 because its Agent client consumes the campaign create/read and ingress projection/disposition interface declared by T002; the landed T019 repair supplies the create/read implementation. T003 remains projection-only and has no Agent command-path dependency.
- T004 depends on T002 because the supported local-execution document names the landed ingress, recovery, and authority semantics rather than documenting a proposed interface.
- T008, T009, T010, and T011 are sequential because they all edit `docs-site/docs/operator/deployment-setup.md`; each later task preserves the earlier corrected claims and its Verify rechecks its own claim.
- T018 depends on T001 and T002 because it repairs the persisted activity/store and ingress files those tasks introduced; its Verify re-runs the database-backed store and ingress regressions.
- T019 depends on T002 because it repairs the endpoint and route wiring consumed by T003; its Verify rechecks T002's mismatch and recovery behavior.
- T020 depends on T003 because its composed SC3 Verify reruns the desktop projection/readback test; the desktop test is verification-only and remains unmodified by the Agent-scoped task.
- T020 depends on T002 because it consumes and reruns T002's already-landed E6 recovery-time signal test as prerequisite evidence; T002 remains SC2-only and its implementation/test are not changed to transfer plan-level ownership.
- T020 depends on T018 and T019 because its Agent recovery/command-path investigation and tests share their endpoint wiring and campaign ingress/store test files. These dependencies order all shared-file work after the landed repair contracts; T020 must not treat the direct capability route or raw runspace submission as an approved substitute.
