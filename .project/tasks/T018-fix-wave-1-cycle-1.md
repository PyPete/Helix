---
id: T018
title: Fix wave 1 cycle 1 review findings in T001, T002
wave: 4
deps: [T001, T002]
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - Helix.Orchestration.Agent/Api/ExternalEvidenceEndpoints.cs
  - Helix.Orchestration.Agent/Campaigns/DurableCampaignActivity.cs
  - Helix.Orchestration.Agent/Campaigns/IExternalEvidenceStore.cs
  - Helix.Orchestration.Agent/Campaigns/PostgresExternalEvidenceStore.cs
  - Helix.Orchestration.Agent/Program.cs
  - postgres/migrations/003_campaign_activity_evidence.sql
  - tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs
  - tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs
---

# T018 — Fix wave 1 cycle 1 review findings in T001, T002

## Context

Repair task created from the wave 1 cycle 1 review of T001, T002.
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

1. The Agent external-evidence ingress endpoint accepts one signal correlated to that pending activity and rejects or safely deduplicates a repeated signal without claiming that an offline instrument was controlled or completed automatically.
2. PostgreSQL persists one bounded activity in an additive `PendingExternalEvidence` lifecycle with stable campaign/activity/flow/awaiting-step/command identities, monotonic revision, immutable audit facts, and a separate `unknown` or `uncertain` physical outcome.
3. The durable store enforces unique source/event receipts with payload hash and returns recovery projections after a fresh store instance without treating an in-memory lease or workflow as recoverable authority.
4. Concurrent duplicate receipt attempts leave one durable receipt and at most one lifecycle transition.
5. The Agent ingress accepts exactly one complete, revision-matching correlated event for a pending activity, persists its audit disposition, and returns stored disposition for an identical replay without a second completion or physical-command claim (SC2/E3/N2).
6. E6: a signal arriving during recovery is reconciled once from durable state and does not leave an ambiguous second command.

## Verify

```bash
set -e
(
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter FullyQualifiedName~PostgresExternalEvidenceStoreTests # tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs
)
(
test -f tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs && dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Rejects_one_component_subject_mismatch"
test -f tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs && dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --filter "FullyQualifiedName~ExternalEvidenceIngressTests.Reconciles_recovery_time_signal_once"
)
```

Heavy: yes

## Review findings

### sc2
Criterion: The Agent external-evidence ingress endpoint accepts one signal correlated to that pending activity and rejects or safely deduplicates a repeated signal without claiming that an offline instrument was controlled or completed automatically.
- The ingress does not authenticate the source of a signal: `ExternalEvidenceRequest.Source` is trusted directly, and the mapped POST route has no authorization requirement (`ExternalEvidenceEndpoints.cs:18-24,69-94`; `Program.cs:331`). A caller can submit a forged but tuple/revision-matching signal and complete the activity. The response correctly disclaims offline control/completion (`ExternalEvidenceEndpoints.cs:15-16,44-45`), but that wording does not prevent forged evidence from changing durable state. E4's one-component mismatch check passed 1/1; the recorded replay/recovery checks passed, but none tests an untrusted source. Secure and bind the source before accepting a transition.

### t001_ac1
Criterion: PostgreSQL persists one bounded activity in an additive `PendingExternalEvidence` lifecycle with stable campaign/activity/flow/awaiting-step/command identities, monotonic revision, immutable audit facts, and a separate `unknown` or `uncertain` physical outcome.
- PostgreSQL persists one bounded activity in an additive `PendingExternalEvidence` lifecycle with stable campaign/activity/flow/awaiting-step/command identities, monotonic revision, immutable audit facts, and a separate `unknown` or `uncertain` physical outcome. — The reconstructed patch defines the additive schema, audit immutability trigger, and transactional creation (`postgres/migrations/003_campaign_activity_evidence.sql:1-60`; `Helix.Orchestration.Agent/Campaigns/PostgresExternalEvidenceStore.cs:73-105`), but the recorded isolated Verify's 3/3 pass does not establish persistence: the task Log says PostgreSQL was unavailable and the integration bodies returned without database assertions (`.project/tasks/T001-durable-campaign-state.md:58-80`); `Fresh_store_recovers_pending_activity_and_audit` returns before setup/assertions when no connection is available (`tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs:23-43`). Fix the test/Verify setup to fail or explicitly report an unavailable database rather than pass, then run the criterion against PostgreSQL.

