---
created: 2026-05-30T18:45:00Z
title: Multi-Mode Workflow Authoring (UI Architecture)
area: ui
files:
  - helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx
  - helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts
  - helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.ts
  - helix-desktop/src/types/agent.ts
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

### Dynamic Capability Binding
- Nodes, Kanban cards, and Start/Stop device entries are auto-generated when GET /devices returns adapters
- UI updates when devices appear or disappear (polling or WebSocket DeviceEvent)
- Capabilities become first-class building blocks — user picks a capability, parameters are auto-populated from the capability schema

### Architecture Notes
- Mode selector in WorkflowBuilder toolbar (tab or toggle)
- Shared state: `currentMode: "nodeflow" | "kanban" | "startstop"` + `wslDocument: WslDocument`
- Each mode is a separate React component that reads/writes `wslDocument` via callbacks
- Mode switch triggers re-parse: `wslDocument → mode-specific representation`

## Implementation hint

Phase 7 candidate (or Phase 6.1 polish). Depends on:
- Phase 6 stable WorkflowBuilder foundation (Rete.js node editor + WSL export)
- GET /devices endpoint returning capability schemas with parameter types (extends Phase 6 06-01)
