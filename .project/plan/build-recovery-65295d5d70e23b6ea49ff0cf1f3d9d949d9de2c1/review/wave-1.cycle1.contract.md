# Review — wave 1, cycle 1

Wave verdict: pass
Cycle: 1
Depth: deep
Lens: contract
Tasks reviewed: 2

## T001 — Persist the bounded pending campaign activity and recovery facts: pass

- ✅ `PendingActivity { string CampaignId; string ActivityId; string FlowId; string AwaitingStepId; string CommandId; long Revision; ActivityLifecycle Lifecycle; PhysicalOutcome PhysicalOutcome; }`; `ExternalEvidenceReceipt { string Source; string EventId; string PayloadHash; string CampaignId; string ActivityId; string FlowId; string AwaitingStepId; EvidenceDisposition Disposition; }`; `IExternalEvidenceStore` atomically loads and conditionally advances the exact `PendingActivity` only when all four subject IDs and `ExpectedRevision` match, and records/returns the receipt disposition. — `DurableCampaignActivity.cs:23-57` defines both records; `IExternalEvidenceStore.cs:22-31` exposes conditional advancement/receipt recording; `PostgresExternalEvidenceStore.cs:178-190,193-273,353-364` row-locks and checks all subject IDs plus revision before advancing or returning the stored receipt result.
- ✅ PostgreSQL persists one bounded activity in an additive `PendingExternalEvidence` lifecycle with stable campaign/activity/flow/awaiting-step/command identities, monotonic revision, immutable audit facts, and a separate `unknown` or `uncertain` physical outcome. — `003_campaign_activity_evidence.sql:1-16,34-60` defines the additive tables, constrained lifecycle/outcome, stable identities, and immutable-audit trigger; `PostgresExternalEvidenceStore.cs:73-105,186-189` creates the pending record and advances revision only by one. T018's recorded isolated Verify is pass and its final Log records PostgreSQL-backed store tests passing 3/3 (`.project/tasks/T018-fix-wave-1-cycle-1.md:121-143`).
- ✅ The durable store enforces unique source/event receipts with payload hash and returns recovery projections after a fresh store instance without treating an in-memory lease or workflow as recoverable authority. — The schema makes `(source,event_id)` the receipt primary key with payload hash (`003_campaign_activity_evidence.sql:18-29`); `PostgresExternalEvidenceStore.cs:127-143,203-233` reads the durable pending projection and returns existing receipt disposition. `PostgresExternalEvidenceStoreTests.cs:25-43,99-113` reconstructs a fresh PostgreSQL store and fails rather than silently passes without PostgreSQL; recorded T018 Verify passed.
- ✅ Concurrent duplicate receipt attempts leave one durable receipt and at most one lifecycle transition. — `PostgresExternalEvidenceStore.cs:203-273` combines unique insert, transaction, row lock, and a single conditional update; `PostgresExternalEvidenceStoreTests.cs:46-70` asserts one stored receipt and one revision increment under concurrent delivery. The recorded T018 isolated Verify passed the required database-backed store suite.

Warnings (non-blocking):
- none

Contract violations (blocking):
- none

## T002 — Expose the Agent-owned correlated external-evidence ingress: pass

