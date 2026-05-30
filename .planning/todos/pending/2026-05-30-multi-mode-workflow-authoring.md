---
created: 2026-05-30T18:45:00Z
title: Multi-Mode Workflow Authoring (UI Architecture)
area: ui
files:
  - helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx
  - helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts
  - helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.ts
  - helix-desktop/src/types/agent.ts
  - Helix.Orchestration.Core/Devices/CapabilityRegistry.cs
  - Helix.Orchestration.Agent/Api/DeviceEndpoints.cs
---

## Problem

The current WorkflowBuilder (Phase 6) exposes only the Rete.js node-flow editor. Scientists may prefer different interaction models depending on their workflow complexity and familiarity with node-based UIs. A single rigid editor mode is a barrier to adoption.

## Solution

Design a **Multi-Mode Workflow Authoring** system where all modes share the same underlying WSL workflow model:

### Three Authoring Modes

1. **Node-flow editor** (current — Rete.js canvas)
   - Full graph editing for complex branching, loops, and parallel execution
   - Right-click context menu adds nodes; drag to connect

2. **Kanban blocks**
   - Drag-and-drop cards in a vertical sequence (linear workflows)
   - Each card = one WSL step node; cards are auto-generated from device capabilities
   - Simpler mental model for scientists who think procedurally

3. **Start/Stop panel**
   - Minimal mode: select devices, set parameters, run
   - No explicit workflow graph — generates a simple linear WSL document automatically
   - Best for one-shot or well-understood protocols

### Shared WSL Model
- All three modes read/write the same `WslDocument` type
- Switching modes re-renders the same document in the target UI
- Export always produces valid WSL JSON (round-trippable through POST /workflows/compile)

### CapabilityRegistry as the Single Source of Truth

Devices expose capabilities → stored server-side in `CapabilityRegistry` (C#).
The UI queries the registry (via `GET /devices` + `GET /devices/{id}/capabilities`) to **generate all authoring surfaces dynamically**:

| Registry entry | Node-flow | Kanban | Start/Stop |
|---|---|---|---|
| Device capability | Rete.js node template | Kanban card template | Device row + parameter inputs |
| Capability parameters | Node input sockets | Card field list | Parameter form |
| Capability verb name | Node title | Card header | Action label |

WSL step templates are also generated from registry entries — the capability verb name becomes the `capability` field in the WSL step node.

### Device Lifecycle — Graceful Degradation

When a device is unplugged (or its adapter goes offline):
- **Workflows already authored:** steps referencing the missing device are marked invalid (`{ invalid: true, reason: "device offline" }`) but the WslDocument is preserved — not discarded
- **Nodes/cards:** rendered with a visual "device unavailable" state (greyed out, warning icon) rather than being silently removed
- **Trends:** trend series that sourced from the device stop updating; the chart shows last-known value + a "device offline" annotation
- **Compilation:** `POST /workflows/compile` returns a 422 diagnostic for each step referencing an unavailable device — not a silent 200
- **Re-connection:** when the device re-appears in the registry, invalid nodes/cards automatically become valid again without requiring a page reload

This degradation contract must be defined at the `GET /devices` + WebSocket `DeviceStatusEvent` layer, not in each UI component individually.

### Architecture Notes
- Mode selector in WorkflowBuilder toolbar (tab or toggle)
- Shared state: `currentMode: "nodeflow" | "kanban" | "startstop"` + `wslDocument: WslDocument`
- Each mode is a separate React component that reads/writes `wslDocument` via callbacks
- Mode switch triggers re-parse: `wslDocument → mode-specific representation`

## Implementation hint

Phase 7 candidate (or Phase 6.1 polish). Depends on:
- Phase 6 stable WorkflowBuilder foundation (Rete.js node editor + WSL export)
- `GET /devices` + `GET /devices/{id}/capabilities` returning capability parameter schemas with types (extends Phase 6 `06-01` — currently returns verb names only, needs parameter type metadata)
- A `DeviceStatusEvent` WebSocket frame type (currently absent from the Agent event hierarchy) to notify UI of device connect/disconnect without polling
- `CapabilityRegistry` exposing parameter type information (currently stores verb names + handler delegates — no schema introspection)
