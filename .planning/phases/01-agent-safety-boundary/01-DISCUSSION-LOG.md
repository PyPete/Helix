# Phase 1: Agent Safety Boundary - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-10-29
**Phase:** 1-Agent Safety Boundary
**Areas discussed:** Local control access, Admission outcomes, Restart uncertainty, Command evidence

---

## Local control access

| Decision | Options considered | Selected |
|---|---|---|
| Request path | Desktop-only loopback; local network clients; Site Server proxy | Desktop-only loopback ✓ |
| Scientist identity | Desktop session; Windows OS identity; workstation identity | Desktop session identity ✓ |
| Missing identity | Fail closed; read-only fallback; dev bypass | Fail closed ✓ |
| Protected surface | All APIs; mutations only; control routes only | All Agent APIs ✓ |

**Notes:** The MVP is single-scientist and local; unauthenticated callers are audited and receive no Agent access.

---

## Admission outcomes

| Decision | Options considered | Selected |
|---|---|---|
| Ordinary validation failure | Reject + explain; hold campaign; retry | Reject + explain ✓ |
| Unsafe/unknown state | Hold + require review; reject only; safe-stop + abort | Hold + require review ✓ |
| Explanation | Plain summary + details; technical only; generic warning | Plain summary + details ✓ |
| Admission timing | Before every dispatch; campaign start only; each campaign step | Before every dispatch ✓ |

**Notes:** Immediate runtime revalidation is required even after earlier proposal/compile feedback.

---

## Restart uncertainty

| Decision | Options considered | Selected |
|---|---|---|
| Initial recovery state | Held pending reconciliation; retry; mark failed | Held pending reconciliation ✓ |
| Simulator reconciliation | Query + confirmation; query automatically; attestation only | Query simulator automatically ✓ |
| Mismatch/unreadable state | Hold for review; abort; retry | Hold for review ✓ |
| Successful reconciliation | Resume + audit event; resume silently; acknowledgement | Resume + prominent audit event ✓ |

**Notes:** Automatic recovery is only permitted for a matching simulator observation.

---

## Command evidence

| Decision | Options considered | Selected |
|---|---|---|
| Organization | Campaign timeline; per-device logs; command table | Chronological campaign timeline ✓ |
| Default detail | Outcome-first summary; all fields; minimal status | Outcome-first summary ✓ |
| Operator context | Append annotation; edit event; no annotations | Append annotation ✓ |
| Visual emphasis | Blocks and recovery; all equally; failures only | Blocks and recovery ✓ |

**Notes:** Facts are immutable; annotations and reconciliation records append to the timeline.

---

## the agent's Discretion

- Concrete loopback/session authentication mechanics and exact diagnostic/timeline implementation are open, subject to CONTEXT.md decisions.

## Deferred Ideas

None.
