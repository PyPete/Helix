---
id: T003
title: Deliver the local desktop campaign workspace
wave: 2
deps: [T002]
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - helix-desktop/src/campaign/agentCampaignClient.ts
  - helix-desktop/src/campaign/CampaignWorkspace.tsx
  - helix-desktop/src/campaign/CampaignWorkspace.test.tsx
  - helix-desktop/src/App.tsx
  - helix-desktop/src/App.test.tsx
---

# T003 — Deliver the local desktop campaign workspace

## Context

The local `helix-desktop` is the primary operator surface; the web dashboard is expressly parked. The walking skeleton starts one bounded simulated activity through the approved Agent create route and displays the durable campaign/activity identity, lifecycle, external-evidence wait, physical-outcome uncertainty, and Agent audit history before and after closure/restart. This is a projection-only task: it does not prove lease release or authorize a later device command; the transferred Agent lease/revalidation proof belongs to T020.

## Approach

- Add the campaign workspace to the existing desktop shell without redesigning unrelated panels and consume only the Agent campaign create/get/evidence route contract landed by T002/T019.
- Starting must request the Agent-owned bounded simulated activity; unknown or missing identity must report an error and never synthesize a second local activity.
- Apply only newer durable revisions to the projection so late lifecycle/audit responses cannot regress `PendingExternalEvidence`; restore the durable projection after remount/reconnect.
- N1: wording and audit presentation must describe a bounded simulated activity and separate physical outcome `unknown`/`uncertain`, never present desktop state as proof that physical work completed. The desktop remains projection-only and sends no direct device command; T020 owns the Agent-side SC3/N3 lease and later-command proof.

## Interface contract

- Agent campaign projection consumed by `agentCampaignClient`: `{ campaignId: string; activityId: string; flowId: string; awaitingStepId: string; commandId: string; revision: number; lifecycle: "PendingExternalEvidence" | "Completed"; physicalOutcome: "unknown" | "uncertain"; audit: Array<{ disposition: string; occurredAt: string }> }`; the client sends no direct device command and ignores a response whose `revision` is lower than the displayed projection.
- Agent API: `POST /campaigns/bounded-activity` returns `{ campaignId, activityId, flowId, awaitingStepId, commandId, revision, lifecycle, physicalOutcome, audit }`; `GET /campaigns/{campaignId}/activities/{activityId}` returns that projection; `POST /external-evidence` accepts `{ campaignId, activityId, flowId, awaitingStepId, expectedRevision, source, eventId, payload }` and returns `{ disposition, projection }`.

## Intent coverage

- SC1

## Acceptance criteria

1. A scientist can start the one bounded simulated activity from `helix-desktop` and see its durable campaign/activity identity, pending external-evidence lifecycle, and Agent audit history (SC1).
2. Closing/remounting the workspace restores the same pending or completed Agent projection and only newer durable revisions update it; the desktop issues no direct device command. This is projection-only behavior; T020 owns the complete SC3 lease and later-command proof.
3. E1: a missing or unknown campaign/activity identity is rejected and does not create a second activity; E2: an older lifecycle/audit revision received after pending cannot regress the displayed state.

## Verify

```bash
npm install --prefix helix-desktop && npm exec --prefix helix-desktop -- vitest run --config helix-desktop/vitest.config.ts helix-desktop/src/campaign/CampaignWorkspace.test.tsx -t "^does_not_create_an_activity_for_an_unknown_identity$"
npm exec --prefix helix-desktop -- vitest run --config helix-desktop/vitest.config.ts helix-desktop/src/campaign/CampaignWorkspace.test.tsx -t "^does_not_regress_pending_projection_from_an_older_revision$"
npm exec --prefix helix-desktop -- vitest run --config helix-desktop/vitest.config.ts helix-desktop/src/App.test.tsx
```

Heavy: no

## Log

- 2026-10-28 — created by planner
- 2026-10-28 — user ruling: keep T003 desktop projection-only; transfer no-retained-lease and later-command Agent revalidation proof to separate T020. Do not waive or change intent SC3/N3; keep T002 as the sole E6 owner. T003 covers only desktop start/display/readback/restore/monotonic projection and the approved no-direct-command boundary.
- 2026-10-28 — planner correction: the earlier Log attribution assigning E6 to T002 was orchestrator guidance, not a user ruling. T020 owns SC3/E6 at plan level; T002's landed E6 implementation/test remain unchanged prerequisite evidence for T020 to rerun/compose.
