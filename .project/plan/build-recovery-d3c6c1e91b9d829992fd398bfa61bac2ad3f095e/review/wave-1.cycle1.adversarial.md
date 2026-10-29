# Review — wave 1, cycle 1

Wave verdict: blocked
Cycle: 1
Depth: deep
Lens: adversarial
Tasks reviewed: 2

## T001 — Persist the bounded pending campaign activity and recovery facts: pass

- ✅ PostgreSQL persists one bounded activity in an additive `PendingExternalEvidence` lifecycle with stable campaign/activity/flow/awaiting-step/command identities, monotonic revision, immutable audit facts, and a separate `unknown` or `uncertain` physical outcome. — Schema constraints enforce the lifecycle/outcome and identities (`postgres/migrations/003_campaign_activity_evidence.sql:1-17`); create and audit are transactional (`PostgresExternalEvidenceStore.cs:73-105`), and audit UPDATE/DELETE is rejected (`postgres/migrations/003_campaign_activity_evidence.sql:48-60`).
- ✅ The durable store enforces unique source/event receipts with payload hash and returns recovery projections after a fresh store instance without treating an in-memory lease or workflow as recoverable authority. — `(source,event_id)` is the receipt primary key; `LoadRecoveryProjectionAsync` loads persisted pending rows (`PostgresExternalEvidenceStore.cs:28-41,127-143`), and Program registers the PostgreSQL store (`Helix.Orchestration.Agent/Program.cs:76-84`). Fresh-store/audit test is included and the recorded Verify passed 3/3 (`PostgresExternalEvidenceStoreTests.cs:23-44`; T001 Log).
- ✅ Concurrent duplicate receipt attempts leave one durable receipt and at most one lifecycle transition. — Receipt insert uses `ON CONFLICT ... DO NOTHING`, then locks the existing receipt; new delivery transitions hold the activity row lock and use expected revision (`PostgresExternalEvidenceStore.cs:203-233,236-255,283-293`). Recorded Verify passed 3/3; the concurrent duplicate test asserts a single receipt and one revision increment (`PostgresExternalEvidenceStoreTests.cs:46-73`; T001 Log).

Warnings (non-blocking):
- `PostgresExternalEvidenceStoreTests.cs:104-116` returns successfully when PostgreSQL is unavailable; the task Log notes an earlier run where all three test bodies returned without database assertions. The recorded isolated Verify is 3/3, but the harness should ensure database-backed assertions actually ran.

Contract violations (blocking):
- None.

## T002 — Expose the Agent-owned correlated external-evidence ingress: fail

- ❌ The Agent ingress accepts exactly one complete, revision-matching correlated event for a pending activity, persists its audit disposition, and returns stored disposition for an identical replay without a second completion or physical-command claim (SC2/E3/N2). — Found: the public `POST /external-evidence` accepts caller-supplied `source` and event data, then invokes the state-changing store without authentication/authorization or binding `source` to an authenticated identity (`ExternalEvidenceEndpoints.cs:18-24,42-45,69-94`; `Program.cs:331`). Any caller able to supply the subject tuple and current revision can invent a source/event/payload and transition the activity to `Completed`; the “correlation only” notice does not establish that the submission came from the claimed external source. Fix: authenticate/authorize the ingress and bind the source to a trusted principal or verify a source signature before calling `RecordReceiptAsync`. Existing tests establish altered-payload rejection and replay disposition (`ExternalEvidenceIngressTests.cs:28-76`); recorded full ingress Verify passed 4/4, but does not exercise source authentication.
- ✅ E4: a signal differing by one campaign/activity/flow/awaiting-step identifier is rejected and does not complete the pending activity. — `Rejects_one_component_subject_mismatch` asserts rejection and unchanged activity (`ExternalEvidenceIngressTests.cs:10-26`); recorded isolated Verify passed 1/1.
- ✅ E6: a signal arriving during recovery is reconciled once from durable state and does not leave an ambiguous second command. — Store transaction serializes the delivery key and activity transition (`PostgresExternalEvidenceStore.cs:203-233,236-255`); recorded recovery-time Verify passed 1/1 and ingress-class Verify passed 4/4. Caveat: `Reconciles_recovery_time_signal_once` models a fresh store with `FakeEvidenceStore` sharing an in-memory state object (`ExternalEvidenceIngressTests.cs:28-57`), so that test alone does not exercise a PostgreSQL/process-restart boundary.

Warnings (non-blocking):
- Recovery-time ingress coverage uses the in-memory fake described above; retain a PostgreSQL-backed recovery/replay check in CI.

Contract violations (blocking):
- None.

## Intent coverage

### SC2 — The Agent external-evidence ingress endpoint accepts one signal correlated to that pending activity and rejects or safely deduplicates a repeated signal without claiming that an offline instrument was controlled or completed automatically.: fail
- ❌ The ingress does not authenticate the source of a signal: `ExternalEvidenceRequest.Source` is trusted directly, and the mapped POST route has no authorization requirement (`ExternalEvidenceEndpoints.cs:18-24,69-94`; `Program.cs:331`). A caller can submit a forged but tuple/revision-matching signal and complete the activity. The response correctly disclaims offline control/completion (`ExternalEvidenceEndpoints.cs:15-16,44-45`), but that wording does not prevent forged evidence from changing durable state. E4's one-component mismatch check passed 1/1; the recorded replay/recovery checks passed, but none tests an untrusted source. Secure and bind the source before accepting a transition.

## Summary for orchestrator

- blocked findings: T002 AC1 and owned SC2 — unauthenticated caller-controlled evidence source can cause a pending activity to be marked completed.
- repeat offenders: none.
- warnings worth a human eye: PostgreSQL tests can return success without database assertions when PostgreSQL is unavailable; T002 recovery-time test uses an in-memory fake.
