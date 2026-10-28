# Evidence — stack

<!-- Written by one researcher role. Consumed by the decider. -->

Dimension: stack
Questions assigned: What durable ingress correlation, deduplication, ordering, and uncertain-physical-outcome contract governs the demonstrator and later analytical-result use?

## Finding: The Agent needs a PostgreSQL-backed ingress record rather than process memory to make completion deduplication survive restart

- **Claim**: The existing Agent has a PostgreSQL-backed Cleipnir function store, but also registers leases, global state, and trends as in-memory services; the checked-in PostgreSQL migration has campaign and iteration identity keys but no ingress-event/idempotency table. Therefore a restart-safe ingress contract requires its accepted-event identity and resulting activity transition to be persisted in PostgreSQL, not held by the Agent process.
- **Source**: `Helix.Orchestration.Agent/Program.cs:62-88` (opened); `postgres/migrations/001_initial.sql:1-46` (opened); `.project/research/evidence-codebase.md:23-28` (opened).
- **Confidence**: high
- **Why it matters here**: SC2 and SC3 require a duplicate signal and an Agent restart during the wait to resolve to the same durable activity without a second command or a retained lease.

## Finding: A source-scoped immutable ingress-event identifier is the deduplication key

- **Claim**: The ingress contract should require an immutable `(source, eventId)` pair and persist it under a unique PostgreSQL constraint; a repeat of that pair must return the recorded acceptance/outcome rather than create another completion. Microsoft’s idempotent-consumer guidance states that at-least-once delivery can duplicate messages, exactly-once across external side effects is impractical, and CloudEvents `source` plus `id` is stable across redeliveries. PostgreSQL documents that `INSERT ... ON CONFLICT` gives an atomic insert-or-update outcome under high concurrency.
- **Source**: https://learn.microsoft.com/en-us/azure/architecture/patterns/idempotent-consumer (opened); https://www.postgresql.org/docs/current/sql-insert.html (opened).
- **Confidence**: high
- **Why it matters here**: It supplies a durable, testable answer to SC2’s repeated correlated completion signal while preserving the intent’s prohibition on treating ingress as an instrument command or proof of physical completion.

## Finding: Correlation must identify the pending subject separately from the delivery identity

- **Claim**: A valid ingress envelope needs both delivery identity `(source, eventId)` and a complete business-correlation tuple `(campaignId, activityId, flowId/awaiting-stepId)`; it must reject unknown, non-pending, or partially matching tuples. The current schema already establishes durable campaign identity and a composite `(campaign_id, iteration_index)` identity, but it has no activity/awaiting-step correlation field.
- **Source**: `postgres/migrations/001_initial.sql:1-45` (opened); `Helix.Orchestration.Infrastructure/Persistence/PostgresCampaignRepository.cs:9-48` (opened); `INTENT.md:36-41,55-58` (opened).
- **Confidence**: high
- **Why it matters here**: Separating correlation from deduplication prevents a one-component-near-match from completing another activity (E4), and gives later analytical-result ingestion a stable attachment point without reinterpreting a delivery ID as a scientific identity.

## Finding: Ordering is a per-activity durable revision rule, not transport arrival order

- **Claim**: The contract should assign an incrementing activity revision (and an ingress-recorded timestamp) when it accepts a transition, and apply a signal only if it is valid for the current pending revision; stale/repeated signals are recorded as no-ops rather than regress state. PostgreSQL documents that `FOR UPDATE` prevents concurrent transactions from modifying a selected row until transaction end, and that ordinary Read Committed reads can otherwise observe different committed data across statements.
- **Source**: https://www.postgresql.org/docs/current/explicit-locking.html (opened); https://www.postgresql.org/docs/current/transaction-iso.html (opened); `INTENT.md:37-41` (opened).
- **Confidence**: high
- **Why it matters here**: This meets the held-out ordering cases (E2 and E6) using the existing PostgreSQL path and avoids asserting an unsupported global order across desktop, restart recovery, and external senders.

