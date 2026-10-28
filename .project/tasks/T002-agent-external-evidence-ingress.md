---
id: T002
title: Expose the Agent-owned correlated external-evidence ingress
wave: 1
deps: [T001]
status: done
agent: build_T002
base: 5dbd4ffa5e84ab0859148b4068a6ef698777c80e
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
- 2026-10-28 — implemented the Agent external-evidence ingress with subject/revision validation, payload hashing, durable disposition/replay handling, and explicitly non-physical-completion wording; added mismatch, stale, altered-payload, and recovery/concurrency tests. Verify: PowerShell guarded equivalent of `test -f tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs && dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Rejects_one_component_subject_mismatch"` — passed (1/1); PowerShell guarded equivalent of `test -f tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs && dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Reconciles_recovery_time_signal_once"` — passed (1/1). Additional `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests"` — passed (4/4).
- 2026-10-28 — orchestrator Verify (sidecar gsd-path-verify/task-t002-verify): pass, exit 0; output tail:
  ```
    Helix.Orchestration.Core -> C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Core\bin\Debug\net10.0\Helix.Orchestration.Core.dll
    Helix.Orchestration.Infrastructure -> C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Infrastructure\bin\Debug\net10.0\Helix.Orchestration.Infrastructure.dll
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277: Found conflicts between different versions of "System.Reactive" that could not be resolved. [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277: There was a conflict between "System.Reactive, Version=6.0.0.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263" and "System.Reactive, Version=6.1.0.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263". [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:     "System.Reactive, Version=6.0.0.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263" was chosen because it was primary and "System.Reactive, Version=6.1.0.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263" was not. [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:     References which depend on "System.Reactive, Version=6.0.0.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263" [C:\Users\u643664\.nuget\packages\system.reactive\6.0.0\lib\net6.0\System.Reactive.dll]. [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:         C:\Users\u643664\.nuget\packages\system.reactive\6.0.0\lib\net6.0\System.Reactive.dll [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:           Project file item includes which caused reference "C:\Users\u643664\.nuget\packages\system.reactive\6.0.0\lib\net6.0\System.Reactive.dll". [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:             C:\Users\u643664\.nuget\packages\system.reactive\6.0.0\lib\net6.0\System.Reactive.dll [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:     References which depend on or have been unified to "System.Reactive, Version=6.1.0.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263" []. [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:         C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Core\bin\Debug\net10.0\Helix.Orchestration.Core.dll [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:           Project file item includes which caused reference "C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Core\bin\Debug\net10.0\Helix.Orchestration.Core.dll". [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:             C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Core\bin\Debug\net10.0\Helix.Orchestration.Core.dll [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
    Helix.Orchestration.Devices -> C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Devices\bin\Debug\net10.0\Helix.Orchestration.Devices.dll
    Helix.Orchestration.Agent -> C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\Helix.Orchestration.Agent\bin\Debug\net10.0\Helix.Orchestration.Agent.dll
    Helix.Orchestration.Agent.Tests -> C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\tests\Helix.Orchestration.Agent.Tests\bin\Debug\net10.0\Helix.Orchestration.Agent.Tests.dll
  Test run for C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t002-verify\tests\Helix.Orchestration.Agent.Tests\bin\Debug\net10.0\Helix.Orchestration.Agent.Tests.dll (.NETCoreApp,Version=v10.0)
  A total of 1 test files matched the specified pattern.
  
  Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 43 ms - Helix.Orchestration.Agent.Tests.dll (net10.0)
  ```
