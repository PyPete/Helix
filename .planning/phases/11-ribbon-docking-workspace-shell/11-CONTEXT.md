# Phase 11: Ribbon + Docking Workspace Shell - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Replace helix-desktop current flat App.tsx layout with a professional VS-style IDE shell: VS-Ribbon (68px), activity bar (40px), dockable/split panel workspace, document tabs, Device Explorer, Trend Explorer, Chart Panel docks, Log/Output dock, status bar, and workspace layout save/restore. No backend changes required.

</domain>

<decisions>
## Implementation Decisions

### Docking Library
- **D-01:** Use `rc-dock` as the docking and split-panel library. Feature-complete: float, dock, split, tab groups, drag-to-dock. TypeScript types included.
- **D-02:** Ship a hardcoded `DEFAULT_LAYOUT` constant (left 220px, right 200px, bottom 140px, center flex-1). User rearranges and saves named layouts on top.
- **D-03:** CSS variable override layer (`helix-dock.css`) maps rc-dock class names to Helix CSS variables. Keeps library upgradeable.

### App.tsx Migration
- **D-04:** Big-bang replacement â€” App.tsx rewritten wholesale. Old header + custom tab system removed. All components re-hosted inside rc-dock panels.
- **D-05:** Home and Devices ribbon tabs wired with real content in Phase 11. Remaining tabs (Trends, Workflows, Charts, Developer, Settings) visible with empty ribbon bands.
- **D-06:** Component-to-panel mapping delegated to planner. Guidelines: WorkflowBuilder and ExecutionPanel to document tabs; ScalarTrendChart + SpectralChart to dockable Chart Panel docks (H8); SafetyPanel to bottom strip or dock; DeviceManualControl to document tab from Device Explorer (H6).

### Trend Explorer Wiring
- **D-07:** `useTrendRegistry` hook accumulates seen `trendName` strings from live WebSocket `trendValues`. No new Agent endpoint needed in Phase 11.
- **D-08:** HTML5 drag-and-drop for Trend Explorer items (`draggable` + `onDragStart` with trendName in `dataTransfer`). Chart Panel docks have `onDrop` handlers. No extra DnD library.
- **D-09:** Unlimited Chart Panel docks, each independently tracking its own `trendName` list. Opened via Ribbon or Trend Explorer drag.

### Workspace Layout Persistence
- **D-10:** `tauri-plugin-store` via `LazyStore` (same pattern as `ConnectionConfig.tsx`). Store key: `workspace-layouts`.
- **D-11:** Serialize rc-dock `LayoutBase` JSON tree only via `DockLayout` ref `getLayout()`/`loadLayout()`. No document tab state in Phase 11.
- **D-12:** Save Layout button in Ribbon Home (inline name input). Load Layout button opens dropdown of saved names.

### Claude Discretion
- Panel IDs, `LayoutBase` structure, CSS class naming in `helix-dock.css`, Ribbon button group details.
- Status bar (22px, connection status, version) included in shell skeleton per sketch findings.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Design (settled â€” do not re-question these decisions)
- `.claude/skills/sketch-findings-Helix/SKILL.md` â€” Design direction summary; CSS variables; source file index
- `.claude/skills/sketch-findings-Helix/references/layout-and-chrome.md` â€” Ribbon CSS patterns, activity bar active indicator, document tab structure, splitter, status bar, HTML skeleton. **Primary implementation reference.**
- `.claude/skills/sketch-findings-Helix/references/manual-control-ui.md` â€” Card grid pattern for DeviceManualControl
- `.claude/skills/sketch-findings-Helix/sources/themes/default.css` â€” All CSS custom properties

### Requirements
- `.planning/REQUIREMENTS.md` Phase H section â€” H1â€“H11 requirements
- `.planning/ROADMAP.md` Phase 11 entry â€” Goal, success criteria, dependencies

### Existing Code (integration points)
- `helix-desktop/src/App.tsx` â€” Current flat layout being replaced
- `helix-desktop/src/hooks/useAgentWebSocket.ts` â€” Provides `trendValues` stream for `useTrendRegistry`
- `helix-desktop/src/components/ConnectionConfig.tsx` â€” `LazyStore` pattern reference (D-10)
- `helix-desktop/src/lib/workflowLibrary.ts` â€” `tauri-plugin-fs` pattern reference
- `helix-desktop/src/components/charts/index.ts` â€” SciChart wrapper re-export
- `helix-desktop/src/components/DeviceManualControl/DeviceManualControl.tsx` â€” Opened as document tab from Device Explorer (H6)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `useAgentWebSocket` â€” already provides `trendValues: TrendValue[]`; `useTrendRegistry` wraps it
- `LazyStore` from `@tauri-apps/plugin-store` â€” already wired, tested pattern for persistence
- `DeviceManualControl` â€” complete component, just needs hosting in Device Explorer dock
- `DeviceRegistry` â€” provides device list for Device Explorer
- All 5 content components (WorkflowBuilder, ExecutionPanel, ScalarTrendChart, SpectralChart, SafetyPanel) â€” unchanged; only hosting changes

### Established Patterns
- TypeScript strict mode; all new hooks/components must type-check
- Vitest tests for all new hooks (`useTrendRegistry`, layout persistence); 76 existing tests must continue to pass
- `vi.hoisted()` + explicit mock patterns required (see STATE.md decisions)
- CSS custom properties via `var(--color-*)` â€” no hardcoded hex values
- `LazyStore` sync constructor pattern (v2.4.3)

### Integration Points
- rc-dock `DockLayout` becomes new composition root replacing `App.tsx` div.app
- Ribbon + title bar + status bar sit outside rc-dock
- `useAgentHealth`, `useAgentWebSocket`, `connection` state lifted to shell, passed to panels

</code_context>

<specifics>
## Specific Ideas

- HTML skeleton from sketch: titlebar â†’ ribbon-tabs â†’ ribbon-band â†’ workspace(actbar + sidebar-left + splitter + main-area + splitter + sidebar-right) â†’ statusbar
- Sidebars collapsible to width=0 via panel close button
- Ribbon labels: uppercase, 0.05â€“0.08em letter-spacing, `var(--text-xs)`
- Document tab format: emoji prefix + name
- Activity bar: Settings icon bottom-anchored, explorer icons top-down

</specifics>

<deferred>
## Deferred Ideas

- **Multi-Mode Workflow Authoring** (Start/Stop, Kanban, Node-Flow) â€” Phase 13
- **SciChart full integration** â€” Phase 15; Chart Panel docks use existing chart wrappers in Phase 11
- **ReactFlow node canvas** â€” Phase 14; WorkflowBuilder stays on Rete.js in Phase 11
- **Trend Explorer replay mode** â€” Phase 17; Phase 11 shows live trends only
- **Developer Debugger panel** â€” Phase 16; Developer Ribbon tab is placeholder in Phase 11

### Reviewed Todos (not folded)
- Multi-Mode Workflow Authoring (UI Architecture) â€” Phase 13
- Real-Time Trend System â€” Phase 17
- SciChart Integration â€” Phase 15
- ReactFlow Node Canvas Spike â€” Phase 14
- Auto-Generated Manual Control Dialogs â€” already DeviceManualControl; Phase 11 wires it into Device Explorer dock as H6

</deferred>

---

*Phase: 11-ribbon-docking-workspace-shell*
*Context gathered: 2026-06-04*
