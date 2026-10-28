# Plan — Helix durable-local-campaign-skeleton

<!-- Written by the planner role. Executed wave-by-wave by gsd-path-build. -->

Project verify: `dotnet restore Helix.Orchestration.slnx && dotnet build Helix.Orchestration.slnx --no-restore && dotnet test Helix.Orchestration.slnx --no-build && npm install --prefix helix-desktop && npm test --prefix helix-desktop -- --run && npm run build --prefix helix-desktop && npm install --prefix docs-site && npm run build --prefix docs-site`

## Config

- max_review_cycles: 3
- wave_budget: none
- review_panel: off
- finding_skeptics: off

## Wave 1 — durable-state and recovery risk burn-down

Goal: Prove the Agent can durably create the one pending simulated activity and atomically reconcile complete, deduplicated external evidence across restart without retaining a lease or replaying a command; failure changes the delivery design before any desktop surface lands.
Review depth: deep

## Wave 2 — local campaign walking skeleton

Goal: Deliver the usable local desktop path from starting the bounded activity through viewing durable pending/completed state and audit history, backed only by the Agent contract proven in wave 1.
Review depth: full

## Wave 3 — supported-contract documentation and accepted audit repairs

Goal: Publish the bounded local execution contract and correct each user-accepted historical documentation claim against the checked-in runtime, with no runtime scope expansion.
Review depth: verify-only

## Surface contract

### local helix-desktop campaign workspace — T003

Criteria: SC1, SC3
Entry: Open the Campaign workspace in `helix-desktop` with a configured Agent URL and select the start affordance for the one bounded simulated activity.
States: Empty shows the start affordance and no invented activity; loading shows that the durable Agent campaign projection is being retrieved; error reports that the projection could not be loaded or that an identity is missing/unknown without fabricating state; success shows campaign/activity/flow identities, a revisioned pending-or-completed lifecycle, physical outcome `unknown`/`uncertain` where applicable, and Agent audit history. Closing and reopening restores that same durable projection rather than creating another activity, and any later command is requested only through the Agent renewal/capability/validation/safety path. The supporting operator documentation describes this local execution, recovery, correlated ingress/deduplication, and Agent-only authority boundary without an offline physical-completion/control claim.
Walkthrough:
1. Start the bounded simulated activity and record the displayed durable campaign and activity IDs, pending external-evidence state, physical-outcome uncertainty, and Agent audit entry (SC1/T003).
2. Close and reopen the workspace while pending or after completion and verify the same durable projection is restored with no new activity, retained lease, or direct desktop command; verify a later command routes through the Agent path (SC3/T003).
3. Read the supported local-campaign documentation and verify that it describes the shipped local execution, ingress/deduplication, recovery, and Agent-only authority boundary without claiming offline physical completion or control (SC4/T004).

### Agent external-evidence ingress endpoint — T002

Criteria: SC2
Entry: POST the documented Agent ingress route with the complete campaign/activity/flow-or-awaiting-step tuple, expected pending revision, `source`, `eventId`, and payload for the pending activity.
States: Loading shows that the ingress HTTP request is in flight; error returns a non-transition disposition for an unknown, partial, one-component near-match, changed-payload, stale, or non-pending signal; success returns the persisted accepted disposition and updated projection, or the stored disposition for an identical replay, with no second transition, physical-command claim, or offline-instrument control/completion claim.
Walkthrough:
1. Submit one event whose complete tuple and expected revision match the pending activity and inspect the accepted transition and audit disposition (SC2/T002).
2. Repeat the exact event and verify the stored disposition is returned with no second transition or command claim (SC2/T002).
3. Submit a one-component mismatch and verify rejection with no activity completion (SC2/T002).

## Wave 4 — fix wave 1 cycle 1 review findings

Goal: Repair the blocking findings of the wave 1 cycle 1 review without changing source task contracts.
Review depth: deep

| Task | Title | Deps | Files |
|------|-------|------|-------|
| T018 | Fix wave 1 cycle 1 review findings in T001, T002 | T001, T002 | Helix.Orchestration.Agent/Api/ExternalEvidenceEndpoints.cs, Helix.Orchestration.Agent/Campaigns/DurableCampaignActivity.cs, Helix.Orchestration.Agent/Campaigns/IExternalEvidenceStore.cs, Helix.Orchestration.Agent/Campaigns/PostgresExternalEvidenceStore.cs, Helix.Orchestration.Agent/Program.cs, postgres/migrations/003_campaign_activity_evidence.sql, tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs, tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs |

## Wave 5 — fix wave 1 cycle 2 review findings

Goal: Repair the blocking findings of the wave 1 cycle 2 review without changing source task contracts.
Review depth: deep

| Task | Title | Deps | Files |
|------|-------|------|-------|
| T019 | Fix wave 1 cycle 2 review findings in T002 | T002 | Helix.Orchestration.Agent/Api/ExternalEvidenceEndpoints.cs, Helix.Orchestration.Agent/Program.cs, tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs |

## Intent coverage

| Criterion | Task | Acceptance |
|-----------|------|------------|
| SC1 | T003 | AC1 |
| SC2 | T002 | AC1 |
| SC3 | T003 | AC2 |
| SC4 | T004 | AC1 |

## Held-out checks

| Edge | Task | Test |
|------|------|------|
| E1 | T003 | helix-desktop/src/campaign/CampaignWorkspace.test.tsx::does_not_create_an_activity_for_an_unknown_identity |
| E2 | T003 | helix-desktop/src/campaign/CampaignWorkspace.test.tsx::does_not_regress_pending_projection_from_an_older_revision |
| E4 | T002 | tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs::Rejects_one_component_subject_mismatch |
| E6 | T003 | tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs::Reconciles_recovery_time_signal_once |

## Dependency notes

- T002 depends on T001 because it consumes the `PendingActivity`, `ExternalEvidenceReceipt`, and `IExternalEvidenceStore` transaction contract to validate and transition the exact durable subject.
- T003 depends on T002 because its Agent client consumes the campaign projection and ingress dispositions defined by the Agent endpoint; its test proves the projection after start/recovery rather than creating local state.
- T004 depends on T002 because the supported local-execution document names the landed ingress, recovery, and authority semantics rather than documenting a proposed interface.
- T008, T009, T010, and T011 are sequential because they all edit `docs-site/docs/operator/deployment-setup.md`; each later task preserves the earlier corrected claims and its Verify rechecks its own claim.
