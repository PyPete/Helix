---
id: T020
title: Prove Agent lease and safety behavior across campaign recovery
wave: 6
deps: [T002, T003, T018, T019]
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - Helix.Orchestration.Agent/Api/DeviceEndpoints.cs
  - Helix.Orchestration.Agent/Api/ExternalEvidenceEndpoints.cs
  - Helix.Orchestration.Agent/Api/WorkflowEndpoints.cs
  - Helix.Orchestration.Agent/Execution/WslExecutionAdapter.cs
  - Helix.Orchestration.Agent/Program.cs
  - helix-desktop/src/campaign/CampaignWorkspace.test.tsx
  - tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs
  - tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs
  - tests/Helix.Orchestration.Agent.Tests/Execution/WslExecutionAdapterTests.cs
---

# T020 — Prove Agent lease and safety behavior across campaign recovery

## Context

Intent SC3 and N3 remain binding: the external wait/recovery must not retain a device lease, and any later command must re-enter the Agent capability, lease, validation, and safety path. T002/T019 provide the durable campaign create/read/evidence routes, but they do not define a later-command campaign route. Current code evidence shows `/devices/{id}/execute` invokes a capability directly, `/runspaces` accepts a runspace definition directly, and `WslExecutionAdapter` acquires a lease but is not wired to either campaign recovery or a public post-wait route; none is sufficient evidence for SC3/N3. T002's existing `Reconciles_recovery_time_signal_once` implementation/test remains byte-for-byte unchanged prerequisite evidence; T020 reruns and composes it but owns E6 in this plan because E6 is a held-out SC3 edge.

## Approach

- Trace the actual mapped Agent routes, their dependency-injection wiring, lease lifetime, capability validation, safety checks, and audit behavior; do not treat route names or compile-time safety checks as proof of execution safety.
- Reuse an actually existing supported Agent command flow only if it demonstrably reacquires/renews the lease and revalidates capability, command parameters, and safety after the external wait/recovery. Prove both no retained lease throughout the wait/recovery and the later command path with Agent tests; do not invent or imply a campaign route.
- If no such supported path exists, do not add or imply a campaign command endpoint and do not implement a new route. Stop and append exactly this question to the task Log as `NEEDS-ORCHESTRATOR`: “Which exact Agent entry point and request contract should T020 use for a post-recovery command, given that the bounded campaign API is create/read/evidence only and the existing direct-capability/raw-runspace routes bypass the required lease, validation, and safety boundary?” Do not claim SC3/N3 is satisfied in that branch; implementation waits for the answer and any necessary plan repair.
- T020 is the plan-level owner of SC3 and E6. Rerun/compose T002's unchanged `Reconciles_recovery_time_signal_once` test and the T018/T019 regressions when touching their shared Agent files; do not edit T002's landed implementation or existing E6 test while transferring ownership.
- The desktop test path is regression-only: run the T003 projection/readback check for the composed SC3 evidence, but do not edit desktop code or tests from this Agent-scoped task.

## Interface contract

- Agent API: `POST /campaigns/bounded-activity` returns `{ campaignId, activityId, flowId, awaitingStepId, commandId, revision, lifecycle, physicalOutcome, audit }`; `GET /campaigns/{campaignId}/activities/{activityId}` returns that projection; `POST /external-evidence` accepts `{ campaignId, activityId, flowId, awaitingStepId, expectedRevision, source, eventId, payload }` and returns `{ disposition, projection }`.

## Intent coverage

- SC3

## Acceptance criteria

1. Using an existing supported Agent command flow (not a newly defined campaign route), the composed Agent and T003 projection/readback tests prove the workspace restores the same durable state without a regressing revision, no device lease is retained through the external wait/recovery, a later command reacquires or renews a lease and revalidates capability/parameters and safety before invocation, and recovery does not repeat the physical command (SC3/E5/N3).
2. E6: rerun/compose the unchanged T002 `Reconciles_recovery_time_signal_once` test and prove a signal arriving during recovery is reconciled once from durable state without an ambiguous second command; T020 owns this held-out SC3 edge at plan level.
3. If no existing supported path satisfies AC1, the task stops before adding or implying a route, records one precise `NEEDS-ORCHESTRATOR` question in its Log, and leaves SC3/N3 explicitly unproven pending the answer; the direct capability route and raw runspace submission are not accepted as substitutes.

## Verify

```bash
set -e
test -f tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Post_recovery_command_reacquires_lease_and_revalidates_before_invocation"
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Rejects_one_component_subject_mismatch"
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Reconciles_recovery_time_signal_once"
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Bounded_activity_routes_create_and_load_the_same_durable_projection"
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~PostgresExternalEvidenceStoreTests"
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~WslExecutionAdapterTests"
npm install --prefix helix-desktop
npm exec --prefix helix-desktop -- vitest run --config helix-desktop/vitest.config.ts helix-desktop/src/campaign/CampaignWorkspace.test.tsx -t "^does_not_regress_pending_projection_from_an_older_revision$"
```

Heavy: yes

## Log

- 2026-10-28 — created by planner during build-repair; no approved/supported campaign later-command route is assumed. If inspection confirms the current gap, stop before implementation and ask the scoped question.
- 2026-10-28 — corrected plan-level ownership: T020 owns SC3/E6; T002 remains an unchanged implementation/test prerequisite whose E6 regression is rerun/combined here.
