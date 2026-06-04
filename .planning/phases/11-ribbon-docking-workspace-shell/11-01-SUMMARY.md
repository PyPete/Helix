---
phase: 11-ribbon-docking-workspace-shell
plan: 01
subsystem: ui
tags: [rc-dock, tauri, react, hooks, css-variables, vitest, tdd]

# Dependency graph
requires:
  - phase: phase-6
    provides: TrendValue type from agent.ts used by useTrendRegistry
  - phase: phase-5
    provides: LazyStore pattern from ConnectionConfig.tsx used by useWorkspaceLayouts

provides:
  - rc-dock@3.3.2 pinned in package.json
  - helix-dock.css CSS variable override layer for all rc-dock visual classes
  - useTrendRegistry hook — accumulates unique trendName strings from TrendValue[] stream
  - useWorkspaceLayouts hook — save/load named LayoutBase objects via LazyStore

affects:
  - 11-02 (shell composition root uses useWorkspaceLayouts and helix-dock.css)
  - 11-03 (Trend Explorer panel uses useTrendRegistry)
  - 11-04 (Ribbon home wires Save/Load Layout with useWorkspaceLayouts)

# Tech tracking
tech-stack:
  added:
    - rc-dock@3.3.2 (docking and split-panel library, exact pin)
  patterns:
    - TDD RED/GREEN cycle for all new hooks
    - vi.hoisted() + vi.mock() for LazyStore constructor mocking
    - useRef<Set<string>> to avoid Set in useEffect deps (prevents re-render cascade)
    - module-level LazyStore singleton per store file

key-files:
  created:
    - helix-desktop/src/styles/helix-dock.css
    - helix-desktop/src/hooks/useTrendRegistry.ts
    - helix-desktop/src/hooks/useTrendRegistry.test.ts
    - helix-desktop/src/hooks/useWorkspaceLayouts.ts
    - helix-desktop/src/hooks/useWorkspaceLayouts.test.ts
  modified:
    - helix-desktop/package.json (rc-dock@3.3.2 exact pin)
    - helix-desktop/package-lock.json

key-decisions:
  - "rc-dock pinned as exact 3.3.2 (not ^3.3.2) because npm latest tag points to 4.0.0-alpha.2 with breaking changes"
  - "useWorkspaceLayouts uses separate store file workspace-layouts.json (not helix-config.json) per D-10"
  - "useTrendRegistry uses useRef<Set<string>> accumulator to prevent re-render cascade on every WebSocket frame"

patterns-established:
  - "Pattern: useRef for accumulator Set in streaming hooks — avoids Set in useEffect deps array"
  - "Pattern: module-level LazyStore singleton — matches ConnectionConfig.tsx pattern"
  - "Pattern: vi.hoisted() + vi.mock() for Tauri store constructor mocks in hook tests"

requirements-completed: [H7, H10, H11]

# Metrics
duration: ~5min
completed: 2026-06-04
---

# Phase 11 Plan 01: Wave 0 Foundation Summary

**rc-dock@3.3.2 installed with exact pin, helix-dock.css CSS variable layer (25 overrides, zero hex), useTrendRegistry (Set accumulator, 6 tests green), and useWorkspaceLayouts (LazyStore save/load/list, 5 tests green)**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-06-04T13:55:41Z
- **Completed:** 2026-06-04T14:00:21Z
- **Tasks:** 3/3
- **Files modified:** 7

## Accomplishments

- Installed rc-dock@3.3.2 with exact version pin (not caret) guarding against accidental alpha upgrade
- Created helix-dock.css with 25 rc-dock class overrides mapping all visual classes to var(--color-*) tokens; zero hardcoded hex values
- Implemented useTrendRegistry hook using useRef<Set<string>> accumulator pattern; all 6 TDD tests pass
- Implemented useWorkspaceLayouts hook with saveLayout (merge-not-overwrite), loadLayouts (?? {}), listLayoutNames (sorted); all 5 TDD tests pass
- TypeScript compiles with zero errors on all new files

## Task Commits

Each task was committed atomically:

