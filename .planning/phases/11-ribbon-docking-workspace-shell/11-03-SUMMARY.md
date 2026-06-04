---
phase: 11-ribbon-docking-workspace-shell
plan: 03
subsystem: ui
tags: [react, panels, docking, rc-dock, html5-dnd, tdd, vitest, css-variables]

# Dependency graph
requires:
  - phase: 11-ribbon-docking-workspace-shell
    plan: 02
    provides: WorkspaceLayout TAB_REGISTRY stubs, WorkspaceLayoutHandle, DockLayout ref

provides:
  - DeviceExplorer panel (device list + onOpenDevice callback, no inline DeviceManualControl)
  - TrendExplorer panel (draggable items with text/x-trend-name dataTransfer)
  - ChartPanel panel (HTML5 drop target tracking trendName[], ScalarTrendChart render)
  - LogOutput panel (runspace + safety event scrollable log)
  - panels.css (shared structural styles, var(--color-*) tokens only)
  - WorkspaceLayout wired with real panel factories, openDeviceTab, dynamic loadTab

affects:
  - 11-04 (App.tsx rewrite mounts HelixShell which hosts this WorkspaceLayout)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - TDD RED/GREEN for all 4 panel components
    - HTML5 drag-and-drop with text/x-trend-name dataTransfer key (D-08)
    - ChartPanel drop target with isDragOver visual state toggle
    - useTrendRegistry wired in WorkspaceLayout to feed TrendExplorer
    - useMemo tabRegistry with openDeviceTab useCallback in deps array
    - try/catch on dataTransfer.dropEffect assignment (jsdom compatibility)

key-files:
  created:
    - helix-desktop/src/shell/panels/DeviceExplorer.tsx
    - helix-desktop/src/shell/panels/DeviceExplorer.test.tsx
    - helix-desktop/src/shell/panels/TrendExplorer.tsx
    - helix-desktop/src/shell/panels/TrendExplorer.test.tsx
    - helix-desktop/src/shell/panels/ChartPanel.tsx
    - helix-desktop/src/shell/panels/ChartPanel.test.tsx
    - helix-desktop/src/shell/panels/LogOutput.tsx
    - helix-desktop/src/shell/panels/LogOutput.test.tsx
    - helix-desktop/src/shell/panels/panels.css
  modified:
    - helix-desktop/src/shell/WorkspaceLayout.tsx

key-decisions:
  - "ChartPanel wraps e.dataTransfer.dropEffect assignment in try/catch — jsdom does not expose dropEffect as writable, so the assignment throws; safe to silently ignore in tests"
  - "openDeviceTab wrapped in useCallback with [props.baseUrl] deps to include in useMemo tabRegistry deps array without causing unnecessary rebuilds"
  - "useTrendRegistry called inside WorkspaceLayout (not HelixShell) — deferred from Wave 1 due to noUnusedLocals; TrendExplorer now consumes the result"
  - "loadTab uses memoized tabRegistry reference (not static module-level object) so factories capture latest props"

requirements-completed: [H6, H7, H8, H9]

# Metrics
duration: ~5min
completed: 2026-06-04
---

# Phase 11 Plan 03: Wave 2 Panel Implementations Summary

**DeviceExplorer, TrendExplorer, ChartPanel, LogOutput panels created with TDD (15 new tests green), panels.css using var(--color-*) tokens exclusively, and WorkspaceLayout TAB_REGISTRY fully wired with real panel factories + openDeviceTab + dynamic loadTab**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-06-04T14:14:51Z
- **Completed:** 2026-06-04T14:20:02Z
- **Tasks:** 3/3
- **Files modified:** 10 (9 created + 1 modified)

## Accomplishments

- Created `DeviceExplorer.tsx`: polls `/devices` every 5s (setInterval + cleanup), loading/error/list states, fires `onOpenDevice(adapterId)` on click — no inline `DeviceManualControl` (H6 satisfied)
- Created `TrendExplorer.tsx`: renders draggable items with `draggable` attribute and `text/x-trend-name` dataTransfer key (H7 / D-08 satisfied)
- Created `ChartPanel.tsx`: HTML5 drop target tracking `trendName[]`, `isDragOver` visual state, renders `ScalarTrendChart` for first tracked trend with filtered `trendValues` (H8 / D-08 / D-09 satisfied)
- Created `LogOutput.tsx`: renders `runspaceEvents` and triggered `safetyEvents` in monospace scrollable list (H9 satisfied)
- Created `panels.css`: 9 CSS rules using `var(--color-*)` tokens exclusively — zero hardcoded hex
- Updated `WorkspaceLayout.tsx`: all 6 TAB_REGISTRY stubs replaced with real component factories via `useMemo`; `useTrendRegistry` wired for TrendExplorer; `openDeviceTab` added with `useCallback`; `loadTab` handles `chart-*` and `device-control-*` dynamic ids; `openChartPanel` updated to use trendName-based id
- All 15 new tests pass: 5 DeviceExplorer + 3 TrendExplorer + 4 ChartPanel + 3 LogOutput
- TypeScript compiles with zero errors across all new and modified files
- 4 pre-existing Phase 6.2 DnD failures remain unchanged (out of scope)

## Task Commits

Each task committed atomically:

1. **Task 11-2-001 RED: DeviceExplorer failing tests** — `d8cd5f4` (test)
2. **Task 11-2-001 GREEN: Implement DeviceExplorer panel** — `5cb857a` (feat)
3. **Task 11-2-002 RED: TrendExplorer, ChartPanel, LogOutput failing tests** — `303425d` (test)
4. **Task 11-2-002 GREEN: Implement TrendExplorer, ChartPanel, LogOutput + panels.css** — `820f042` (feat)
5. **Task 11-2-003: Wire real panels into WorkspaceLayout TAB_REGISTRY** — `68fe607` (feat)

