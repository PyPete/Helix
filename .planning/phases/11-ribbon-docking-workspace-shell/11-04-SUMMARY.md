---
phase: 11-ribbon-docking-workspace-shell
plan: 04
subsystem: ui
tags: [react, app-entry-point, tdd, vitest, typescript, vite, helix-shell]

# Dependency graph
requires:
  - phase: 11-ribbon-docking-workspace-shell
    plan: 03
    provides: HelixShell composition root (HelixShell.tsx), all panel implementations, WorkspaceLayout fully wired

provides:
  - App.tsx rewritten as 5-line HelixShell wrapper (D-04 big-bang complete)
  - App.test.tsx smoke tests verifying HelixShell mounts from App entry point
  - TypeScript clean build (tsc + vite build exits 0)
  - Full regression suite: 107/111 tests pass (4 pre-existing Phase 6.2 DnD failures unchanged)

affects:
  - main.tsx (unchanged -- import App from ./App continues to work)
  - Phase 12+ (entry point now correctly delegates to HelixShell; no old flat layout)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - TDD RED/GREEN for App entry point rewrite
    - App.test.tsx mirrors HelixShell.test.tsx mock block (rc-dock + LazyStore + hooks + ConnectionConfig stubs)
    - ConnectionConfig mocked at App.test level to isolate LazyStore module-level side effects

key-files:
  created:
    - helix-desktop/src/App.test.tsx
  modified:
    - helix-desktop/src/App.tsx

key-decisions:
  - "App.tsx rewritten to 5 lines per D-04: import HelixShell and return HelixShell; all old state and imports removed"
  - "ConnectionConfig mocked in App.test.tsx to prevent LazyStore module-level constructor from running in test environment"
  - "App.test.tsx forwardRef mock uses explicit TypeScript parameter types to satisfy strict mode (mirrors HelixShell.test.tsx)"

requirements-completed: [H1, H2, H3, H4, H5, H6, H7, H8, H9, H10, H11]

# Metrics
duration: ~10min
completed: 2026-06-04
---

# Phase 11 Plan 04: Wave 3 App.tsx Cutover Summary

**App.tsx rewritten as a 5-line HelixShell wrapper completing D-04 big-bang migration; 4 new regression-guard smoke tests added; 107/111 tests pass; TypeScript and Vite build clean**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-06-04T15:22:00Z
- **Completed:** 2026-06-04T15:30:00Z
- **Tasks:** 1/1 auto + 1 checkpoint
- **Files modified:** 2

## Accomplishments

- Verified baseline: 103 tests passing before any changes (4 pre-existing Phase 6.2 DnD failures confirmed and out of scope)
- Created `App.test.tsx` with 4 smoke tests (helix-shell, ribbon-tab-*, activity-bar, status-bar) -- RED phase confirmed all 4 failing because App still rendered old flat layout
- Rewrote `App.tsx` to 5 lines: import HelixShell + export default function App returning HelixShell -- all old imports removed per D-04
- GREEN phase: all 4 new App tests pass; full regression confirms 107 tests passing (4 App + 103 pre-existing = 107)
- TypeScript `tsc --noEmit` exits 0 with zero errors across all helix-desktop/src
- Vite build (`npm run build`) succeeds in 2.68s with zero errors

## Task Commits

1. **Task 11-3-001 RED: App.test.tsx failing tests** -- `bd91b4c` (test)
2. **Task 11-3-001 GREEN: App.tsx rewrite + App.test.tsx type fix** -- `0c29109` (feat)

_TDD tasks have separate test -> feat commits per RED/GREEN cycle_

## Files Created/Modified

- `helix-desktop/src/App.tsx` -- Rewritten from ~60 lines to 5 lines; single import + HelixShell render; all old state and component imports removed per D-04
- `helix-desktop/src/App.test.tsx` -- 4 smoke tests with rc-dock/LazyStore/hooks/ConnectionConfig mocks; mirrors HelixShell.test.tsx mock block adjusted for App entry point import

## Decisions Made

- `ConnectionConfig` mocked at component level in `App.test.tsx` -- ConnectionConfig creates a module-level LazyStore singleton at import time; mocking the whole component is cleaner than mocking LazyStore a second time alongside the existing store mock
- `App.test.tsx` forwardRef mock uses explicit TypeScript annotations to satisfy `strict: true` -- the initial version omitted these and caused 5 type errors caught by tsc --noEmit

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] App.test.tsx forwardRef mock missing TypeScript parameter types**
- **Found during:** Task 11-3-001 GREEN (TypeScript check step)
- **Issue:** Initial App.test.tsx omitted explicit type annotations on forwardRef callback parameters; `npx tsc --noEmit` reported 5 errors (implicitly has any type for _props, ref, and this)
- **Fix:** Rewrote App.test.tsx with explicit annotations matching HelixShell.test.tsx: `_props: Record<string, unknown>`, `ref: React.ForwardedRef<typeof instanceMethods>`, and typed the LazyStore constructor function body
- **Files modified:** `helix-desktop/src/App.test.tsx`
- **Commit:** `0c29109`

---

**Total deviations:** 1 auto-fixed (Rule 1 - TypeScript type annotation)
**Impact on plan:** Minimal -- test file type safety fix only. No scope creep.

## Known Stubs

None -- all TAB_REGISTRY entries are real panel components (from Plan 03). App.tsx is the minimal entry point; HelixShell owns all state and layout.

## Threat Flags

None -- this plan only changes the app entry point to delegate to an already-built component. No new network endpoints, auth paths, file access patterns, or schema changes introduced.

## Self-Check: PASSED

| Check | Result |
|-------|--------|
| helix-desktop/src/App.tsx exists | FOUND |
| helix-desktop/src/App.test.tsx exists | FOUND |
| App.tsx is 5 lines | VERIFIED |
| No old imports in App.tsx (grep activeTab etc.) | ZERO MATCHES |
| Commit bd91b4c (test: RED) | FOUND |
| Commit 0c29109 (feat: GREEN) | FOUND |
| 4 App tests pass | PASSED |
| 107 total tests pass (4 pre-existing unchanged) | PASSED |
| TypeScript: zero errors | PASSED |
| Vite build: succeeds | PASSED |

---
*Phase: 11-ribbon-docking-workspace-shell*
*Completed: 2026-06-04*