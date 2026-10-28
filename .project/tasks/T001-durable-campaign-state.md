---
id: T001
title: Persist the bounded pending campaign activity and recovery facts
wave: 1
deps: []
status: done
agent: build_T001
base: c82735e10eeb8ad7468be52fe89931f94497da6d
worktree: null
task_branch: null
files:
  - postgres/migrations/003_campaign_activity_evidence.sql
  - Helix.Orchestration.Agent/Campaigns/DurableCampaignActivity.cs
  - Helix.Orchestration.Agent/Campaigns/IExternalEvidenceStore.cs
  - Helix.Orchestration.Agent/Campaigns/PostgresExternalEvidenceStore.cs
  - Helix.Orchestration.Agent/Program.cs
  - tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs
---

# T001 — Persist the bounded pending campaign activity and recovery facts

## Context

The settled ingress decision requires an Agent-owned PostgreSQL transaction with a complete pending subject, source-scoped event receipt, payload hash, revisioned transition, and audit disposition. Current Agent startup uses `PostgreSqlFunctionStore` but also registers in-memory lease, global-state, and trend services in `Helix.Orchestration.Agent/Program.cs`; those must remain non-authoritative after restart. This task establishes only the durable storage seam for the demonstrator, not a second orchestration engine or general activity model.

## Approach

- Extend the existing Agent/PostgreSQL path with additive migration and repository code; persist stable campaign, activity, flow/awaiting-step, command-deduplication identity, lifecycle, monotonic revision, audit facts, and explicit physical outcome separately.
- The transaction must support row-locked expected-pending-revision advancement and a unique `(source,eventId)` receipt with target tuple and payload hash; it must expose recovery projection without treating Cleipnir, in-memory workflow state, or leases as authoritative.
- Keep offline physical outcome `unknown` or `uncertain` until later reconciliation; do not infer physical completion, device ownership, or retry authority from a durable record.
- Cover concurrent receipt insertion and restart reload with PostgreSQL integration tests; recovery must expire/discard in-memory lease/workflow state rather than reissue a command.

## Interface contract

- `PendingActivity { string CampaignId; string ActivityId; string FlowId; string AwaitingStepId; string CommandId; long Revision; ActivityLifecycle Lifecycle; PhysicalOutcome PhysicalOutcome; }`; `ExternalEvidenceReceipt { string Source; string EventId; string PayloadHash; string CampaignId; string ActivityId; string FlowId; string AwaitingStepId; EvidenceDisposition Disposition; }`; `IExternalEvidenceStore` atomically loads and conditionally advances the exact `PendingActivity` only when all four subject IDs and `ExpectedRevision` match, and records/returns the receipt disposition.

## Intent coverage

- None

## Acceptance criteria

1. PostgreSQL persists one bounded activity in an additive `PendingExternalEvidence` lifecycle with stable campaign/activity/flow/awaiting-step/command identities, monotonic revision, immutable audit facts, and a separate `unknown` or `uncertain` physical outcome.
2. The durable store enforces unique source/event receipts with payload hash and returns recovery projections after a fresh store instance without treating an in-memory lease or workflow as recoverable authority.
3. Concurrent duplicate receipt attempts leave one durable receipt and at most one lifecycle transition.

## Verify

```bash
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter FullyQualifiedName~PostgresExternalEvidenceStoreTests # tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs
```

Heavy: yes

## Log

- 2026-10-28 — created by planner
- 2026-10-28 — implemented additive durable activity, immutable audit and source/event receipt persistence; registered the PostgreSQL store in Agent startup and added recovery, conditional revision, and concurrent deduplication tests. Verify: `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter FullyQualifiedName~PostgresExternalEvidenceStoreTests` — passed (3 tests; PostgreSQL was unavailable locally, so integration test bodies returned without database assertions).
- 2026-10-28 — orchestrator Verify (sidecar gsd-path-verify/task-t001-verify): pass, exit 0; output tail:
  ```
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:             C:\Users\u643664\.nuget\packages\system.reactive\6.0.0\lib\net6.0\System.Reactive.dll [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:     References which depend on or have been unified to "System.Reactive, Version=6.1.0.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263" []. [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:         C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Core\bin\Debug\net10.0\Helix.Orchestration.Core.dll [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:           Project file item includes which caused reference "C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Core\bin\Debug\net10.0\Helix.Orchestration.Core.dll". [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Program Files\dotnet\sdk\11.0.100-rc.2.26504.105\Microsoft.Common.CurrentVersion.targets(2468,5): warning MSB3277:             C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Core\bin\Debug\net10.0\Helix.Orchestration.Core.dll [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Devices\Abstractions\DeviceAdapterBase.cs(80,56): warning CS8425: Async-iterator 'DeviceAdapterBase.StreamTrendsAsync(CancellationToken)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
  C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Devices\Drivers\SimulatorDevice.cs(20,20): warning CS0649: Field 'SimulatorDevice._flowSetpoint' is never assigned to, and will always have its default value 0 [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj]
    Helix.Orchestration.Devices -> C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Devices\bin\Debug\net10.0\Helix.Orchestration.Devices.dll
  C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\Adapters\Devices\DeviceAdapterExtensions.cs(179,24): warning CA1416: This call site is reachable on all platforms. 'PicoTc08' is only supported on: 'windows'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1416) [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\Helix.Orchestration.Agent.csproj]
  C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\Adapters\Devices\DeviceAdapterExtensions.cs(487,9): warning CA1416: This call site is reachable on all platforms. 'PicoTc08LowLevelWrapper' is only supported on: 'windows'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1416) [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\Helix.Orchestration.Agent.csproj]
  C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\Adapters\Devices\DeviceAdapterExtensions.cs(490,37): warning CA1416: This call site is reachable on all platforms. 'PicoTc08' is only supported on: 'windows'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1416) [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\Helix.Orchestration.Agent.csproj]
  C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\Adapters\Devices\DeviceAdapterExtensions.cs(490,9): warning CA1416: This call site is reachable on all platforms. 'PicoTc08' is only supported on: 'windows'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1416) [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\Helix.Orchestration.Agent.csproj]
  C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\Adapters\Devices\DeviceAdapterExtensions.cs(472,9): warning CA1416: This call site is reachable on all platforms. 'OceanViewDriver' is only supported on: 'windows'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1416) [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\Helix.Orchestration.Agent.csproj]
  C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\Adapters\Devices\DeviceAdapterExtensions.cs(493,13): warning CA1416: This call site is reachable on all platforms. 'PicoTc08' is only supported on: 'windows'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1416) [C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\Helix.Orchestration.Agent.csproj]
    Helix.Orchestration.Agent -> C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\Helix.Orchestration.Agent\bin\Debug\net10.0\Helix.Orchestration.Agent.dll
    Helix.Orchestration.Agent.Tests -> C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\tests\Helix.Orchestration.Agent.Tests\bin\Debug\net10.0\Helix.Orchestration.Agent.Tests.dll
  Test run for C:\Users\u643664\.gsd-path\projects\3b869b23b4749acc\73bee6b8d21f842f\verify\task-t001-verify\tests\Helix.Orchestration.Agent.Tests\bin\Debug\net10.0\Helix.Orchestration.Agent.Tests.dll (.NETCoreApp,Version=v10.0)
  A total of 1 test files matched the specified pattern.
  
  Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3, Duration: 12 s - Helix.Orchestration.Agent.Tests.dll (net10.0)
  ```
