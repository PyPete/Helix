---
phase: 11-ribbon-docking-workspace-shell
plan: 02
subsystem: ui
tags: [rc-dock, react, shell, ribbon, activitybar, statusbar, tdd, vitest, css-variables]

# Dependency graph
requires:
  - phase: 11-ribbon-docking-workspace-shell
    plan: 01
    provides: rc-dock@3.3.2, helix-dock.css, useTrendRegistry, useWorkspaceLayouts

provides:
  - WorkspaceLayout — DockLayout wrapper with DEFAULT_LAYOUT, TAB_REGISTRY, forwardRef handle
  - Ribbon — 7-tab chrome with Home/Devices wired, other tabs placeholder
  - ActivityBar — 40px nav with 2px left-edge active indicator
  - StatusBar — 22px status strip with connection/health display
  - HelixShell — composition root lifting all shell state
  - shell.css — outer shell structural CSS

affects:
  - 11-03 (panels replace TAB_REGISTRY stubs)
  - 11-04 (App.tsx rewritten to mount HelixShell)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - forwardRef + useImperativeHandle for DockLayout class component ref exposure
    - TAB_REGISTRY factory pattern — (props) => TabData closures for Plan 03 substitution
    - LayoutBase cast to LayoutData for DockLayout.defaultLayout prop compatibility
    - vi.fn(forwardRef) pattern for mocking class component DockLayout in Vitest
    - TDD RED/GREEN for HelixShell smoke test

key-files:
  created:
    - helix-desktop/src/shell/WorkspaceLayout.tsx
    - helix-desktop/src/shell/Ribbon.tsx
    - helix-desktop/src/shell/Ribbon.css
    - helix-desktop/src/shell/ActivityBar.tsx
    - helix-desktop/src/shell/ActivityBar.css
    - helix-desktop/src/shell/StatusBar.tsx
    - helix-desktop/src/shell/StatusBar.css
    - helix-desktop/src/shell/HelixShell.tsx
    - helix-desktop/src/shell/shell.css
    - helix-desktop/src/shell/HelixShell.test.tsx

key-decisions:
  - "WorkspaceLayout uses React.forwardRef to expose saveLayout/loadLayout/openChartPanel handle to HelixShell"
  - "DEFAULT_LAYOUT cast as LayoutData for DockLayout.defaultLayout prop — LayoutBase is structurally compatible but the prop type is LayoutData"
  - "useTrendRegistry deferred from HelixShell (TypeScript noUnusedLocals blocks unused hook calls) — wired in Plan 03 when TrendExplorer panel is created"
  - "StatusBar.css uses var(--color-success) and var(--color-danger) without fallback hex — keeps zero-hardcoded-hex invariant clean"
  - "ConnectionConfig rendered inside Settings band (when activeRibbonTab === Settings) per D-04 — no modal needed"
  - "DockLayout mock uses React.forwardRef function component (not class constructor mock) — avoids this-is-undefined error in Vitest class component rendering"

# Metrics
duration: ~25min
completed: 2026-06-04
---

# Phase 11 Plan 02: Wave 1 Shell Skeleton Summary

**WorkspaceLayout (DockLayout + 6-tab TAB_REGISTRY + forwardRef handle), Ribbon (7 tabs, Home+Devices wired), ActivityBar (2px left-edge indicator), StatusBar (22px connection display), and HelixShell composition root — 9 files, 5 smoke tests green, TypeScript clean**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-06-04
- **Completed:** 2026-06-04
- **Tasks:** 3/3
- **Files modified:** 10 (9 created + 1 CSS fix)

## Accomplishments

