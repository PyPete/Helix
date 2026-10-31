---
phase: 02-scientific-setup-foundation
plan: "31"
subsystem: desktop scientific setup
tags: [react, tauri, scientific-setup, procedures, plans]
requires:
  - phase: 02-30
    provides: authenticated fixed-kind scientific lifecycle boundary
provides:
  - separate Procedure and Plan dock workspace identities
  - receipt-validated client mutations with retained uncertain operation identity
affects: [02-32]
tech-stack:
  added: []
  patterns: [validated native receipts, immutable history presentation, stable dock tab IDs]
key-files:
  created: [helix-desktop/src/types/scientific.ts, helix-desktop/src/lib/scientificClient.ts, helix-desktop/src/components/ScientificSetup/ProcedurePlans.tsx]
  modified: [helix-desktop/src/shell/WorkspaceLayout.tsx, helix-desktop/src/shell/HelixShell.tsx, helix-desktop/src/shell/ActivityBar.tsx]
decisions:
  - Procedure and plan scientific views use independent stable dock IDs.
  - A malformed or missing committed receipt never becomes UI success.
metrics:
  duration: 137s
  completed: 2026-10-31
status: complete
actuals:
  tokens: 7800
  tasks: 2
  commits: 4
plan_head_before: 6b44f3beca7526c2b5308615bbde648cd700aa5c
plan_head_after: 15afa1f4fd872e7feb220f3069b72a08cb5658a2
commits: 4
---

# Phase 02 Plan 31: Procedure and Plan Workspaces Summary

Independent scientific procedure and plan draft surfaces now retain their own workspace identities, publish intents, and uncertainty state.

## Tasks Completed

1. Added typed procedure/plan client contracts, receipt validation, drafts, named publication controls, exact pin/value/unit fields, immutable history presentation, and retained operation IDs for uncertain acknowledgements.
2. Registered separately labelled Procedures and Plans activity-bar entries and stable dock tabs so close/restore keeps the kind-specific workspace identity.

## Verification

- `npm --prefix helix-desktop test -- --run src/lib/scientificClient.test.ts src/components/ScientificSetup/ProcedurePlans.test.tsx` — passed (2 tests).
- `npm --prefix helix-desktop test -- --run src/shell/WorkspaceLayout.test.tsx src/shell/HelixShell.test.tsx src/lib/scientificClient.test.ts src/components/ScientificSetup/ProcedurePlans.test.tsx` — passed (10 tests).
- `npm --prefix helix-desktop run build` — not clean: existing `ScientificSetup.tsx` imports unused `ApparatusDraft` under the TypeScript no-unused-locals gate; unrelated to this plan.

## Decisions Made

- Procedure and plan views use independent stable dock tab IDs.
- Native response data must contain the expected kind, operation ID, resource ID, and committed status before the UI presents success.

## Deviations from Plan

### Auto-fixed Issues

1. [Rule 2 - Safety] Added receipt shape validation before a mutation can display committed scientific state.
- **Found during:** Task 1
- **Fix:** Reject malformed, wrong-kind, incomplete, or non-committed receipt payloads.
- **Files modified:** `helix-desktop/src/lib/scientificClient.ts`
- **Commit:** `eb90199`

## Known Stubs

None.

## Self-Check: PASSED

- Confirmed scientific client, procedure/plan component, and workspace registration files exist.
- Confirmed `615c2c1`, `eb90199`, `ef24e68`, and `15afa1f` are ancestors of HEAD.