### t001_ac2
Criterion: The durable store enforces unique source/event receipts with payload hash and returns recovery projections after a fresh store instance without treating an in-memory lease or workflow as recoverable authority.
- The durable store enforces unique source/event receipts with payload hash and returns recovery projections after a fresh store instance without treating an in-memory lease or workflow as recoverable authority. — The schema has `(source,event_id)` as its primary key and stores the target tuple/hash (`postgres/migrations/003_campaign_activity_evidence.sql:18-29`); recovery reads pending rows (`Helix.Orchestration.Agent/Campaigns/PostgresExternalEvidenceStore.cs:127-143`). However, the fresh-store test returns before it creates or reloads an activity when PostgreSQL is unavailable (`tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs:23-43`), and the recorded pass is explicitly assertion-free in that environment (task Log, `.project/tasks/T001-durable-campaign-state.md:58-80`). Make unavailable PostgreSQL non-green and verify persistence/reload using two store instances against the database.

### t001_ac3
Criterion: Concurrent duplicate receipt attempts leave one durable receipt and at most one lifecycle transition.
- Concurrent duplicate receipt attempts leave one durable receipt and at most one lifecycle transition. — The implementation uses a unique receipt key, transaction, and row lock (`postgres/migrations/003_campaign_activity_evidence.sql:18-29`; `Helix.Orchestration.Agent/Campaigns/PostgresExternalEvidenceStore.cs:193-273`), but the sole concurrent database test exits before `Task.WhenAll` and its assertions when PostgreSQL is unavailable (`tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs:46-73`). The recorded 3/3 pass therefore does not exercise this criterion. Require a live database for this integration test and verify one receipt/transition under concurrency.

### t002_ac1
Criterion: The Agent ingress accepts exactly one complete, revision-matching correlated event for a pending activity, persists its audit disposition, and returns stored disposition for an identical replay without a second completion or physical-command claim (SC2/E3/N2).
- The Agent ingress accepts exactly one complete, revision-matching correlated event for a pending activity, persists its audit disposition, and returns stored disposition for an identical replay without a second completion or physical-command claim (SC2/E3/N2). — Found: the public `POST /external-evidence` accepts caller-supplied `source` and event data, then invokes the state-changing store without authentication/authorization or binding `source` to an authenticated identity (`ExternalEvidenceEndpoints.cs:18-24,42-45,69-94`; `Program.cs:331`). Any caller able to supply the subject tuple and current revision can invent a source/event/payload and transition the activity to `Completed`; the “correlation only” notice does not establish that the submission came from the claimed external source. Fix: authenticate/authorize the ingress and bind the source to a trusted principal or verify a source signature before calling `RecordReceiptAsync`. Existing tests establish altered-payload rejection and replay disposition (`ExternalEvidenceIngressTests.cs:28-76`); recorded full ingress Verify passed 4/4, but does not exercise source authentication.

### t002_ac3
Criterion: E6: a signal arriving during recovery is reconciled once from durable state and does not leave an ambiguous second command.
- E6: a signal arriving during recovery is reconciled once from durable state and does not leave an ambiguous second command. — The recorded `Reconciles_recovery_time_signal_once` output is 1/1 passed, but this is not a durable recovery check: both purported store instances receive the same `EvidenceState` object (`tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs:31-41`), whose activities and receipts are in-memory dictionaries (`:117-123`). Thus it proves neither a fresh PostgreSQL reload nor an Agent restart/recovery boundary. Replace this with an integration test that persists the pending activity, reconstructs the store/Agent against the same database, delivers the signal during recovery, and asserts one durable receipt and one transition with no second command; do not pass the test using shared in-memory state as recovery evidence.

## Log

- 2026-10-28 — created by dispatch_driver.py fix-tasks from wave 1 cycle 1