- Created `WorkspaceLayout.tsx` with DockLayout wrapper, `DEFAULT_LAYOUT` (left 220px, center 600px, right 200px, bottom 140px), `TAB_REGISTRY` with 6 stub factories, and `forwardRef` handle exposing `saveLayout`, `loadLayout`, `openChartPanel`
- Created `Ribbon.tsx` with 7 tab buttons (Home/Devices/Trends/Workflows/Charts/Developer/Settings), Home band with Execution and Workspace groups (Run/Pause/Abort + Save/Load layout), Devices band with Manual Control button, other tabs with placeholder bands
- Created `ActivityBar.tsx` with 40px nav, 2px left-edge active indicator via CSS `::before`, top explorer+trends icons, bottom settings icon
- Created `StatusBar.tsx` with 22px footer showing connection dot (healthy/unhealthy/connecting) and agentUrl
- Created `HelixShell.tsx` as composition root: lifts connection, health, WebSocket state, execution state; wires Ribbon callbacks, layout save/load, DockLayout ref
- Created `shell.css` with structural CSS for full-height flex layout (`html/body/#root height: 100%`, `.helix-shell`, `.workspace`) — T-11-06 height-collapse mitigation
- All 5 HelixShell smoke tests pass (TDD GREEN); 88 total tests pass; 4 pre-existing Phase 6.2 DnD failures unchanged
- TypeScript compiles with zero errors across all shell files
- All CSS files use `var(--color-*)` tokens exclusively — zero hardcoded hex values

## Task Commits

1. **Task 11-1-001: WorkspaceLayout** — `c41712c` (feat)
2. **Task 11-1-002: Ribbon + ActivityBar + StatusBar** — `4a0ad19` (feat)
3. **Task 11-1-003 RED: HelixShell smoke tests** — `5fae61d` (test)
4. **Task 11-1-003 GREEN: HelixShell + shell.css** — `fbd2bb8` (feat)
5. **Task 11-1-003 FIX: StatusBar.css hex cleanup** — `0decc38` (fix)

## Files Created/Modified

- `helix-desktop/src/shell/WorkspaceLayout.tsx` — DockLayout wrapper, DEFAULT_LAYOUT, TAB_REGISTRY (6 stubs), forwardRef handle
- `helix-desktop/src/shell/Ribbon.tsx` — 7-tab ribbon with Home/Devices wired, RibbonTab type, all data-testid attributes
- `helix-desktop/src/shell/Ribbon.css` — Tab strip + band chrome styles, all var(--color-*) tokens
- `helix-desktop/src/shell/ActivityBar.tsx` — 40px nav with active state via CSS class
- `helix-desktop/src/shell/ActivityBar.css` — `.abtn.active::before` 2px left-edge indicator
- `helix-desktop/src/shell/StatusBar.tsx` — 22px footer with status dot and agentUrl
- `helix-desktop/src/shell/StatusBar.css` — `.statusbar` + `.status-dot` variants, all var(--color-*) tokens
- `helix-desktop/src/shell/HelixShell.tsx` — Composition root, full shell chrome, state lifting
- `helix-desktop/src/shell/shell.css` — Structural layout CSS for html/body/#root/.helix-shell/.workspace
- `helix-desktop/src/shell/HelixShell.test.tsx` — 5 smoke tests with rc-dock/LazyStore/hooks mocks

## Decisions Made

- `WorkspaceLayout` uses `React.forwardRef` to expose the `WorkspaceLayoutHandle` to `HelixShell` — chosen because DockLayout is a class component ref, `forwardRef` + `useImperativeHandle` is the correct React pattern for exposing imperative methods up the component tree
- `DEFAULT_LAYOUT` typed as `LayoutBase` and cast to `LayoutData` at the DockLayout prop — rc-dock's `defaultLayout` prop type is `LayoutData` but accepts `LayoutBase` structure; the cast is type-safe because rc-dock materialises `BoxBase` → `BoxData` internally
- `useTrendRegistry` not called in `HelixShell` Wave 1 — TypeScript `noUnusedLocals` prevents keeping an unused hook result; the hook call will be added when `TrendExplorer` panel is wired in Plan 03
- DockLayout mock uses `React.forwardRef` function component approach — the class constructor `vi.fn(function(this...) {...})` pattern fails in Vitest because `this` is undefined when React renders the component; `forwardRef` approach populates the ref correctly

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] DockLayout mock class constructor fails in Vitest**
- **Found during:** Task 11-1-003 (HelixShell test RED phase)
- **Issue:** The plan specified `vi.fn(function (this: Record<string, unknown>) {...})` for the DockLayout mock. In Vitest with jsdom, `this` is undefined when React invokes the mock component, causing `TypeError: Cannot set properties of undefined (setting 'saveLayout')`
- **Fix:** Replaced class constructor mock with `React.forwardRef` function component mock that populates the ref with stub instance methods directly. This correctly satisfies WorkspaceLayout's `useImperativeHandle` ref.
- **Files modified:** `helix-desktop/src/shell/HelixShell.test.tsx`
- **Commit:** `fbd2bb8`