## Finding: Physical outcome is an explicit epistemic state, independent of ingress acceptance and activity lifecycle

- **Claim**: The durable record must distinguish at least `ingress accepted/deduplicated`, `activity transition accepted`, and `physical outcome unknown/observed`; it must never derive physical completion or device ownership from an ingress event. The Microsoft guidance explicitly says exactly-once cannot cover consumer-implemented external side effects, while the intent explicitly prohibits treating ingress or durable records as proof of offline physical completion.
- **Source**: https://learn.microsoft.com/en-us/azure/architecture/patterns/idempotent-consumer (opened); `INTENT.md:28-29,48-50,65-66` (opened).
- **Confidence**: high
- **Why it matters here**: The demonstrator can safely show a correlated simulated completion, and the same model can later retain an analytical result’s provenance without falsely elevating an externally reported result to proof of physical work.

## Finding: The lowest-migration-cost implementation extends the current Agent/PostgreSQL boundary

- **Claim**: Adding narrow PostgreSQL migration(s) for activity state, ingress receipts, and append-only audit records, then handling the endpoint in the existing Agent, is lower migration cost than adding an orchestration engine: this repository already uses PostgreSQL/Npgsql/Dapper and a PostgreSQL-backed Cleipnir store, and the intent expressly requires extending that path and vetoes a second orchestration engine. A transactional outbox, if durable notification/replay is later needed, is a table plus relay pattern: its core property is committing business state and the outgoing record in the same local database transaction, not introducing another workflow engine.
- **Source**: `.project/research/evidence-codebase.md:7-13,23-28` (opened); `Helix.Orchestration.Agent/Program.cs:70-83` (opened); `INTENT.md:15,53-59,68-73` (opened); https://learn.microsoft.com/en-us/power-platform/architecture/reference-architectures/transactional-outbox-pattern (opened).
- **Confidence**: high
- **Why it matters here**: It fits the brownfield stack and preserves the Agent-only capability/lease/safety authority while leaving later analytical-result support able to reuse the same receipt, correlation, revision, and audit contract.

## Assigned questions — answers

- What durable ingress correlation, deduplication, ordering, and uncertain-physical-outcome contract governs the demonstrator and later analytical-result use? → Use one Agent-owned PostgreSQL transaction to (1) validate the full pending-subject tuple `(campaignId, activityId, flowId/awaiting-stepId)`, (2) insert a uniquely constrained immutable `(source, eventId)` receipt, (3) lock and conditionally advance only that activity’s current revision, and (4) append the resulting audit/outcome record. Replays return the original recorded disposition; unknown, partial, stale, or non-pending correlations do not advance state. Record per-activity revision order rather than arrival/global order. Model ingress acceptance, lifecycle transition, and physical observation separately, retaining `unknown` when evidence cannot establish the external physical outcome. Later analytical results reuse the source/event receipt and full subject correlation and attach provenance/audit records; they do not change the meaning of a demonstrator ingress signal. This is an extension of the existing Agent/PostgreSQL/Cleipnir path, not a second orchestration engine. Sources: https://learn.microsoft.com/en-us/azure/architecture/patterns/idempotent-consumer (opened); https://www.postgresql.org/docs/current/sql-insert.html (opened); https://www.postgresql.org/docs/current/explicit-locking.html (opened); `INTENT.md:15,27-29,36-41,48-50,55-58,68-73` (opened).

## Dead ends

- Current Agent/campaign code search — no existing external-evidence ingress endpoint, activity-level pending correlation field, or durable ingress deduplication receipt was found in the inspected results; the future contract cannot safely be inferred from current campaign primary keys alone.
- A second orchestration engine — not investigated as an implementation option because `INTENT.md:15,68-73` requires extension of the existing Agent/PostgreSQL/Cleipnir path and explicitly rules it out.
