---
phase: 02-scientific-setup-foundation
plan: "01"
subsystem: verification, scientific-contract
tags: [powershell, baseline, scientific-setup, approval, provenance]
requires:
  - phase: 01-agent-safety-boundary
    provides: Agent/native/SQL boundaries with explicitly waived acceptance gates
provides:
  - Reproducible, non-vacuous Phase 2 baseline evidence with explicit dispositions
  - Scientist-approved catalog, canonicalization, provenance, and numerical-bound contract
affects: [02-18 tracer, Phase 2 scientific setup, scientific schema stabilization]
tech-stack:
  added: []
  patterns: [baseline evidence is distinct from acceptance evidence, explicit scientific contract approval]
key-files:
  created: [scripts/verification/phase02-baseline.ps1, .planning/phases/02-scientific-setup-foundation/02-BASELINE.md, .planning/phases/02-scientific-setup-foundation/02-CONTRACT-APPROVAL.md]
  modified: []
key-decisions:
  - "Scientist approved the exact proposed Phase 2 catalog, canonical vectors, provenance semantics, and candidate numerical bounds."
  - "The plan-pinned older procedure remains authoritative until explicit adoption; candidate topology size must be reconciled by tracer evidence rather than silently widening bounds."
actuals:
  tokens: 7848
  tasks: 2
  commits: 2
plan_head_before: aac9501738550924c706ad94b9e1000f8d35942f
plan_head_after: 40fd4088b8869ecac2ec3310d1f652421920b7f4
patterns-established:
  - "Baseline reports name command exits, discovery, duration, and disposition without converting waivers into passes."
requirements-completed: [APP-01, APP-02, APP-03, APP-04, SAMP-01, SAMP-02]
coverage:
  - id: D1
    description: Baseline gate reports tool availability, existing command evidence, candidate budgets, and Phase 1 waiver dispositions.
    verification:
      - kind: other
        ref: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verification/phase02-baseline.ps1 -Mode Check
        status: pass
    human_judgment: false
  - id: D2
    description: Exact scientific setup contract is explicitly approved with source hash and retained baseline blockers.
    verification:
      - kind: manual_procedural
        ref: Scientist response "approved" recorded in 02-CONTRACT-APPROVAL.md
        status: pass
    human_judgment: true
    rationale: Scientific semantics and numerical bounds require scientist authority.
duration: 6min
completed: 2026-10-30
status: complete
---

# Phase 02 Plan 01: Scientific Approval and Honest Baseline Preflight Summary

**Scientist-approved scientific setup contract paired with reproducible baseline evidence that preserves Phase 1 waivers and current failures as non-passing evidence.**

## Performance

- **Duration:** 6 min
- **Started:** 2026-10-30T16:05:39.4403255Z
- **Completed:** 2026-10-30T16:11:00Z
- **Tasks:** 2/2
- **Files modified:** 3

## Accomplishments

- Added a PowerShell baseline gate that records tool versions, command exits, test discovery, timing, output tails, and dispositions without following `PGHOST` or accessing a user database.
- Measured representative topology, procedure, and transformation fixtures against candidate bounds; recorded the oversized topology fixture as a visible tracer blocker rather than silently widening a limit.
- Recorded the scientist's explicit approval of the exact proposed catalog, units, canonical vectors, sample provenance, native/API bounds, and plan-pinned procedure rule.

## Task Commits

1. **Task 1: Capture non-vacuous affected baseline and representative budgets** — `c657dc1` (feat)
2. **Task 2: Approve catalog, canonicalization and provenance semantics** — `40fd408` (docs)

## Files Created/Modified

- `scripts/verification/phase02-baseline.ps1` — Runs or validates the no-live-database baseline evidence gate.
- `.planning/phases/02-scientific-setup-foundation/02-BASELINE.md` — Captures exact baseline outcomes and candidate fixture measurements.
- `.planning/phases/02-scientific-setup-foundation/02-CONTRACT-APPROVAL.md` — Records the approval source, contract hash, approved semantics, budgets, and non-approvals.

## Decisions Made

- The scientist approved the exact scientific contract from `02-PLAN-CONTRACT.md` at SHA-256 `67a40715b53e584677f674251378d76391ca379ce75c770142f78b47e0475f10`.
- A plan's existing pinned procedure remains authoritative over a newer procedure until explicit adoption and plan publication.
- The 64 KiB setup-request candidate remains approved but is not yet proven for the maximal topology fixture; 02-18 must prove a bounded representation or obtain an explicit coherent adjustment.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Corrected baseline discovery parsing**
- **Found during:** Task 2
- **Issue:** The initial parser captured a user-directory number from test output and treated Cargo's final zero-test binaries as the entire focused discovery result.
- **Fix:** Parse xUnit totals, Vitest totals, and the maximum Cargo passed count using runner-specific result formats.
- **Files modified:** `scripts/verification/phase02-baseline.ps1`, `02-BASELINE.md`
- **Verification:** Re-ran baseline Run mode, then `-Mode Check` passed.
- **Committed in:** `40fd408`

---

**Total deviations:** 1 auto-fixed (1 Rule 1 bug)
**Impact on plan:** The correction is required for honest nonzero discovery reporting; no scientific implementation scope was added.

## Issues Encountered

- Existing Core, Agent, and desktop test suites contain baseline failures. They are recorded explicitly, remain non-passing evidence, and were not remediated because they predate this preflight task.
- The maximal representative topology fixture measured 141,625 bytes against the approved 64 KiB candidate request limit. This is an unresolved tracer/schema-stabilization concern, not permission to widen the limit.

## Known Stubs

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Wave 2 has an approved scientific contract and an executable baseline gate.
- 02-18 must retain all current baseline dispositions, provide authenticated desktop/native/Agent/isolated-PostgreSQL tracer evidence, and resolve the candidate topology-size tension before schema stabilization.
- Phase 1 waivers and all unexecuted native/SQL acceptance remain distinct from passing Phase 2 evidence.

## Self-Check: PASSED

- Found required baseline script, baseline report, approval record, and both task commits in `HEAD`.

---
*Phase: 02-scientific-setup-foundation*
*Completed: 2026-10-30*
