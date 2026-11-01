---
phase: 02-scientific-setup-foundation
plan: "27"
subsystem: scientific-desktop-recovery
tags: [typescript, react, vitest, rc-dock, scientific-records]
requires: [02-26]
provides: [user-scoped-operation-recovery, accessible-evidence-controls, stable-scientific-tabs]
affects: [APP-01, APP-02, APP-03, APP-04, SAMP-01, SAMP-02]
tech-stack:
  added: []
  patterns: [agent-reconciled-operation-recovery, stable-dock-tab-factories, accessible-evidence-dialog]
key-files:
  created:
    - helix-desktop/src/hooks/useScientificOperation.ts
    - helix-desktop/src/components/ScientificSetup/ScientificRecordUi.tsx
  modified:
    - helix-desktop/src/shell/WorkspaceLayout.tsx
    - helix-desktop/src/shell/HelixShell.tsx
    - helix-desktop/src/shell/ActivityBar.tsx
decisions:
  - Local operation recovery retains only user-scoped caller intent and reconciles through the Agent before reporting a commit.
  - All five scientific entries use stable dock IDs and recreate their current factory content after layout restoration.
metrics:
  duration: 2min
  completed: 2026-11-01
  tasks: 2
status: complete
plan_head_before: fbc6b530e22a8f295ac7386fde1809a1b5cf1ff9
plan_head_after: 6843d4eb141d7b5ddfff76d036224a5b43692fba
commits: 4
actuals:
  tokens: 4500
  tasks: 2
  commits: 4
requirements-completed: [APP-01, APP-02, APP-03, APP-04, SAMP-01, SAMP-02]
---

# Phase 02 Plan 27: Scientific Recovery and Workspace Resilience Summary

Scientific operation intent now survives a user-scoped shell remount without becoming local authority, and every scientific entry has a stable dock identity that can be restored and refreshed.

## Accomplishments

- Added a user-isolated session recovery hook that keeps original operation key, kind, and request context without credentials; only an authenticated Agent receipt can transition it to committed.
- Added shared scientific evidence UI with textual status, full-ID dialog, keyboard focus restoration, and copy control.
- Registered the missing Apparatus entry and refreshed all five scientific tab factories through the existing `updateTab` lifecycle.

## Task Commits

1. **Task 1: Persist unresolved intent and share accessible truthful evidence controls** — `b477017`, `498b1fd`
2. **Task 2: Preserve all existing scientific entry points across close/restore/session changes** — `5a55860`, `6843d4e`

## Verification

- `npm --prefix helix-desktop test -- --run src/hooks/useScientificOperation.test.ts src/components/ScientificSetup/ScientificRecordUi.test.tsx src/shell/WorkspaceLayout.test.tsx src/shell/HelixShell.test.tsx` — passed: 4 files, 10 tests.
- `npm --prefix helix-desktop run build` — blocked by pre-existing TypeScript errors in `ScientificSetup.tsx` and `scientificClient.ts`; focused plan tests passed.

## TDD Evidence

- **RED:** The evidence-control test failed because no `View full ID` button existed; the shell test failed because Apparatus was absent from the saved layout and activity bar.
- **GREEN:** Added the evidence dialog/control and the stable Apparatus dock registration; focused suites pass.
- **REFACTOR:** Reused the existing `tabRegistry` and `updateTab` pattern; no separate cleanup commit was required.

## Decisions Made

- Recovered local context is never treated as scientific evidence or success; reconciliation remains a read-only Agent lookup.
- Restored dock tabs use existing stable IDs and current factory props, preserving the current shell rather than authorizing a redesign.

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED

- Created recovery hook and shared evidence UI files exist.
- Task commits `b477017`, `498b1fd`, `5a55860`, and `6843d4e` are present in HEAD ancestry.
