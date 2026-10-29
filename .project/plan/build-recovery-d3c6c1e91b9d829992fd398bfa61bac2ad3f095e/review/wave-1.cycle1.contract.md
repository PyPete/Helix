# Review — wave 1, cycle 1

Wave verdict: blocked
Cycle: 1
Depth: deep
Lens: contract
Tasks reviewed: 2

## T001 — Persist the bounded pending campaign activity and recovery facts: fail

- ❌ PostgreSQL persists one bounded activity in an additive `PendingExternalEvidence` lifecycle with stable campaign/activity/flow/awaiting-step/command identities, monotonic revision, immutable audit facts, and a separate `unknown` or `uncertain` physical outcome. — The reconstructed patch defines the additive schema, audit immutability trigger, and transactional creation (`postgres/migrations/003_campaign_activity_evidence.sql:1-60`; `Helix.Orchestration.Agent/Campaigns/PostgresExternalEvidenceStore.cs:73-105`), but the recorded isolated Verify's 3/3 pass does not establish persistence: the task Log says PostgreSQL was unavailable and the integration bodies returned without database assertions (`.project/tasks/T001-durable-campaign-state.md:58-80`); `Fresh_store_recovers_pending_activity_and_audit` returns before setup/assertions when no connection is available (`tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs:23-43`). Fix the test/Verify setup to fail or explicitly report an unavailable database rather than pass, then run the criterion against PostgreSQL.
- ❌ The durable store enforces unique source/event receipts with payload hash and returns recovery projections after a fresh store instance without treating an in-memory lease or workflow as recoverable authority. — The schema has `(source,event_id)` as its primary key and stores the target tuple/hash (`postgres/migrations/003_campaign_activity_evidence.sql:18-29`); recovery reads pending rows (`Helix.Orchestration.Agent/Campaigns/PostgresExternalEvidenceStore.cs:127-143`). However, the fresh-store test returns before it creates or reloads an activity when PostgreSQL is unavailable (`tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs:23-43`), and the recorded pass is explicitly assertion-free in that environment (task Log, `.project/tasks/T001-durable-campaign-state.md:58-80`). Make unavailable PostgreSQL non-green and verify persistence/reload using two store instances against the database.
- ❌ Concurrent duplicate receipt attempts leave one durable receipt and at most one lifecycle transition. — The implementation uses a unique receipt key, transaction, and row lock (`postgres/migrations/003_campaign_activity_evidence.sql:18-29`; `Helix.Orchestration.Agent/Campaigns/PostgresExternalEvidenceStore.cs:193-273`), but the sole concurrent database test exits before `Task.WhenAll` and its assertions when PostgreSQL is unavailable (`tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs:46-73`). The recorded 3/3 pass therefore does not exercise this criterion. Require a live database for this integration test and verify one receipt/transition under concurrency.

Warnings (non-blocking):
- The source implements the declared four-part subject, revision check, and unknown/uncertain physical outcome (`PostgresExternalEvidenceStore.cs:165-190, 353-378`); the blocking issue is that the database-dependent Verify silently passes without exercising it.

Contract violations (blocking):
- None found in the reconstructed patch: changed paths are the T001 declared files plus its permitted task metadata/append-only Log update.

## T002 — Expose the Agent-owned correlated external-evidence ingress: fail

