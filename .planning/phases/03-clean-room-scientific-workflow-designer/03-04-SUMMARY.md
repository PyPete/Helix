---
phase: 03-clean-room-scientific-workflow-designer
plan: 04
subsystem: ui
tags: [react, tauri, appdata, proposals, accessibility, vitest]

# Dependency graph
requires:
  - phase: 03-03
    provides: source-bounded workflow board and evidence-only Agent presentation
provides:
  - explicit local proposal creation/editing and exact current-user AppData persistence
  - serialized schema-validated proposal writes, scoped discard, and explicit restart resume
  - typed revision/route/procedure context resolution with Save, Discard, and Keep editing
  - accessible dirty-loss disclosure, focus-managed decisions, and keyboard shortcuts
affects: [03-05-installed-tauri-uat]

# Actuals (#2632); tokens are chars/4 over the realized diff from the persisted plan ledger.
actuals:
  tokens: 29814.5
  tasks: 3
  commits: 11
plan_head_before: f44cb24164c409cd9960aa5eb4ae50b8884bdd29
plan_head_after: 227bf5e29ad0a69e6594fa593d427201a48eba84
commits: 11

# Tech tracking
tech-stack:
  added: []
  patterns:
    - one renderer-level AppData proposal store with per-proposal serialized writes
    - canonical JSON object-key comparison preserves array ordering
    - explicit local editing permission is separate from Agent health and session state

key-files:
  created:
    - helix-desktop/src/lib/scientificWorkflowProposalStore.ts
    - helix-desktop/src/hooks/useProposalEditability.ts
    - helix-desktop/src/hooks/useDraftContextResolver.ts
    - helix-desktop/src/components/ScientificWorkflowBoard/DraftContextDialog.tsx
  modified:
    - helix-desktop/src/components/ScientificWorkflowBoard/ScientificWorkflowBoard.tsx
    - helix-desktop/src/components/ScientificWorkflowBoard/WorkflowStructure.tsx
    - helix-desktop/src/components/ScientificWorkflowBoard/ScientificWorkflowBoard.test.tsx
    - helix-desktop/src/components/ScientificWorkflowBoard/ScientificWorkflowBoard.a11y.test.tsx
    - helix-desktop/src/components/ScientificWorkflowBoard/DraftContextResolver.test.tsx
    - helix-desktop/src/lib/scientificWorkflowProposalStore.test.ts

key-decisions:
  - "Explicit Create/Edit Proposal intent alone enables local proposal mutation; Agent health/session remains evidence-only."
  - "Context changes require exact local Save acknowledgement or scoped deletion/absence confirmation; ordinary navigation bypasses resolution."
  - "Inventory discovery never resumes or applies proposal content without an explicit Resume Proposal action."

requirements-completed: [UX-01]
coverage:
  - id: D1
    description: "Explicit local proposal persistence, restart resume, and typed dirty-context resolution"
    requirement: UX-01
    verification:
      - kind: integration
        ref: "helix-desktop/src/components/ScientificWorkflowBoard/ScientificWorkflowBoard.test.tsx"
        status: pass
      - kind: unit
        ref: "helix-desktop/src/lib/scientificWorkflowProposalStore.test.ts"
        status: pass
      - kind: unit
        ref: "helix-desktop/src/components/ScientificWorkflowBoard/DraftContextResolver.test.tsx"
        status: pass
    human_judgment: false
  - id: D2
    description: "Accessible dirty-loss disclosure, keyboard parity, and proposal-only node/edge editing"
    requirement: UX-01
    verification:
      - kind: automated_ui
        ref: "helix-desktop/src/components/ScientificWorkflowBoard/ScientificWorkflowBoard.a11y.test.tsx"
        status: pass
      - kind: unit
        ref: "helix-desktop/src/components/ScientificWorkflowBoard/WorkflowStructure.test.tsx"
        status: pass
    human_judgment: false

# Metrics
duration: 13min
completed: 2026-11-03
status: complete
---

# Phase 3 Plan 4: Local Proposal Editing and Context Resolution Summary

**Local-only workflow proposals now persist through exact AppData read-back, resume only by explicit selection, and block typed context changes until an acknowledged Save or scoped Discard.**

## Performance

- **Duration:** 13 min in this continuation session
- **Started:** 2026-11-03T11:03:00Z
- **Completed:** 2026-11-03T11:16:03Z
- **Tasks:** 3
- **Files modified:** 16 planned artifacts plus deferred build-warning note

## Accomplishments

- Added an isolated `scientific-workflow-proposals` AppData namespace with strict local-only schema validation, canonical content matching, serialized per-ID writes, exact read-back acknowledgement, retry idempotence, stale-write conflict protection, and scoped deletion confirmation.
- Added explicit Create/Edit and Resume Proposal paths independent of Agent health/session; inventory discovery does not apply or execute proposals.
- Added typed revision/route/procedure context resolution, dirty Save/Discard/Keep editing decisions, delayed-ack protection, and ordinary-navigation bypass.
- Added proposal-only node and edge mutation, accessible unsaved-loss disclosure, keyboard shortcuts, live status/error announcements, dialog focus handling, and authority-boundary regression coverage.

