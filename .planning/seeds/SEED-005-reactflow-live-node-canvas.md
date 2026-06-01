---
id: SEED-005
status: dormant
planted: 2026-06-01
planted_during: Phase driver-port (milestone v1.0)
trigger_when: Phase 7 complete or when Node-Flow authoring becomes a milestone focus
scope: large
---

# SEED-005: ReactFlow Node Canvas — Live Execution, Blender/UE Theme, Device Action Palette

## Why This Matters

The current Rete.js WorkflowBuilder is a thin scaffold — 7 node types, a
capability dropdown that sets text on a single StepNode, and a right-click
context menu. It proves the concept but does not deliver the authoring
experience scientists and power users need. This seed captures a complete
redesign of the Node-Flow authoring surface.

The core upgrade: replace Rete.js with ReactFlow, apply a dark IDE theme
matching the app's VS Code/JetBrains aesthetic, introduce a device action
palette (Blender/UE drag-and-drop library), make nodes live-reactive during
execution (traffic-light state + animated edges), and support a hybrid
static-value/data-flow port model.

Migration research (2026-06-01) confirmed: WslDocument schema needs zero
changes, migration cost is 2–4 days, all capabilities are confirmed in
ReactFlow's API.

## When to Surface

**Trigger:** When Phase 7 (Site-Level React Dashboard) is complete, or when
Node-Flow authoring is selected as a milestone focus.

This seed surfaces naturally alongside SEED-004 (Three Workflow Modes) — the
Node-Flow mode described there is exactly this canvas.

## Scope Estimate

**Large** — recommended breakdown into 3 sub-phases:

| Sub-phase | Scope | Est. |
|---|---|---|
| 7.N.A: ReactFlow migration + theme | Replace Rete.js, custom node components, Blender-style dark theme, minimap, pan/scroll | M |
| 7.N.B: Device action palette + hybrid ports | Left panel palette by device instance, drag-to-canvas, static+dataflow hybrid inputs | M |
| 7.N.C: Live execution state + Agent ETA | Traffic-light nodes, animated edges, Agent WS ETA events, time-started/ETA display | M |

## Design Summary

### The Canvas

ReactFlow replaces Rete.js. The canvas is the same `WslDocument` graph —
`WslNode` and `WslEdge` JSON — with a ReactFlow renderer on top.
`exportToWsl()` reads from `useReactFlow().getNodes()/getEdges()` instead
of `editor.getNodes()/getConnections()`. The schema is unchanged.

**Theme:** Dark IDE palette matching the app — near-black background
(`#141414`), node bodies in `#1e1e1e`, port handles in the app's accent
colour, connections in `#4a4a4a` at rest. Inspired by Blender's compositor
and Unreal Engine Blueprint — dense, readable, professional. Not consumer SaaS.

**Navigation:** Free pan (drag) and zoom (scroll wheel). Minimap in bottom-right
corner with per-node colour keyed on execution state. Horizontal scroll
supported natively — no configuration required.

---

### Device Action Palette

A left-side panel lists every connected device adapter, grouped by `adapterId`.
Each device expands to show its capabilities as draggable cards.

Multiple instances of the same device type appear as separate entries:
- Alicat [COM3] → SetFlow, GetFlow, SetGas, ...
- Alicat [COM7] → SetFlow, GetFlow, SetGas, ...

Drag a capability card onto the canvas → a pre-populated Device Action node
lands at the drop point. The node's header shows the device name and
capability. Its input handles are pre-generated from the
`CapabilityDescriptor.ParameterSchema` JSON Schema for that capability.

The palette is populated from the Agent `/devices` endpoint (same source as
the current capability dropdown). Hot-reloads when devices connect/disconnect.

---

### Hybrid Static + Data-Flow Ports

Every input port on a Device Action node supports two modes:

**Static mode (default):** An inline form field rendered beside the port
handle — `<input type="number">` for numeric parameters, `<select>` for
enums, `<input type="text">` for strings. The field shows the parameter name,
unit (from CapabilityDescriptor), and min/max if present. This is the
Blender model — every input has a usable default value without wiring.

**Data-flow mode:** When another node's output handle is wired to this input,
the inline field is hidden and replaced by the connection label. The upstream
node's output value is used at execution time. Wiring is type-gated — a
`number` output cannot connect to a `string` input.

Logic: `if (edges.some(e => e.targetHandle === portId)) { use wire } else { use field.value }`