**2. [Rule 1 - Bug] StatusBar.css used hex fallback values inside var()**
- **Found during:** Post-task verification (verification step 3: grep for hardcoded hex)
- **Issue:** `var(--color-success, #50fa7b)` and `var(--color-danger, #ff5555)` contained hex fallbacks; the plan's must-have "no hardcoded hex" applies to all hex values including those inside var()
- **Fix:** Removed fallback hex values, leaving pure `var(--color-success)` and `var(--color-danger)` — these tokens are defined in the design system theme
- **Files modified:** `helix-desktop/src/shell/StatusBar.css`
- **Commit:** `0decc38`

## Known Stubs

The following TAB_REGISTRY entries are intentional Wave 1 stubs — they will be replaced in Plan 03 when real panel components are created:

| Stub | File | Reason |
|------|------|--------|
| `device-explorer` → `<div data-testid="device-explorer-stub" />` | WorkspaceLayout.tsx | Plan 03 wires DeviceExplorerPanel |
| `trend-explorer` → `<div data-testid="trend-explorer-stub" />` | WorkspaceLayout.tsx | Plan 03 wires TrendExplorerPanel |
| `workflow-editor` → `<div data-testid="workflow-editor-stub" />` | WorkspaceLayout.tsx | Plan 03 wires WorkflowBuilder |
| `execution-panel` → `<div data-testid="execution-panel-stub" />` | WorkspaceLayout.tsx | Plan 03 wires ExecutionPanel |
| `output` → `<div data-testid="output-stub" />` | WorkspaceLayout.tsx | Plan 03 wires LogOutput |
| `safety-log` → `<div data-testid="safety-log-stub" />` | WorkspaceLayout.tsx | Plan 03 wires SafetyPanel |

These stubs are intentional per plan design (Wave 1 → Wave 2 progression). The `loadTab` function provides a safe fallback for any unknown tab IDs (T-11-05 mitigation).

## Threat Flags

None — no new network endpoints, auth paths, file access patterns, or schema changes introduced. This plan is pure frontend shell scaffolding.

## Self-Check: PASSED

| Check | Result |
|-------|--------|
| helix-desktop/src/shell/WorkspaceLayout.tsx | FOUND |
| helix-desktop/src/shell/Ribbon.tsx | FOUND |
| helix-desktop/src/shell/Ribbon.css | FOUND |
| helix-desktop/src/shell/ActivityBar.tsx | FOUND |
| helix-desktop/src/shell/ActivityBar.css | FOUND |
| helix-desktop/src/shell/StatusBar.tsx | FOUND |
| helix-desktop/src/shell/StatusBar.css | FOUND |
| helix-desktop/src/shell/HelixShell.tsx | FOUND |
| helix-desktop/src/shell/shell.css | FOUND |
| helix-desktop/src/shell/HelixShell.test.tsx | FOUND |
| Commit c41712c (WorkspaceLayout) | FOUND |
| Commit 4a0ad19 (Ribbon + ActivityBar + StatusBar) | FOUND |
| Commit 5fae61d (HelixShell test RED) | FOUND |
| Commit fbd2bb8 (HelixShell GREEN) | FOUND |
| Commit 0decc38 (StatusBar CSS fix) | FOUND |
| TypeScript: zero errors | PASSED |
| No hardcoded hex in shell/*.css | PASSED |
| 5 HelixShell smoke tests pass | PASSED |
| 88 total tests pass (4 pre-existing failures unchanged) | PASSED |

---
*Phase: 11-ribbon-docking-workspace-shell*
*Completed: 2026-06-04*
