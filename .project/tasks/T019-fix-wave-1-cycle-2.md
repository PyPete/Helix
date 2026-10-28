---
id: T019
title: Fix wave 1 cycle 2 review findings in T002
wave: 5
deps: [T002]
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

# T019 — Fix wave 1 cycle 2 review findings in T002

## Context

Repair task created from the wave 1 cycle 2 review of T002.
The failed criteria and every reviewer observation are recorded verbatim under Review findings.
The source task contracts are unchanged; read them and .project/intent/INTENT.md before editing.

## Approach

- Fix only what the Review findings name, inside the listed files; do not change the source
  tasks' acceptance criteria or interface contracts.
- Re-run the source tasks' Verify commands after the fix and record the result in the Log.

## Interface contract

- None

## Intent coverage

- None

## Acceptance criteria

1. Agent API: `POST /campaigns/bounded-activity` returns `{ campaignId, activityId, flowId, awaitingStepId, commandId, revision, lifecycle, physicalOutcome, audit }`; `GET /campaigns/{campaignId}/activities/{activityId}` returns that projection; `POST /external-evidence` accepts `{ campaignId, activityId, flowId, awaitingStepId, expectedRevision, source, eventId, payload }` and returns `{ disposition, projection }`.

## Verify

```bash
set -e
(
test -f tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs && dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Rejects_one_component_subject_mismatch"
test -f tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs && dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Reconciles_recovery_time_signal_once"
)
```

Heavy: yes

## Review findings

### t002_interface_contract
Criterion: Agent API: `POST /campaigns/bounded-activity` returns `{ campaignId, activityId, flowId, awaitingStepId, commandId, revision, lifecycle, physicalOutcome, audit }`; `GET /campaigns/{campaignId}/activities/{activityId}` returns that projection; `POST /external-evidence` accepts `{ campaignId, activityId, flowId, awaitingStepId, expectedRevision, source, eventId, payload }` and returns `{ disposition, projection }`.
- Agent API: `POST /campaigns/bounded-activity` returns `{ campaignId, activityId, flowId, awaitingStepId, commandId, revision, lifecycle, physicalOutcome, audit }`; `GET /campaigns/{campaignId}/activities/{activityId}` returns that projection; `POST /external-evidence` accepts `{ campaignId, activityId, flowId, awaitingStepId, expectedRevision, source, eventId, payload }` and returns `{ disposition, projection }`. — The reconstructed T002/T018 Agent product maps only `POST /external-evidence` (`Helix.Orchestration.Agent/Api/ExternalEvidenceEndpoints.cs:28-69`; Program maps only `MapExternalEvidenceEndpoints` at `Program.cs:334`); the T002 landing diff adds that single route and no bounded-activity POST or campaign/activity GET (`git show 1c40345f96a2febee3febf8b45b0be64b43d9729 -- Helix.Orchestration.Agent/Api/ExternalEvidenceEndpoints.cs Helix.Orchestration.Agent/Program.cs`). Neither required route exists in the landed Agent API, although T003's interface contract says its desktop client consumes the APIs landed by T002 (`.project/tasks/T003-desktop-campaign-workspace.md:28,35-36`). Fix: implement and map both routes through `IExternalEvidenceStore`, returning the declared durable projection/audit shape, and cover their create/load behavior with tests.

## Log

- 2026-10-28 — created by dispatch_driver.py fix-tasks from wave 1 cycle 2