- ✅ `PendingActivity { string CampaignId; string ActivityId; string FlowId; string AwaitingStepId; string CommandId; long Revision; ActivityLifecycle Lifecycle; PhysicalOutcome PhysicalOutcome; }`; `ExternalEvidenceReceipt { string Source; string EventId; string PayloadHash; string CampaignId; string ActivityId; string FlowId; string AwaitingStepId; EvidenceDisposition Disposition; }`; `IExternalEvidenceStore` atomically loads and conditionally advances the exact `PendingActivity` only when all four subject IDs and `ExpectedRevision` match, and records/returns the receipt disposition. — The full recovery product retains the contract in `DurableCampaignActivity.cs:23-57` and `IExternalEvidenceStore.cs:22-31`; its store implementation checks the complete tuple and revision before transition at `PostgresExternalEvidenceStore.cs:178-190,236-273`.
- ✅ Agent campaign projection consumed by `agentCampaignClient`: `{ campaignId: string; activityId: string; flowId: string; awaitingStepId: string; commandId: string; revision: number; lifecycle: "PendingExternalEvidence" | "Completed"; physicalOutcome: "unknown" | "uncertain"; audit: Array<{ disposition: string; occurredAt: string }> }`; the client sends no direct device command and ignores a response whose `revision` is lower than the displayed projection. — `ExternalEvidenceEndpoints.cs:178-197,258-269` produces the declared Agent projection shape, including identity, lifecycle, physical outcome, revision, and audit; this Agent-only T002 product exposes no desktop or direct-device command path. The client-side monotonic-display behavior is a later T003-owned consumer responsibility, so this interface is satisfied as the T002 producer contract and should be tightened to distinguish producer from consumer obligations.
- ✅ Agent API: `POST /campaigns/bounded-activity` returns `{ campaignId, activityId, flowId, awaitingStepId, commandId, revision, lifecycle, physicalOutcome, audit }`; `GET /campaigns/{campaignId}/activities/{activityId}` returns that projection; `POST /external-evidence` accepts `{ campaignId, activityId, flowId, awaitingStepId, expectedRevision, source, eventId, payload }` and returns `{ disposition, projection }`. — T019 full-head repair maps create/read at `ExternalEvidenceEndpoints.cs:71-113` and ingress at `:28-68`, with application wiring at `Program.cs:334-335`; `ExternalEvidenceIngressTests.cs:128-173` covers create/read/same projection/404. T019's recorded isolated Verify is pass (`.project/tasks/T019-fix-wave-1-cycle-2.md:64-86`).
- ✅ The Agent ingress accepts exactly one complete, revision-matching correlated event for a pending activity, persists its audit disposition, and returns stored disposition for an identical replay without a second completion or physical-command claim (SC2/E3/N2). — `ExternalEvidenceEndpoints.cs:25-26,144-175` validates the complete request and explicitly disclaims control/completion; `:30-68,115-148` requires authenticated source binding before recording. `PostgresExternalEvidenceStore.cs:203-273` persists disposition and makes one transition, while replay returns stored disposition. `ExternalEvidenceIngressTests.cs:61-79,83-125` asserts one accepted transition/receipt, replay behavior, notice wording, and rejects unauthenticated, invalid-token, and forged-source calls. The recorded T018/T019 isolated Verifies passed these regressions.
- ✅ E4: a signal differing by one campaign/activity/flow/awaiting-step identifier is rejected and does not complete the pending activity. — `ExternalEvidenceIngressTests.cs:20-35` changes only `FlowId`, observes `Rejected`, no projection, and unchanged activity; recorded T002 Verify passed this exact held-out test, and T019 recorded it passing again (`T019-fix-wave-1-cycle-2.md:64`).
- ✅ E6: a signal arriving during recovery is reconciled once from durable state and does not leave an ambiguous second command. — `ExternalEvidenceIngressTests.cs:38-80` uses fresh PostgreSQL stores across recovery, races correlated submissions, then asserts one accepted receipt, completed revision exactly once, unchanged command ID, and one durable receipt. T018's recorded final isolated Verify passed this test 1/1, and T019 recorded the same regression as pass.

Warnings (non-blocking):
- The T002 projection-client sentence combines T002 producer requirements with later T003 client behavior; no current violation is found in the Agent producer, but the ownership wording should be split in a later plan/task revision.

Contract violations (blocking):
- none

## Intent coverage

### SC2 — The Agent external-evidence ingress endpoint accepts one signal correlated to that pending activity and rejects or safely deduplicates a repeated signal without claiming that an offline instrument was controlled or completed automatically.: pass
- ✅ The authenticated `POST /external-evidence` maps only to the durable correlation store (`ExternalEvidenceEndpoints.cs:28-68,133-148`), binds request source to a configured authenticated source (`:206-239`), and its notice expressly states that it does not assert offline instrument control or physical completion (`:25-26`). The durable transaction accepts once and returns the existing disposition for the identical `(source,eventId)` delivery (`PostgresExternalEvidenceStore.cs:203-273`). E3 is covered by `ExternalEvidenceIngressTests.cs:61-79` (one accepted transition/receipt with replayed stored disposition); E4 is covered by `:20-35` (one-component mismatch rejected); N2 is observed in endpoint wording and tested in `:63-67`. The recorded isolated T018 and T019 Verifies passed the PostgreSQL recovery/mismatch regressions.

## Summary for orchestrator

- blocked findings: none
- repeat offenders: none
- warnings worth a human eye: T002's projection-client interface sentence should separate the Agent producer contract from T003 consumer behavior.
