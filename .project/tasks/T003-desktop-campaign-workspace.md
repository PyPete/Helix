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
  - tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs
  - helix-desktop/src/App.tsx
  - helix-desktop/src/App.test.tsx
---

# T003 — Deliver the local desktop campaign workspace

## Context

The local `helix-desktop` is the primary operator surface; the web dashboard is expressly parked. The walking skeleton starts one bounded simulated activity through the Agent and displays the durable campaign/activity identity, lifecycle, external-evidence wait, physical-outcome uncertainty, and Agent audit history before and after closure/restart. The desktop is a projection, not command authority or evidence of offline physical completion.

## Approach

- Add the campaign workspace to the existing desktop shell without redesigning unrelated panels and consume only the Agent projection/command contract landed by T002.
- Starting must request the Agent-owned bounded simulated activity; unknown or missing identity must report an error and never synthesize a second local activity.
- Apply only newer durable revisions to the projection so late lifecycle/audit responses cannot regress `PendingExternalEvidence`; restore the durable projection after remount/reconnect.
- N1: wording and audit presentation must describe a bounded simulated activity and separate physical outcome `unknown`/`uncertain`, never present desktop state as proof that physical work completed. For SC3, a later command is shown/requested only through the Agent path that renews lease, capability, validation, and safety checks (N3).

## Interface contract

- Agent campaign projection consumed by `agentCampaignClient`: `{ campaignId: string; activityId: string; flowId: string; awaitingStepId: string; commandId: string; revision: number; lifecycle: "PendingExternalEvidence" | "Completed"; physicalOutcome: "unknown" | "uncertain"; audit: Array<{ disposition: string; occurredAt: string }> }`; the client sends no direct device command and ignores a response whose `revision` is lower than the displayed projection.
- Agent API: `POST /campaigns/bounded-activity` returns `{ campaignId, activityId, flowId, awaitingStepId, commandId, revision, lifecycle, physicalOutcome, audit }`; `GET /campaigns/{campaignId}/activities/{activityId}` returns that projection; `POST /external-evidence` accepts `{ campaignId, activityId, flowId, awaitingStepId, expectedRevision, source, eventId, payload }` and returns `{ disposition, projection }`.

## Intent coverage

- SC1
- SC3

## Acceptance criteria

1. A scientist can start the one bounded simulated activity from `helix-desktop` and see its durable campaign/activity identity, pending external-evidence lifecycle, and Agent audit history (SC1).
2. Closing/remounting the workspace restores the same pending or completed Agent projection without another physical command or a retained lease; a recovery-time completion signal is reconciled once, and subsequent command attempts route back through the Agent renewal/capability/validation/safety path (SC3/E5/E6/N3).
3. E1: a missing or unknown campaign/activity identity is rejected and does not create a second activity; E2: an older lifecycle/audit revision received after pending cannot regress the displayed state.
4. E6: a completion signal that arrives during restart/recovery is reconciled once and does not create a second command or ambiguous duplicate completion.

## Verify

```bash
npm install --prefix helix-desktop && npm exec --prefix helix-desktop -- vitest run --config helix-desktop/vitest.config.ts helix-desktop/src/campaign/CampaignWorkspace.test.tsx -t "^does_not_create_an_activity_for_an_unknown_identity$"
npm exec --prefix helix-desktop -- vitest run --config helix-desktop/vitest.config.ts helix-desktop/src/campaign/CampaignWorkspace.test.tsx -t "^does_not_regress_pending_projection_from_an_older_revision$"
test -f tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs && dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Reconciles_recovery_time_signal_once"
npm exec --prefix helix-desktop -- vitest run --config helix-desktop/vitest.config.ts helix-desktop/src/App.test.tsx
```

Heavy: no

## Log

- 2026-10-28 — created by planner
