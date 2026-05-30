---
id: SEED-001
status: dormant
planted: 2026-05-30
planted_during: Phase 6.1 — Desktop App Polish (milestone v1.0)
trigger_when: when relevant
scope: unknown
---

# SEED-001: Ribbon + Docking Workspace UI for Helix Desktop Tauri app

## Why This Matters

_To be filled in. Run `/gsd:capture --seed --enrich SEED-001` to add context._

## When to Surface

**Trigger:** when relevant

This seed will surface during `/gsd:new-milestone` when the milestone scope matches.

## Scope Estimate

**Unknown** — run `/gsd:capture --seed --enrich SEED-001` to estimate effort.

## Design Summary

Visual Studio–style IDE layout for the Helix Desktop Tauri application:

**Ribbon** (top chrome):
- Tabs: Home, Devices, Trends, Workflows, Charts, Optimisation, Settings
- Each tab exposes context-specific controls and ribbon buttons
- Ribbon buttons open dockable panels — no page navigation

**Docking Workspace** (main area):
- Device Explorer — device list grouped by type/capability; selecting opens auto-generated Manual Control panel
- Trend Explorer — raw trends, derived trends, custom trends; drag into chart panels
- Chart Panels — dockable, real-time or replayed; custom charts via trend drag-and-drop
- Workflow Editor — Rete.js node-flow canvas in a dockable panel
- Experiment Timeline — event protocol, manual actions, trend overlays
- Manual Control Panels — auto-generated from CapabilityDescriptor schema
- Python Console — optional dock for agent/trend engine snippets
- Log/Output Panels — system logs, device logs, execution logs

**Key behaviour:**
- Manual control actions execute through CapabilityRegistry (same as automated workflows) and are recorded as structured events
- Workspace layouts saveable and restorable
- Panels are dockable, floatable, pinnable, closeable

## Breadcrumbs

- `.planning/sketches/001-ribbon-workspace-shell/` — Sketch 001 started (3 variants). index.html written with Variant A (Dense VS-Ribbon), B (Figma-style), C (Compact menu+toolbar). Interrupted — sketches 002 and 003 not yet built.
- `helix-desktop/src/App.tsx` — current flat layout; gap-closure plan 06-11-PLAN.md adds simple tabs as a stepping stone
- `helix-desktop/src/components/DeviceManualControl/DeviceManualControl.tsx` — auto-generated manual control panel (Phase 6.1); seed of the Manual Control Panel dock
- `Helix.Orchestration.Core/Capabilities/CapabilityDescriptor.cs` — schema that drives auto-generated UI
- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx` — Rete.js canvas; becomes the Workflow Editor dock
- `helix-desktop/src/components/TrendCharts/` — recharts components; become Chart Panel docks

## Notes

Captured after interrupting `/gsd:sketch` session. Resume with `/gsd:sketch` to complete sketches 002 (docking panel model) and 003 (manual control dialog). A docking library spike (react-mosaic, golden-layout, rc-dock, or custom CSS grid) is recommended before planning this phase.
