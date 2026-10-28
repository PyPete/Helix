# Evidence — pitfalls

<!-- Written by one researcher role. Consumed by the decider. -->

Dimension: pitfalls
Questions assigned: What durable ingress correlation, deduplication, ordering, and uncertain-physical-outcome contract governs the demonstrator and later analytical-result use?

## Finding: The present Agent cannot make restart guarantees from Cleipnir alone

- **Claim**: The Agent registers Cleipnir's PostgreSQL function store but retains device leases, global workflow state, and trend state in singleton in-memory services; a process restart therefore loses those states even if a durable function record remains.
- **Source**: `Helix.Orchestration.Agent/Program.cs:62-88` (opened); `.project/research/evidence-codebase.md:23-28` (opened).
- **Confidence**: high
- **Why it matters here**: The ingress/recovery contract must persist its own pending/correlation/deduplication decision and treat leases as expired on restart, rather than infer either from a durable workflow record.

## Finding: A PostgreSQL constraint can durably arbitrate one ingress identity

- **Claim**: PostgreSQL `PRIMARY KEY` and `UNIQUE` constraints require non-null unique key values and automatically create a unique B-tree index, so a durable ingress ledger can atomically reject or return the same record for a reused, non-null deduplication key.
- **Source**: https://www.postgresql.org/docs/current/ddl-constraints.html (opened; sections 5.5.3–5.5.4).
- **Confidence**: high
- **Why it matters here**: Repeated evidence must be deduplicated in PostgreSQL by a stable ingress/event key plus its target identity, not by Agent memory that disappears during the required restart scenario.

## Finding: Correlation and deduplication must be distinct stable identities

- **Claim**: A correlation identifier groups related messages and must not be used as an idempotency key; deduplication instead requires an identifier that uniquely identifies one logical event across redeliveries, and mismatched immutable payload under a reused key must be treated as an error rather than silently accepted.
- **Source**: https://learn.microsoft.com/en-us/azure/architecture/patterns/idempotent-consumer (opened, “Choose a stable deduplication key” and example steps 4–6).
- **Confidence**: high
- **Why it matters here**: The ingress contract needs the durable `(campaignId, activityId)` target correlation and a separate source/event idempotency key (with a stored payload hash); a one-component mismatch must reject without creating or completing another activity.

## Finding: Deduplication does not impose lifecycle order

- **Claim**: Duplicate suppression alone does not guarantee message order; consumers that depend on order need an ordering mechanism or sequence/version data to reject stale messages.
- **Source**: https://learn.microsoft.com/en-us/azure/architecture/patterns/idempotent-consumer (opened, “Account for message ordering”).
- **Confidence**: high
- **Why it matters here**: Each durable activity needs a monotonic lifecycle/audit version (or expected-state transition) committed with the ingress record, so delayed evidence or UI/audit events cannot regress a pending/completed display.

## Finding: An unknown external side effect must remain unresolved rather than retried as completed

- **Claim**: When an external side effect cannot join the database transaction, a durable record must move through `in-progress` and `completed`; a redelivery that finds an unresolved `in-progress` record requires reconciliation or intervention before acknowledgement, because the earlier action might be partially completed.
- **Source**: https://learn.microsoft.com/en-us/azure/architecture/patterns/idempotent-consumer (opened, “Handle side effects that can't join the transaction”).
- **Confidence**: high
- **Why it matters here**: A timeout/restart around a device or offline-instrument action cannot be represented as physical completion; it must be an explicit uncertain outcome, must not issue a repeat command automatically, and requires renewed Agent lease/capability/safety checks before any later command.

## Finding: Current campaign completion means submission, not external evidence

- **Claim**: `CampaignManager.StartAsync` marks an iteration `Completed` with message `Submitted` immediately after `SubmitExperimentAsync`, and marks the campaign `Completed` after the loop; it has no pending-external-evidence state or ingress reconciliation path.
- **Source**: `Helix.Orchestration.SiteServer/Campaigns/CampaignManager.cs:72-136` (opened).
- **Confidence**: high
- **Why it matters here**: The demonstrator needs an additive durable pending-evidence lifecycle with an auditable state transition; it cannot reinterpret the present submitted/completed status as proof of an offline physical outcome or later analytical result.

## Assigned questions — answers

- What durable ingress correlation, deduplication, ordering, and uncertain-physical-outcome contract governs the demonstrator and later analytical-result use? → No existing complete ingress contract was found. Evidence supports an additive PostgreSQL-backed ledger that (1) durably binds immutable campaign/activity correlation to one logical source event; (2) atomically records the deduplication marker, lifecycle/audit update, payload hash, and outcome; (3) accepts an identical repeat as the already-recorded outcome while rejecting a mismatched target or payload; (4) applies only an expected lifecycle transition or monotonic version so stale updates cannot regress state; and (5) records an unacknowledged/offline physical action as `uncertain`, reconciled or manually resolved without automatic repeat. On Agent recovery all in-memory leases/global/workflow state are non-authoritative; later commands must re-enter the existing Agent lease, capability, validation, safety, and audit path. Sources: findings above; `Helix.Orchestration.Core/Leasing/InMemoryDeviceLeasingService.cs:5-12` and `Helix.Orchestration.Core/State/WorkflowStateStore.cs:5-19` (opened).

## Dead ends

- Agent endpoint mapping — `Helix.Orchestration.Agent/Program.cs:313-324` was opened; it shows gRPC, health, workflow, runspace, and device mappings in the inspected range, but no evidence-ingress contract. It is not evidence that no endpoint exists elsewhere, so no absence claim is based on it.