## Task Commits

1. **Task 1: Trace explicit local proposal creation through exact AppData save acknowledgement**
   - `256b641` — failing local proposal intent test (RED)
   - `87b1813` — explicit local proposal save implementation (GREEN)
2. **Task 2: Resolve typed context requests with Save, scoped Discard, and Keep editing**
   - `e6e305b` — typed context request RED test
   - `76a2d10` — strict schema RED test and accepted evidence
   - `1861a11` — typed resolver, strict store, and board integration
3. **Task 3: Complete proposal-only editing and keyboard/screen-reader parity**
   - `afd4177` — accessible proposal editing and keyboard flows
   - `227bf5e` — reject a mismatched acknowledgement document

## Verification

- Task 1 targeted tests: 2 files, 14 tests passed; tracer verification passed.
- Task 2 targeted tests: 3 files, 35 tests passed.
- Task 3 targeted tests: 2 files, 5 tests passed.
- Final desktop production build passed (`tsc && vite build`).
- Final desktop suite passed: **48 files, 227 tests**.
- The build emits a non-blocking Vite bundle-size advisory; it is recorded in `deferred-items.md` and was not expanded into chunk-architecture work.

## Files Created/Modified

- `helix-desktop/src/lib/scientificWorkflowProposalStore.ts` — strict AppData store and exact acknowledgement contract.
- `helix-desktop/src/hooks/useProposalEditability.ts` — local-only edit lifecycle and exact dirty baseline.
- `helix-desktop/src/hooks/useDraftContextResolver.ts` — typed context state machine and acknowledgement-gated transitions.
- `helix-desktop/src/components/ScientificWorkflowBoard/DraftContextDialog.tsx` — accessible pending-draft resolution dialog.
- `helix-desktop/src/components/ScientificWorkflowBoard/ScientificWorkflowBoard.tsx` — local proposal UI, inventory/resume, typed context picker, and keyboard routes.
- `helix-desktop/src/components/ScientificWorkflowBoard/WorkflowStructure.tsx` — proposal-only node/edge editing with source evidence remaining read-only.
- `helix-desktop/src/lib/scientificWorkflowProposalStore.test.ts` — serialized storage, schema, retry, conflict, and deletion tests.
- `helix-desktop/src/components/ScientificWorkflowBoard/DraftContextResolver.test.tsx` — target retention, exact Save/Discard, failures, and focus tests.
- `helix-desktop/src/components/ScientificWorkflowBoard/ScientificWorkflowBoard.test.tsx` and `ScientificWorkflowBoard.a11y.test.tsx` — board integration, resume, keyboard, disclosure, and authority-boundary tests.
- `helix-desktop/src/hooks/useProposalEditability.test.ts` and `WorkflowStructure.test.tsx` — revision/baseline and structure tests.
- `.planning/phases/03-clean-room-scientific-workflow-designer/deferred-items.md` — existing bundle-size advisory.

## Decisions Made

- Explicit local Create/Edit intent controls proposal editing; health/session state does not grant or revoke that permission.
- Only a typed revision, route, or procedure request enters the resolver; Save/Discard target application follows exact local acknowledgement.
- Saved inventory remains separate from active board context until the user explicitly selects Resume Proposal.
- Local proposal IDs and context values remain renderer intent, not scientific identities or evidence.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Validate the complete returned save document at the hook boundary**
- **Found during:** Task 3 final contract review
- **Issue:** The hook checked acknowledgement scalar fields/content but accepted a differently identified or versioned `document`, which could establish a false dirty baseline.
- **Fix:** Validate the returned document's ID, schema version, content revision, and canonical content against the submitted snapshot; keep the proposal dirty/editable on mismatch.
- **Files modified:** `helix-desktop/src/hooks/useProposalEditability.ts`, `helix-desktop/src/hooks/useProposalEditability.test.ts`
- **Verification:** Full desktop suite and production build passed.
- **Committed in:** `227bf5e`

### Process Deviation

- Task 3 accessibility implementation did not have a separate committed failing test before implementation; the Task 3 accessibility tests were added and passed afterward. Task 1 and Task 2 retain their committed RED/GREEN evidence. This is recorded in `.planning/WINDOWS.md` for follow-up visibility.

**Total deviations:** 1 correctness strengthening; 1 TDD-sequencing deviation. No package installs or authority-scope expansion.

## Issues Encountered

- The desktop build has a non-blocking >500 kB minified-chunk advisory (largest chunk approximately 1,089 kB). It is documented as deferred rather than changing bundle architecture within this plan.
- No new Agent, driver, device, scientific mutation, approval, validation, publication, dispatch, or execution paths were introduced.

## User Setup Required

None.

## Next Phase Readiness

Plan 03-04 is complete and Plan 03-05 was not modified or started. Installed-Tauri visual comparison, keyboard-only/screen-reader evidence, and future explicit scientist disposition remain reserved for Plan 03-05; no such human UAT is claimed here.

---
*Phase: 03-clean-room-scientific-workflow-designer*
*Completed: 2026-11-03*

## Self-Check: PASSED

- All listed output files exist.
- All seven task commits are present in the current history.
- Plan 03-05 remains untouched.
