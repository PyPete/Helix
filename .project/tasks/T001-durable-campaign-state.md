---
id: T001
title: Persist the bounded pending campaign activity and recovery facts
wave: 1
deps: []
status: pending
agent: null
base: null
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