Port colour convention (Blender-inspired):
- `number` → grey-blue
- `string` → pink
- `boolean` → green
- `enum` → orange
- `exec` (control flow) → white/bright

---

### Execution State — Traffic Light

During workflow execution, the Agent WebSocket streams `RunspaceEvent` records
with `nodeId` and `state` fields. The canvas subscribes to these and updates
each node's visual state:

| State | Node appearance | Edge appearance |
|---|---|---|
| `pending` | Default dark style | Static grey path |
| `running` | **Amber** border + subtle amber glow | Travelling dot/dash animation along path |
| `succeeded` | **Green** border; node body dims to 60% opacity | Static green path |
| `failed` | **Red** border + red glow; node body stays full opacity | Static red path |

**Animated edges (running):** SVG `<animateMotion>` with a bright circle
travelling the path at ~1s/loop. The path itself becomes amber. Multiple
parallel edges animate independently.

**Minimap:** Node colour in the minimap uses the same traffic-light colours
via `<MiniMap nodeColor={node => executionStateColour(node.data.state)} />`.
Makes global execution progress visible without scrolling the main canvas.

---

### Node Timing Display

Each node has a footer section showing timing information during and after
execution:

**While running:**
```
▶ Started 14:32:07   ETA ~14:34:30
```

**After succeeded:**
```
✓ Finished in 2m 23s
```

**After failed:**
```
✗ Failed at 14:33:41
```

`startedAt` and `estimatedCompletionAt` are delivered by the Agent in the
`RunspaceEvent` WebSocket message. The Agent derives ETA from the
CapabilityDescriptor's `estimatedDurationMs` metadata field (to be added to
the C# `CapabilityDescriptor` record). If no ETA is available, only elapsed
time is shown.

---

### Agent WebSocket Changes Required (backend prerequisite)

The `RunspaceEvent` record in `Helix.Orchestration.Agent` needs two new
optional fields:

```csharp
// Helix.Orchestration.Agent/Streaming/RunspaceEvent.cs (approx.)
public record RunspaceEvent(
    string RunspaceId,
    string NodeId,               // existing
    NodeExecutionState State,    // existing
    DateTimeOffset? StartedAt,   // NEW
    DateTimeOffset? EstimatedCompletionAt  // NEW
);
```

`CapabilityDescriptor` gains an optional `EstimatedDurationMs` field (set by
driver authors who know typical device response times).

The TypeScript `RunspaceEvent` type in `agent.ts` mirrors these fields.

---

### Node Types (unchanged from current)

The 7 existing node types (Step, Condition, Branch, Loop, Parallel, Delay,
WaitFor) remain. Device Action nodes are a new 8th type — they are Step nodes
with a pre-populated `capability` and auto-generated parameter handles.

---

### WslDocument Compatibility

Zero schema changes. ReactFlow stores `nodeType`, `parameters`, and `position`
in `node.data`. `exportToWsl()` maps these to `WslNode` and `WslEdge`
identically to the current implementation. Existing saved workflows load
without migration.

---

## Relationship to Other Seeds

- **SEED-004** — Three Workflow Modes: Node-Flow mode is this canvas. The
  Kanban and Start/Stop modes are siblings.
- **SEED-002** — Strict authoring: CapabilityDescriptor-validated inputs on
  Device Action nodes align with the controlled parameter entry model.
- **SEED-003** — Experiment as top-level scope: execution state display on
  nodes is part of the experiment scope model.

## Breadcrumbs

- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx` — replace
- `helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts` — replace with `useHelixFlow.ts`
- `helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.ts` — replace with ReactFlow node components
- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.test.tsx` — rewrite (simpler)
- `helix-desktop/src/types/agent.ts` — add `startedAt`, `estimatedCompletionAt` to `RunspaceEvent`
- `helix-desktop/package.json` — add `reactflow`, remove `rete` + plugins
- `Helix.Orchestration.Agent/Streaming/` — extend `RunspaceEvent` with timing fields
- `Helix.Orchestration.Core/Capabilities/CapabilityDescriptor.cs` — add `EstimatedDurationMs`

## Reference Implementations

- [chaiNNer](https://github.com/chaiNNer-org/chaiNNer) — hybrid static+dataflow ports, per-node execution state, custom renderers
- [synergycodes/workflowbuilder](https://github.com/synergycodes/workflowbuilder) — schema-driven node config pattern, plugin architecture
- Blender Compositor / Shader Editor — Blender-style dark theme, port colour conventions
- Unreal Engine Blueprint — traffic-light execution state, animated pulse edges