1. **Task 11-0-001: Install rc-dock@3.3.2 and create helix-dock.css** - `d4969c8` (chore)
2. **Task 11-0-002 RED: Add failing tests for useTrendRegistry** - `3a9f995` (test)
3. **Task 11-0-002 GREEN: Implement useTrendRegistry hook** - `90492bc` (feat)
4. **Task 11-0-003 RED: Add failing tests for useWorkspaceLayouts** - `ff26fe2` (test)
5. **Task 11-0-003 GREEN: Implement useWorkspaceLayouts hook** - `10fcc4f` (feat)

_TDD tasks have separate test → feat commits per RED/GREEN cycle_

## Files Created/Modified

- `helix-desktop/package.json` - Added rc-dock@3.3.2 (exact pin, no caret)
- `helix-desktop/package-lock.json` - Lock file updated with rc-dock@3.3.2 and its 18 transitive deps
- `helix-desktop/src/styles/helix-dock.css` - CSS variable override layer for all rc-dock visual classes
- `helix-desktop/src/hooks/useTrendRegistry.ts` - Accumulates unique trendName strings from TrendValue[] via useRef Set
- `helix-desktop/src/hooks/useTrendRegistry.test.ts` - 6 unit tests (empty, single, dedup, sort, append, stable ref)
- `helix-desktop/src/hooks/useWorkspaceLayouts.ts` - save/load/list LayoutBase via LazyStore("workspace-layouts.json")
- `helix-desktop/src/hooks/useWorkspaceLayouts.test.ts` - 5 unit tests (set+save, merge, load, null-fallback, sorted-keys)

## Decisions Made

- rc-dock@3.3.2 exact pin: npm's `latest` tag currently points to 4.0.0-alpha.2 (breaking API changes); exact pin prevents accidental upgrade when running `npm update`
- useWorkspaceLayouts uses `workspace-layouts.json` as the store file (separate from `helix-config.json`) per D-10, keeping workspace layout persistence isolated from connection config
- useTrendRegistry uses `useRef<Set<string>>` for the accumulator to keep the Set out of the useEffect dependency array, preventing a re-render cascade on every incoming WebSocket frame

## Deviations from Plan

None - plan executed exactly as written. The exact-pin vs caret pin for rc-dock was an explicit must_have in the plan; the npm install defaulted to `^3.3.2` which was corrected immediately.

## Issues Encountered

- `npm install rc-dock@3.3.2` wrote `"rc-dock": "^3.3.2"` (with caret) to package.json automatically. Fixed inline by editing package.json to `"rc-dock": "3.3.2"` before committing. This is npm's default behavior and is not a bug in the plan.
- 4 pre-existing test failures in `DevicePalette.test.tsx` and `WorkflowBuilder.test.tsx` (drag-and-drop tests from Phase 6.2). These are out-of-scope for this plan and were not caused by any changes here. Logged to `deferred-items.md`.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

All three Wave 0 artifacts are ready for downstream plans:
- `helix-dock.css` is available for import in Plan 02 (shell composition root)
- `useTrendRegistry` is ready to be wired in Plan 03 (Trend Explorer panel)
- `useWorkspaceLayouts` is ready to be wired in Plan 04 (Ribbon home Save/Load Layout)
- TypeScript compiles cleanly; no new type errors introduced

## Self-Check: PASSED

All created files found on disk. All 5 task commits present in git log.

| Check | Result |
|-------|--------|
| helix-desktop/src/styles/helix-dock.css | FOUND |
| helix-desktop/src/hooks/useTrendRegistry.ts | FOUND |
| helix-desktop/src/hooks/useTrendRegistry.test.ts | FOUND |
| helix-desktop/src/hooks/useWorkspaceLayouts.ts | FOUND |
| helix-desktop/src/hooks/useWorkspaceLayouts.test.ts | FOUND |
| .planning/phases/11-ribbon-docking-workspace-shell/11-01-SUMMARY.md | FOUND |
| Commit d4969c8 (chore: rc-dock + helix-dock.css) | FOUND |
| Commit 3a9f995 (test: useTrendRegistry RED) | FOUND |
| Commit 90492bc (feat: useTrendRegistry GREEN) | FOUND |
| Commit ff26fe2 (test: useWorkspaceLayouts RED) | FOUND |
| Commit 10fcc4f (feat: useWorkspaceLayouts GREEN) | FOUND |

---
*Phase: 11-ribbon-docking-workspace-shell*
*Completed: 2026-06-04*
