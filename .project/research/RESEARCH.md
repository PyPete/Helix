# Research Handoff

Phase: research
Status: complete
Intent: `.project/intent/INTENT.md`

## Dispatch

- `domain` — skipped → none — The milestone has no unresolved practitioner terminology or domain-rule question beyond the bounded Agent contract.
- `stack` — dispatched → `.project/research/evidence-stack.md` — Establish whether the existing Agent/Cleipnir/PostgreSQL primitives can provide the required durable activity, external wait, idempotency, and restart behavior without a second orchestration engine.
- `pitfalls` — dispatched → `.project/research/evidence-pitfalls.md` — Identify failure modes in the mapped mix of durable workflow state and in-memory lease/runtime services that can violate recovery, deduplication, or authority constraints.
- `similar` — skipped → none — Comparable-product research would not answer an unresolved question distinct from the stack and reliability evidence.

## Question assignments

- `` [RESEARCH] What durable ingress correlation, deduplication, ordering, and uncertain-physical-outcome contract governs the demonstrator and later analytical-result use? `` → `stack`, `pitfalls`
