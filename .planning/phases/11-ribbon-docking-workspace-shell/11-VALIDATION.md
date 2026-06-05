---
phase: "11"
type: validation
status: defined
---

# Phase 11: Ribbon + Docking Workspace Shell — Validation Architecture

## Validation Strategy

Phase 11 is a UI-only phase (helix-desktop only). Backend is unchanged. All new code must have Vitest unit/component tests. 76 pre-existing tests must continue to pass.

---

## Test Coverage Map

| Requirement | Test File | Test Assertions |
|-------------|-----------|-----------------|
| H1 – Ribbon chrome, 7 tabs, Home+Devices wired | `src/shell/Ribbon.test.tsx` | Tab buttons render; Home band shows Execution + Workspace groups; Devices band renders; others show placeholder |
| H2 – Activity bar, 2px indicator | `src/shell/ActivityBar.test.tsx` | 5 icon buttons render; active indicator class applied; Settings is bottom-anchored |
| H3 – Split layout 220/flex/200/140 | `src/shell/WorkspaceLayout.test.tsx` | DEFAULT_LAYOUT contains left panel 220px, right panel 200px, bottom panel 140px; DockLayout receives defaultLayout |
| H4 – Dockable panels | `src/shell/WorkspaceLayout.test.tsx` | DockLayout component mounts; saveTab/loadTab callbacks registered; no test of native rc-dock drag (runtime-only) |
| H5 – Document Tabs | `src/shell/WorkspaceLayout.test.tsx` | DEFAULT_LAYOUT contains WorkflowEditor and Execution tab IDs in central panel group |
| H6 – Device Explorer → DeviceManualControl tab | `src/shell/panels/DeviceExplorer.test.tsx` | Device item click calls onOpenManualControl with deviceId; component renders device list |
| H7 – Trend Explorer + DnD | `src/shell/panels/TrendExplorer.test.tsx` | Items render per useTrendRegistry output; draggable attr set; onDragStart sets dataTransfer |
| H7 – useTrendRegistry | `src/hooks/useTrendRegistry.test.ts` | Accumulates unique trendNames; deduplicates on re-render; works with empty input |
| H8 – Chart Panel docks | `src/shell/panels/ChartPanel.test.tsx` | Drop target onDrop adds trendName to tracked list; ScalarTrendChart receives correct data |
| H9 – Log/Output dock | `src/shell/panels/LogOutput.test.tsx` | Renders SafetyPanel events; renders runspaceEvents; no events = empty state message |
| H10 – Workspace save/restore | `src/hooks/useWorkspaceLayouts.test.ts` | saveLayout persists via LazyStore; loadLayouts retrieves by name; listLayoutNames returns keys |
| H10 – Ribbon Save/Load | `src/shell/Ribbon.test.tsx` | Save button calls onSaveLayout with input value; Load dropdown options = savedLayoutNames |
| H11 – CSS variables | All CSS files | No hardcoded hex values; `var(--color-*)` used throughout; helix-dock.css imported before custom styles |
| 76 pre-existing tests | All prior test files | All pass unchanged (regression gate) |
| Clean build | Build step | `npx tsc --noEmit` exits 0; `npm run build` succeeds |

---

## Hook Test Specifications

### useTrendRegistry.test.ts
```typescript
// RED: trendNames is empty on mount
// RED: accumulates unique trendNames from trendValues stream
// RED: does not duplicate trendNames already seen
// RED: handles batch update (multiple new trendNames at once)
// GREEN: implement useTrendRegistry with Set<string> accumulation (RESEARCH.md pattern)
```

### useWorkspaceLayouts.test.ts
```typescript
// RED: saveLayout(name, layout) calls store.set + store.save
// RED: loadLayout(name) calls store.get and returns LayoutBase | null
// RED: listLayoutNames() returns array of stored layout names
// GREEN: implement useWorkspaceLayouts
```

---

## Mocking Requirements

| Dependency | Mock strategy |
|------------|---------------|
| `rc-dock` DockLayout | `vi.mock('rc-dock')` — class component with spied `saveLayout`/`loadLayout` on prototype |
| `@tauri-apps/plugin-store` LazyStore | `vi.hoisted()` + `vi.mock(...)` with `mockSet`/`mockSave`/`mockGet` fakes (ConnectionConfig.test.tsx pattern) |
| `useAgentWebSocket` | `vi.mock('../hooks/useAgentWebSocket')` returning controlled state |
| `useAgentHealth` | `vi.mock('../hooks/useAgentHealth')` returning `{ healthy: null, error: null }` |
| `DeviceRegistry` component | Mocked to return controlled device list |

---

## Regression Gate

Before Plan 04 cutover, confirm baseline:
```bash
cd helix-desktop && npm test -- --run
```
Expected: 76 tests pass. If any fail before App.tsx is touched, STOP and report.

After cutover:
```bash
cd helix-desktop && npm test -- --run --reporter=verbose 2>&1 | tail -20 && npx tsc --noEmit && npm run build
```
Expected: (76 + new shell/panel/App tests) pass; tsc exits 0; build succeeds.

---

## Files Added in Phase 11

| File | Plan | Test file |
|------|------|-----------|
| `src/shell/HelixShell.tsx` | 11-02 | `HelixShell.test.tsx` |
| `src/shell/Ribbon.tsx` | 11-02 | `Ribbon.test.tsx` |
| `src/shell/ActivityBar.tsx` | 11-02 | `ActivityBar.test.tsx` |
| `src/shell/StatusBar.tsx` | 11-02 | `StatusBar.test.tsx` |
| `src/shell/WorkspaceLayout.tsx` | 11-02 | `WorkspaceLayout.test.tsx` |
| `src/shell/panels/DeviceExplorer.tsx` | 11-03 | `DeviceExplorer.test.tsx` |
| `src/shell/panels/TrendExplorer.tsx` | 11-03 | `TrendExplorer.test.tsx` |
| `src/shell/panels/ChartPanel.tsx` | 11-03 | `ChartPanel.test.tsx` |
| `src/shell/panels/LogOutput.tsx` | 11-03 | `LogOutput.test.tsx` |
| `src/hooks/useTrendRegistry.ts` | 11-01 | `useTrendRegistry.test.ts` |
| `src/hooks/useWorkspaceLayouts.ts` | 11-01 | `useWorkspaceLayouts.test.ts` |
| `src/styles/helix-dock.css` | 11-01 | (visual — no unit test) |
| `src/App.tsx` (rewritten) | 11-04 | `App.test.tsx` (updated) |
