# Synthesis

<!-- Written by the decider role. The planner treats Decisions as settled. -->

## Settled

- Delivery boundary — extend the existing Agent/PostgreSQL/Cleipnir path; do not add a second orchestration engine (`INTENT.md` § Summary; `evidence-stack.md` § Finding: The lowest-migration-cost implementation extends the current Agent/PostgreSQL boundary).
- Authority and surface — the local `helix-desktop` is the primary surface and the Agent remains the sole command authority; recovered commands must re-enter lease, capability, validation, safety, and audit checks (`INTENT.md` § Constraints; `evidence-pitfalls.md` § Finding: The present Agent cannot make restart guarantees from Cleipnir alone).
- Current lifecycle baseline — current campaign completion means submission, not external evidence, so the demonstrator requires an additive pending-evidence lifecycle (`evidence-pitfalls.md` § Finding: Current campaign completion means submission, not external evidence).
- Documentation — fix documentation to match the current runtime contract by default (`INTENT.md` § Current state).

## Decisions

### Durable ingress contract — research question: What durable ingress correlation, deduplication, ordering, and uncertain-physical-outcome contract governs the demonstrator and later analytical-result use?

- **Decision**: Implement an Agent-owned PostgreSQL transaction that validates the complete pending-subject tuple `(campaignId, activityId, flowId/awaiting-stepId)`, records an immutable source-scoped `(source, eventId)` receipt under a unique constraint with its payload hash, conditionally advances only the matching pending activity, and appends its disposition to the audit record. An identical replay returns the stored disposition; a reused identity with changed target or payload, an unknown/partial tuple, or a non-pending target is rejected without a transition.
- **Runner-up**: Use correlation as the idempotency key or process-memory duplicate tracking; it loses because correlation groups activity messages rather than identifying one delivery, and memory cannot survive the required Agent restart.
- **Evidence**: `evidence-stack.md` § Finding: A source-scoped immutable ingress-event identifier is the deduplication key; `evidence-stack.md` § Finding: Correlation must identify the pending subject separately from the delivery identity; `evidence-pitfalls.md` § Finding: Correlation and deduplication must be distinct stable identities; `evidence-pitfalls.md` § Finding: A PostgreSQL constraint can durably arbitrate one ingress identity.
- **Confidence**: high

### Lifecycle ordering and recovery

- **Decision**: Assign a durable per-activity monotonic revision and ingress timestamp; under a row lock, accept only the expected pending revision and record stale or repeated signals as no-ops. On Agent recovery, restore the durable activity/receipt/audit state, treat in-memory leases and workflow state as non-authoritative, and never automatically repeat a physical command.
- **Runner-up**: Treat transport arrival order, Cleipnir function records, or duplicate suppression alone as lifecycle ordering/recovery; they lose because they cannot prevent stale regressions or restore the Agent's in-memory state.
- **Evidence**: `evidence-stack.md` § Finding: Ordering is a per-activity durable revision rule, not transport arrival order; `evidence-stack.md` § Finding: The Agent needs a PostgreSQL-backed ingress record rather than process memory to make completion deduplication survive restart; `evidence-pitfalls.md` § Finding: Deduplication does not impose lifecycle order; `evidence-pitfalls.md` § Finding: The present Agent cannot make restart guarantees from Cleipnir alone.
- **Confidence**: high

### Physical-outcome model and later analytical-result reuse

- **Decision**: Persist ingress acceptance/deduplication, activity lifecycle transition, and physical observation as separate facts. Keep an external/offline physical outcome `unknown` or `uncertain` until reconciled or manually resolved; later analytical-result ingestion reuses the receipt and full subject correlation with provenance/audit, without changing the meaning of the demonstrator signal.
- **Runner-up**: Infer physical completion, ownership, or a safe retry from a durable record, desktop state, or accepted ingress signal; it loses because external side effects cannot participate in the database transaction and the intent forbids those claims.
- **Evidence**: `evidence-stack.md` § Finding: Physical outcome is an explicit epistemic state, independent of ingress acceptance and activity lifecycle; `evidence-pitfalls.md` § Finding: An unknown external side effect must remain unresolved rather than retried as completed.
- **Confidence**: high

## For the planner

- **Wave-1 blockers**: Prove the Agent can atomically persist the pending activity, full subject correlation, unique receipt/payload hash, revisioned transition, and audit disposition in PostgreSQL; prove restart recovery restores that state while expiring/non-authoritatively discarding in-memory lease/workflow state; exercise concurrent duplicate, altered-payload, near-match, stale, and recovery-time signals.
- **Walking skeleton**: From `helix-desktop`, start one bounded simulated activity through the Agent, persist and display its campaign/activity identity and `pending external evidence` state, submit one fully correlated ingress event, show the recorded completion disposition/audit history, then restart the desktop or Agent and show the restored state without another command or retained lease.
- **Pitfalls → tasks**: In-memory Agent state surviving neither restart nor authority checks → recovery task explicitly expires/reacquires leases and revalidates later commands; duplicate receipt races → migration/repository task creates unique source/event receipt plus stored target and payload hash; correlation/idempotency conflation and one-component matches → ingress validation task checks the complete tuple before transition; delayed updates regressing UI/lifecycle → revisioned transition and desktop projection task; uncertain offline action being retried/claimed complete → outcome/audit contract and recovery test task; current `Submitted`/`Completed` semantics being reused → additive pending-evidence lifecycle migration task.

## User rulings

- None — evidence resolves implementation choices; no genuine human-values choice was identified.

## Still unknown

- None — the intent's research question is resolved by the durable ingress, ordering, and physical-outcome decisions above.