_TDD tasks have separate test → feat commits per RED/GREEN cycle_

## Files Created/Modified

- `helix-desktop/src/shell/panels/DeviceExplorer.tsx` — Device list panel; click → onOpenDevice(adapterId); setInterval poll; no inline DeviceManualControl
- `helix-desktop/src/shell/panels/DeviceExplorer.test.tsx` — 5 unit tests (loading, list, click, error, no-inline-DMC)
- `helix-desktop/src/shell/panels/TrendExplorer.tsx` — Draggable trend name list; text/x-trend-name dataTransfer; panel-empty state
- `helix-desktop/src/shell/panels/TrendExplorer.test.tsx` — 3 unit tests (empty, draggable, dragstart data)
- `helix-desktop/src/shell/panels/ChartPanel.tsx` — HTML5 drop target; trackedTrends[]; isDragOver toggle; ScalarTrendChart for first trend
- `helix-desktop/src/shell/panels/ChartPanel.test.tsx` — 4 unit tests (render, drop, initial trend, drag-over)
- `helix-desktop/src/shell/panels/LogOutput.tsx` — Runspace + triggered safety event list; monospace styling via panels.css
- `helix-desktop/src/shell/panels/LogOutput.test.tsx` — 3 unit tests (testid, runspace events, safety triggered filter)
- `helix-desktop/src/shell/panels/panels.css` — 9 CSS rules; panel-empty, trend-list, trend-item, chart-panel, drag-over, log-list, log-entry variants; zero hardcoded hex
- `helix-desktop/src/shell/WorkspaceLayout.tsx` — Real panel factories, useTrendRegistry, openDeviceTab, dynamic loadTab, updated WorkspaceLayoutHandle

## Decisions Made

- `ChartPanel.tsx` wraps `e.dataTransfer.dropEffect = 'copy'` in `try/catch` — jsdom does not expose `dropEffect` as a writable property, causing the isDragOver test to throw; the try/catch is a jsdom compatibility shim with no effect in real browser environments
- `openDeviceTab` defined with `useCallback([props.baseUrl])` so it can be included in the `useMemo` `tabRegistry` deps array cleanly — prevents stale closure without causing unnecessary memo rebuilds
- `useTrendRegistry` called inside `WorkspaceLayout` rather than `HelixShell` — this was deferred in Wave 1 due to TypeScript `noUnusedLocals`; Plan 03 provides the `TrendExplorer` consumer, making the call valid
- `loadTab` reads from the memoized `tabRegistry` (not a module-level constant) so factories close over the latest `props` values when panels are restored from a saved layout

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] dataTransfer.dropEffect assignment throws in jsdom**
- **Found during:** Task 11-2-002 GREEN (ChartPanel isDragOver test)
- **Issue:** `e.dataTransfer.dropEffect = 'copy'` throws `TypeError: Cannot set property dropEffect` in jsdom because the `DataTransfer` interface in jsdom defines `dropEffect` as read-only
- **Fix:** Wrapped the assignment in `try { e.dataTransfer.dropEffect = 'copy'; } catch { /* jsdom */ }` — harmless in browsers where `dropEffect` is writable
- **Files modified:** `helix-desktop/src/shell/panels/ChartPanel.tsx`
- **Commit:** `820f042`

## Known Stubs

None — all 6 TAB_REGISTRY entries now render real component content. The `unknown-panel` fallback in `loadTab` remains as a safety net for unrecognised tab IDs (T-11-05 mitigation), but it is not a visible stub in normal operation.

## Threat Flags

None — no new network endpoints, auth paths, or schema changes. Panel components receive data via props from the shell; trendName from dataTransfer is used only as a React string prop (T-11-07: no eval, no innerHTML — React escapes all string children). adapterId from the /devices API is used as string key and DeviceManualControl prop only (T-11-08).

## Self-Check: PASSED

| Check | Result |
|-------|--------|
| helix-desktop/src/shell/panels/DeviceExplorer.tsx | FOUND |
| helix-desktop/src/shell/panels/DeviceExplorer.test.tsx | FOUND |
| helix-desktop/src/shell/panels/TrendExplorer.tsx | FOUND |
| helix-desktop/src/shell/panels/TrendExplorer.test.tsx | FOUND |
| helix-desktop/src/shell/panels/ChartPanel.tsx | FOUND |
| helix-desktop/src/shell/panels/ChartPanel.test.tsx | FOUND |
| helix-desktop/src/shell/panels/LogOutput.tsx | FOUND |
| helix-desktop/src/shell/panels/LogOutput.test.tsx | FOUND |
| helix-desktop/src/shell/panels/panels.css | FOUND |
| helix-desktop/src/shell/WorkspaceLayout.tsx | FOUND |
| Commit d8cd5f4 (test: DeviceExplorer RED) | FOUND |
| Commit 5cb857a (feat: DeviceExplorer GREEN) | FOUND |
| Commit 303425d (test: TrendExplorer/ChartPanel/LogOutput RED) | FOUND |
| Commit 820f042 (feat: panels GREEN) | FOUND |
| Commit 68fe607 (feat: WorkspaceLayout wired) | FOUND |
| TypeScript: zero errors | PASSED |
| No hardcoded hex in panels.css | PASSED |
| 5 DeviceExplorer tests pass | PASSED |
| 3 TrendExplorer tests pass | PASSED |
| 4 ChartPanel tests pass | PASSED |
| 3 LogOutput tests pass | PASSED |
| 103 total tests pass (4 pre-existing failures unchanged) | PASSED |

---
*Phase: 11-ribbon-docking-workspace-shell*
*Completed: 2026-06-04*
