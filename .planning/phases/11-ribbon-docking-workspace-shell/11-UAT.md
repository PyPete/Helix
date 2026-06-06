---
status: complete
phase: 11-ribbon-docking-workspace-shell
source:
  - 11-01-SUMMARY.md
  - 11-02-SUMMARY.md
  - 11-03-SUMMARY.md
  - 11-04-SUMMARY.md
started: 2026-06-05T00:00:00Z
updated: 2026-06-05T00:00:00Z
---

## Current Test

number: 20
name: App entry point mounts the shell
expected: |
  When helix-desktop starts, App.tsx renders HelixShell as the root — the full VS-style IDE shell is visible on launch.
result: pass

## Tests

### 1. Shell composes all components correctly
expected: The IDE shell fully boots with ribbon (68px, 7 tabs), activity bar (40px left), workspace with docked panels, and status bar (22px bottom) all rendered and interactive.
result: pass

### 2. Ribbon renders with 7 tabs
expected: User sees a ribbon control bar at the top with 7 tab buttons: Home, Devices, Trends, Workflows, Charts, Developer, and Settings.
result: pass

### 3. Home ribbon band with workspace controls
expected: When Home tab is active, user sees a Workspace group with Save Layout and Load Layout buttons.
result: pass

### 4. Home ribbon band with execution controls
expected: When Home tab is active, user sees an Execution group with Run, Pause, and Abort buttons.
result: pass

### 5. Devices ribbon band with manual control
expected: When Devices tab is active, user sees a Manual Control button in the ribbon band.
result: pass

### 6. Activity bar renders with icons
expected: User sees a 40px vertical navigation bar on the left with explorer and trends icons at top, settings icon at bottom.
result: pass

### 7. Activity bar active state indicator
expected: User sees a 2px left-edge highlight indicator on the currently active Activity Bar item.
result: pass

### 8. Status bar displays connection state
expected: User sees a 22px status strip at the bottom showing a connection status dot (healthy/unhealthy/connecting) and the agent URL.
result: pass

### 9. Device Explorer panel displays devices
expected: User sees a Device Explorer panel showing a list of available devices that refreshes every 5 seconds.
result: pass

### 10. Click device in explorer opens control panel
expected: User can click a device in the Device Explorer and a new tab opens to control that device.
result: issue
reported: "device manual control is not styled versus what we planned but looks good"
severity: cosmetic

### 11. Trend Explorer shows draggable trends
expected: User sees a Trend Explorer panel listing trend names as draggable items (draggable=true, data-transfer key: text/x-trend-name).
result: pass

### 12. Drag trend to chart panel
expected: User can drag a trend name from Trend Explorer and drop it into a Chart Panel to display the trend data.
result: blocked
blocked_by: prior-phase
reason: "simulator does not have trends"

### 13. Chart panel drop target highlights
expected: When dragging a trend over the Chart Panel, the panel visually highlights to indicate it is a valid drop target.
result: issue
reported: "cant see chart panel"
severity: major

### 14. Chart panel displays scalar trend chart
expected: After dropping a trend name, the Chart Panel renders a ScalarTrendChart visualization for that trend.
result: issue
reported: "cant see chart panel"
severity: major

### 15. Multiple chart panels can be opened
expected: User can open chart-0, chart-1, chart-2 etc. as separate dock tabs, each independently tracking different trends.
result: issue
reported: "cant see chart panel"
severity: major

### 16. Log output shows execution events
expected: User sees a Log Output panel displaying runspace execution events in a monospace scrollable list.
result: issue
reported: "Log output there, agent is running but no logs"
severity: major

### 17. Log output shows safety events
expected: Triggered safety events appear in the Log Output panel.
result: issue
reported: "Log output there, agent is running but no logs"
severity: major

### 18. Dockable panels can be rearranged
expected: User can drag panel tabs to different dock regions (left, center, right, bottom) and panels remain in the new positions.
result: pass

### 19. Workspace layout persists across sessions
expected: User can click Save Layout, enter a name, and on next session Load Layout restores panels and docking positions.
result: issue
reported: "fails. doesn't save"
severity: major

### 20. App entry point mounts the shell
expected: When helix-desktop starts, App.tsx renders HelixShell as the root — the full VS-style IDE shell is visible on launch.
result: pass

## Summary

total: 20
passed: 12
issues: 7
skipped: 0
blocked: 1
pending: 0

## Gaps

- truth: "DeviceManualControl panel opens in a new dock tab styled per the Phase 11 design spec"
  status: failed
  reason: "User reported: device manual control is not styled versus what we planned but looks good"
  severity: cosmetic
  test: 10
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

- truth: "Chart Panel is visible in the default docked layout and can be dragged over and dropped into"
  status: failed
  reason: "User reported: cant see chart panel"
  severity: major
  test: 13
  root_cause: "DEFAULT_LAYOUT in WorkspaceLayout.tsx has no chart tab; ChartPanel only created dynamically via openChartPanel() — no way to drop a trend without an existing target"
  artifacts:
    - path: "helix-desktop/src/shell/WorkspaceLayout.tsx"
      issue: "DEFAULT_LAYOUT missing chart-0 tab entry"
  missing:
    - "Add chart-0 tab to DEFAULT_LAYOUT right sidebar or bottom panel"
  debug_session: ""

- truth: "Log Output panel shows runspace events from the agent WebSocket when agent is running"
  status: failed
  reason: "User reported: Log output there, agent is running but no logs"
  severity: major
  test: 16
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

- truth: "Save Layout persists the current dock layout under a named key via useWorkspaceLayouts / LazyStore"
  status: failed
  reason: "User reported: fails. doesn't save"
  severity: major
  test: 19
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""
