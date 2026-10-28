---
id: T002
title: Expose the Agent-owned correlated external-evidence ingress
wave: 1
deps: [T001]
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - Helix.Orchestration.Agent/Api/ExternalEvidenceEndpoints.cs
  - Helix.Orchestration.Agent/Program.cs
  - tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs
---

# T002 — Expose the Agent-owned correlated external-evidence ingress

## Context

The accepted design separates correlation from delivery identity: the complete pending subject tuple identifies the activity, while source-scoped `(source,eventId)` deduplicates delivery. The Agent is the sole command authority, and ingress records correlation and disposition only; it cannot claim an offline instrument was controlled or physically completed. Recovery-time and stale signals must reconcile once under the durable revision rule and never trigger a second physical command.

## Approach

- Map an Agent Minimal API ingress route in `Helix.Orchestration.Agent/Program.cs` through an endpoint module that exclusively calls the durable store contract from T001.
- Validate all subject identifiers and expected pending revision before a transition; reject unknown/partial, non-pending, stale, and one-component-near-match subjects, and reject reused delivery identity whose payload hash or target differs.
- Identical source/event/payload replays return the stored disposition; accepted, duplicate, rejected, and stale dispositions append auditable facts and never imply offline control/completion.
- Exercise concurrent duplicate, altered-payload, near-match, stale, and recovery-time delivery. N2: the endpoint response and audit wording must state correlation/deduplication only, not instrument authority or automatic completion.

## Interface contract

- `PendingActivity { string CampaignId; string ActivityId; string FlowId; string AwaitingStepId; string CommandId; long Revision; ActivityLifecycle Lifecycle; PhysicalOutcome PhysicalOutcome; }`; `ExternalEvidenceReceipt { string Source; string EventId; string PayloadHash; string CampaignId; string ActivityId; string FlowId; string AwaitingStepId; EvidenceDisposition Disposition; }`; `IExternalEvidenceStore` atomically loads and conditionally advances the exact `PendingActivity` only when all four subject IDs and `ExpectedRevision` match, and records/returns the receipt disposition.
- Agent campaign projection consumed by `agentCampaignClient`: `{ campaignId: string; activityId: string; flowId: string; awaitingStepId: string; commandId: string; revision: number; lifecycle: "PendingExternalEvidence" | "Completed"; physicalOutcome: "unknown" | "uncertain"; audit: Array<{ disposition: string; occurredAt: string }> }`; the client sends no direct device command and ignores a response whose `revision` is lower than the displayed projection.
- Agent API: `POST /campaigns/bounded-activity` returns `{ campaignId, activityId, flowId, awaitingStepId, commandId, revision, lifecycle, physicalOutcome, audit }`; `GET /campaigns/{campaignId}/activities/{activityId}` returns that projection; `POST /external-evidence` accepts `{ campaignId, activityId, flowId, awaitingStepId, expectedRevision, source, eventId, payload }` and returns `{ disposition, projection }`.

## Intent coverage

- SC2

## Acceptance criteria

1. The Agent ingress accepts exactly one complete, revision-matching correlated event for a pending activity, persists its audit disposition, and returns stored disposition for an identical replay without a second completion or physical-command claim (SC2/E3/N2).
2. E4: a signal differing by one campaign/activity/flow/awaiting-step identifier is rejected and does not complete the pending activity.
3. E6: a signal arriving during recovery is reconciled once from durable state and does not leave an ambiguous second command.

## Verify

```bash
test -f tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs && dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Rejects_one_component_subject_mismatch"
test -f tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs && dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Reconciles_recovery_time_signal_once"
```

Heavy: yes

## Log

- 2026-10-28 — created by planner