- ✅ The Agent ingress accepts exactly one complete, revision-matching correlated event for a pending activity, persists its audit disposition, and returns stored disposition for an identical replay without a second completion or physical-command claim (SC2/E3/N2). — Recorded ingress-class Verify passed 4/4. The reconstructed endpoint validates all four subject IDs and expected revision, hashes the payload, and delegates to `IExternalEvidenceStore.RecordReceiptAsync` (`Helix.Orchestration.Agent/Api/ExternalEvidenceEndpoints.cs:20-45, 69-94`); the durable store records/replays the `(source,eventId)` disposition in its transaction (`Helix.Orchestration.Agent/Campaigns/PostgresExternalEvidenceStore.cs:193-273`). The returned contract includes disposition/projection plus replay and notice fields, and the notice explicitly disclaims offline control/completion (`ExternalEvidenceEndpoints.cs:15-16, 42-45`). The recorded duplicate/replay test also passed (`ExternalEvidenceIngressTests.Reconciles_recovery_time_signal_once`, task Log `.project/tasks/T002-agent-external-evidence-ingress.md:58-80`).
- ✅ E4: a signal differing by one campaign/activity/flow/awaiting-step identifier is rejected and does not complete the pending activity. — The recorded E4 test `ExternalEvidenceIngressTests.Rejects_one_component_subject_mismatch` passed 1/1; the test asserts rejection and unchanged lifecycle (`tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs:10-26`; task Log `.project/tasks/T002-agent-external-evidence-ingress.md:58-80`). The production store also rejects a tuple mismatch before transition (`PostgresExternalEvidenceStore.cs:236-255`).
- ❌ E6: a signal arriving during recovery is reconciled once from durable state and does not leave an ambiguous second command. — The recorded `Reconciles_recovery_time_signal_once` output is 1/1 passed, but this is not a durable recovery check: both purported store instances receive the same `EvidenceState` object (`tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs:31-41`), whose activities and receipts are in-memory dictionaries (`:117-123`). Thus it proves neither a fresh PostgreSQL reload nor an Agent restart/recovery boundary. Replace this with an integration test that persists the pending activity, reconstructs the store/Agent against the same database, delivers the signal during recovery, and asserts one durable receipt and one transition with no second command; do not pass the test using shared in-memory state as recovery evidence.

Warnings (non-blocking):
- The ingress tests call `ProcessAsync` rather than dispatching an HTTP request through the mapped route; route wiring is present at `Program.cs:332` and was inspected, but not exercised by the recorded filters. The create/projection routes in the broader interface are allocated to T003 by `PLAN.md:31-39`; their absence from this T002 patch is not treated as a scope violation.

Contract violations (blocking):
- None found in the reconstructed patch: changed paths are the T002 declared files plus its permitted task metadata/append-only Log update. The ingress request fields and response projection are compatible with the declared T002 ingress interface (`.project/tasks/T002-agent-external-evidence-ingress.md:30-34`; `ExternalEvidenceEndpoints.cs:124-149`).

## Intent coverage

### SC2 — The Agent external-evidence ingress endpoint accepts one signal correlated to that pending activity and rejects or safely deduplicates a repeated signal without claiming that an offline instrument was controlled or completed automatically.: pass
- ✅ Recorded isolated outputs are E4 mismatch 1/1, recovery/idempotent replay 1/1, and ingress class 4/4 (T002 task Log, `.project/tasks/T002-agent-external-evidence-ingress.md:58-80`); the endpoint/store path checks the complete subject and expected revision and returns the stored receipt disposition (`ExternalEvidenceEndpoints.cs:20-45`; `PostgresExternalEvidenceStore.cs:193-273`). E3 is covered by the repeated-request assertions (`ExternalEvidenceIngressTests.cs:39-56`). E4 held-out result: `Rejects_one_component_subject_mismatch`, 1/1 passed (same task Log). N2 judgment: the response notice states correlation/deduplication only and explicitly denies an offline control or physical-completion assertion (`ExternalEvidenceEndpoints.cs:15-16, 44-45`); the projection preserves `PhysicalOutcome.unknown` (`ExternalEvidenceEndpoints.cs:105-114`). E6 is a SC3 edge and is blocked under T002 above; it does not change this SC2 ruling.

## Fixed since last cycle

- Not applicable — cycle 1.

## Summary for orchestrator

- blocked findings: T001 AC1–AC3 — `tests/Helix.Orchestration.Agent.Tests/Campaigns/PostgresExternalEvidenceStoreTests.cs` returns without database assertions when PostgreSQL is unavailable; T002 E6/AC3 — `tests/Helix.Orchestration.Agent.Tests/Campaigns/ExternalEvidenceIngressTests.cs` uses shared in-memory state instead of durable recovery.
- repeat offenders: none — cycle 1.
- warnings worth a human eye: green T001 Verify totals mask early returns; T002's 4/4 class suite exercises the processor/fake store, not the HTTP host or durable recovery.
