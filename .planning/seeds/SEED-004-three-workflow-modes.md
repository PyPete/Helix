---
id: SEED-004
status: dormant
planted: 2026-05-30
planted_during: Phase 6 / 6.1 — Tauri Desktop App (milestone v1.0)
trigger_when: when relevant
scope: unknown
---

# SEED-004: Three Workflow Modes — Node-Flow, Kanban, Start/Stop

## Why This Matters

_To be filled in. Run `/gsd:capture --seed --enrich SEED-004` to add context._

## When to Surface

**Trigger:** when relevant

This seed will surface during `/gsd:new-milestone` when the milestone scope matches.

## Scope Estimate

**Unknown** — run `/gsd:capture --seed --enrich SEED-004` to estimate effort.

## Design Summary

Helix exposes the same underlying WSL workflow through three different UI shells,
targeting different levels of user expertise.

---

### Mode Overview

| Mode | Target user | Editing capability | Complexity |
|------|-------------|-------------------|-----------|
| **Start/Stop** | Operator (run-only) | None — executes a saved workflow | Minimal |
| **Kanban** | Scientist (SOP-writer) | Block-based drag-and-drop into swimlanes | Intermediate |
| **Node-Flow** | Power user / developer | Full directed graph, nested loops, advanced logic | Full |

Mode switching changes **only the UI shell** — not the `WslDocument`, not the
compiler, not the validation engine, not the execution engine.

---

### Start/Stop Mode

A minimalist execution shell with no editing surface:
- Shows the current workflow name and phase (pre/main/post)
- Large **Start** / **Stop** / **Pause** controls
- Live step progress and event log
- No canvas, no node editing, no capability picker

Intended for operators who run known SOPs on the floor without modifying them.

---

### Kanban Mode

A block-based workflow builder where the workflow appears as cards in swimlanes:

**Structure:**
- Swimlanes represent experiment phases: Pre-Experiment, Main, Post-Experiment
- Blocks (cards) represent WSL nodes — dragged from a Task Library dock panel
- Block order within a lane defines execution order (linear sequence per lane)

**Authoring constraints (same as SEED-002 strict authoring):**
- Blocks only added by drag from Task Library — no arbitrary node creation
- Each block's inputs are validated through `CapabilityDescriptor.ParameterSchema`
- Controlled condition builder for conditional blocks (no free-form expressions)
- Units enforced on numeric inputs

**Compilation:**
- Kanban layout (lane + order) is stored as metadata in `WslNode.Parameters`:
  `{ "lane": "main", "laneOrder": 3 }`
- `WslDocument` serialises the same graph structure regardless of mode
- `WslCompiler` is unaware of Kanban — it sees standard `WslNode` + `WslEdge` records
- Edges between Kanban blocks are inferred from lane order (sequential within lane)
  and explicit cross-lane dependencies (drag-to-connect between lanes)

---

### Node-Flow Mode

The full Rete.js directed graph editor (current Phase 6 implementation, extended
by SEED-002):
- Nodes created by dragging from Task Library dock or context menu
- Full graph editing: arbitrary connections, nested loops, branch paths, parallel forks
- `CapabilityDescriptor`-validated controls on Step nodes
- Controlled condition builder on Condition/Branch nodes
- Scope inspector panel (SEED-002/003) visible during execution

---

### Mode Switching

A `viewMode` field in `WslDocument` metadata records the last-used mode:
```json
{ "viewMode": "kanban" }
```

When the user switches mode in the Ribbon's View group:
1. The current `WslDocument` is preserved unchanged
2. The UI shell re-renders — Kanban view or Node-Flow canvas mounts
3. If switching Kanban → Node-Flow: lane/order metadata preserved in parameters,
   edges inferred from lane order are made explicit in the graph
4. If switching Node-Flow → Kanban: nodes without a `lane` parameter are placed
   in a staging area; complex graph structures (nested loops, non-linear branches)
   are flagged as "not representable in Kanban view" with an affordance to convert

---

### Shared Infrastructure (all modes)

All three modes use the same:
- `WslDocument` data model
- `WslCompiler` (validation + graph transforms)
- `ISafetyEngine` (safety envelope injection)
- Experiment execution engine (SEED-003 scope model)
- Event log and replayability

---

## Relationship to Other Seeds

- **SEED-001** — Ribbon + Docking workspace; mode buttons live in the Ribbon Home tab
- **SEED-002** — Loop scopes + strict authoring; strict authoring rules apply to both Kanban and Node-Flow modes
- **SEED-003** — Experiment as top-level scope; all three modes share this execution model
- These four seeds together describe the complete Helix authoring and execution architecture

## Breadcrumbs

- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx` — current Node-Flow UI; mode switching would conditionally render Kanban or Start/Stop shell instead
- `Helix.Orchestration.Core/WSL/WslDocument.cs` — add optional `viewMode` to Metadata
- `Helix.Orchestration.Core/WSL/WslNode.cs` — add `lane` / `laneOrder` as convention in Parameters (no schema change needed)
- `helix-desktop/src/types/agent.ts` — `WslDocument` TypeScript interface
- `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx` — basis for Start/Stop mode shell

## Notes

The key architectural insight: **WSL is the canonical form; the mode is a view**.
This means:
1. No workflow lock-in — a workflow authored in Kanban can be opened in Node-Flow
2. No duplicate validation — one compiler validates all modes
3. No duplicate execution — one engine runs all modes
4. Progressive disclosure — operators get simplicity, power users get power