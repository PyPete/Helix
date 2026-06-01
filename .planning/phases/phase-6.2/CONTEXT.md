# Phase 6.2: ReactFlow Node Canvas — Device Palette, Hybrid Ports & Live Execution

**Phase:** 6.2
**Date:** 2026-06-01
**Status:** context captured, ready to plan
**Depends on:** Phase 6.1 (CapabilityDescriptor schema, DeviceEndpoints), Phase 6 (WorkflowBuilder foundation)
**Seed:** SEED-005

---

<domain>
Extend the ReactFlow WorkflowBuilder (spike landed in dev, 56 tests passing) with:
- A device action palette (left panel, grouped by adapterId, drag-to-canvas)
- A DeviceAction node type with CapabilityDescriptor-driven parameter inputs
- Hybrid static+dataflow port model on DeviceAction nodes
- Live execution state: traffic-light per node + animated amber edges driven by Agent WebSocket
- Step timing footer: startedAt / ETA / elapsed driven by StepStartedEvent / StepCompletedEvent
- Agent-side: add EstimatedDurationMs to CapabilityDescriptor; expose startedAt on StepStartedEvent

The ReactFlow spike (Phase 6.2 sub-phase A) is ALREADY DONE on dev:
- rete removed, reactflow installed
- 7 node components (Step, Condition, Branch, Loop, Parallel, Delay, WaitFor)
- AnimatedHelixEdge with SVG animateMotion amber dot
- useHelixFlow hook, exportWsl(), dark theme, minimap, right-click context menu
- 56 tests passing

Remaining scope is sub-phases B and C only.
</domain>

<decisions>

## What is already done (spike — DO NOT redo)
- ReactFlow installed, rete removed (package.json)
- HelixNodes.tsx: 7 node types as React components with traffic-light CSS hooks
- AnimatedHelixEdge.tsx: amber animateMotion dot + traffic-light stroke
- useHelixFlow.ts: useState nodes/edges, addNode(), exportWsl()
- WorkflowBuilder.tsx: canvas with Background, Controls, MiniMap, context menu
- WorkflowBuilder.test.tsx: 11 tests including context menu open/add/close
- 56 total tests passing

## Sub-phase B: Device Palette + DeviceAction Node + Hybrid Ports

### DeviceAction Node
- New 8th node type: `DeviceAction` in HelixNodes.tsx
- Header: `{adapterId} › {capabilityName}` (e.g. "Alicat [COM3] › SetFlow")
- Parameters driven by `CapabilityDescriptor.parameters[]` from `/devices/{id}/capabilities`
- For each `ParameterSchema`:
  - `Numeric` → `<input type="number" min max step>` + unit label
  - `Enum` → `<select>` with `options[]`
  - `Boolean` → `<input type="checkbox">`
  - `String` → `<input type="text">`
  - `Duration` → `<input type="number">` + "ms" label
- Each parameter field has a Handle on its left for data-flow wiring (target handle)
- Hybrid logic: field shows when no edge targets that handle; field hides when a wire is connected
- Output handle: single `exec-out` for control flow
- Port colours (Blender convention): number=`#5b8dd9`, enum=`#e67e22`, boolean=`#27ae60`, string=`#e91e8c`, exec=`#a6adc8`

### Device Palette
- New component: `DevicePalette.tsx` in `src/components/WorkflowBuilder/`
- Left panel, ~220px wide, scrollable
- Groups by `adapterId` with collapsible sections
- Each capability appears as a draggable card showing capability name
- HTML5 drag: `draggable`, `onDragStart` sets `dataTransfer` with `{ adapterId, capability }`
- WorkflowBuilder canvas `onDrop` handler: reads dataTransfer, fetches CapabilityDescriptor for that capability, creates DeviceAction node at drop position
- Palette populated from `/devices` endpoint (existing fetch) + `/devices/{id}/capabilities` per device
- DevicePalette shown alongside the canvas in a flex layout

### useHelixFlow changes
- Add `addDeviceActionNode(adapterId, capability, descriptor, position)` to the hook
- DeviceAction node data: `{ nodeType: "DeviceAction", adapterId, capability, parameters: descriptor.parameters, fieldValues: {} }`
- `exportWsl()` maps DeviceAction nodes: `nodeType="Step"`, `parameters={ capability: "{adapterId}.{capability}", ...fieldValues }`

## Sub-phase C: Live Execution State + Step Timing

### Agent WebSocket → canvas state
- `useAgentWebSocket` already dispatches `runspaceEvents` array
- New hook: `useNodeExecutionState(runspaceId, nodes)` that maps StepId→executionState
  - `step-started` event → node with matching `nodeId` (= StepId) set to `executionState: "running"`, record `startedAt`
  - `step-completed` → set to `executionState: "succeeded"`, record `finishedAt`
  - `step-faulted` → set to `executionState: "failed"`, record `faultMessage`
  - `runspace-started` → reset all nodes to `"idle"`
  - `runspace-completed` / `runspace-aborted` → do nothing (nodes keep their final state)

### StepId → nodeId mapping
- The WslDocument `nodeId` is the ReactFlow node `id` (set by `useHelixFlow.addNode`)
- The Agent emits `StepStartedEvent.StepId` which equals the WslNode.nodeId
- WorkflowBuilder passes the current workflow's nodeId→node map to `useNodeExecutionState`

### Node timing footer
- Each node component receives `executionState`, `startedAt?: Date`, `estimatedCompletionAt?: Date`, `finishedAt?: Date`
- Footer row below body:
  - running: `▶ Started HH:MM:SS   ETA ~HH:MM:SS` (or just `▶ Started HH:MM:SS` if no ETA)
  - succeeded: `✓ Finished in Xm Ys`
  - failed: `✗ Failed at HH:MM:SS`
  - idle/pending: nothing shown

### Agent-side additions (C# — minimal)
- `CapabilityDescriptor` gains optional `EstimatedDurationMs?: int` parameter
- `StepStartedEvent` gains optional `EstimatedDurationMs?: int?` (nullable)
- `RunspaceEndpoints` or wherever StepStartedEvent is emitted: populate EstimatedDurationMs from CapabilityDescriptor if available
- TypeScript `RunspaceEvent` in agent.ts: add optional `estimatedDurationMs?: number` to step-started events

### Edge execution state
- Edges inherit execution state from their source node: when source node is `running`, edge becomes `running`
- `useNodeExecutionState` also returns `edgeExecutionStates: Record<string, ExecutionState>`
- WorkflowBuilder passes these into the edges array via `setEdges` on state change

## Capability naming convention (existing pattern)
- DeviceAction node capability field = `"{adapterId}.{capabilityName}"` e.g. `"alicat.SetFlow"`
- Matches the existing capability registry key format

## WslDocument schema (NO CHANGES)
- DeviceAction nodes export as `nodeType: "Step"` with `parameters.capability = "{adapterId}.{capability}"`
- All parameter field values export as `parameters.{paramName} = fieldValue`
- Schema version remains "1.0"

## Files to touch

### New files
- `helix-desktop/src/components/WorkflowBuilder/DevicePalette.tsx`
- `helix-desktop/src/components/WorkflowBuilder/DevicePalette.test.tsx`
- `helix-desktop/src/hooks/useNodeExecutionState.ts`
- `helix-desktop/src/hooks/useNodeExecutionState.test.ts`

### Modified files
- `helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.tsx` — add DeviceAction node, add timing footer to all nodes, add port colour convention
- `helix-desktop/src/components/WorkflowBuilder/useHelixFlow.ts` — add addDeviceActionNode(), update exportWsl() for DeviceAction
- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx` — add onDrop handler, add DevicePalette panel, wire useNodeExecutionState, update nodes/edges with execution state
- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.test.tsx` — add tests for DeviceAction drag-drop, execution state rendering
- `helix-desktop/src/types/agent.ts` — add estimatedDurationMs to RunspaceEvent
- `Helix.Orchestration.Core/Capabilities/CapabilityDescriptor.cs` — add optional EstimatedDurationMs
- `Helix.Orchestration.Agent/Events/RunspaceEvent.cs` — add EstimatedDurationMs? to StepStartedEvent

## Out of scope for this phase
- Saving/loading DeviceAction node CapabilityDescriptor metadata (descriptors are fetched fresh on load)
- Actual dataflow execution (the WslCompiler receives static field values only for now)
- The full SEED-004 three-mode switching (Node-Flow / Kanban / Start-Stop)
- SciChart integration (separate backlog item)

</decisions>

<canonical_refs>
## Files to read before implementing

### Already-implemented spike files (read to understand current state)
- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx`
- `helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.tsx`
- `helix-desktop/src/components/WorkflowBuilder/edges/AnimatedHelixEdge.tsx`
- `helix-desktop/src/components/WorkflowBuilder/useHelixFlow.ts`

### Agent source (for C# changes)
- `Helix.Orchestration.Agent/Events/RunspaceEvent.cs` — step events (StepStartedEvent has StepId, StepLabel)
- `Helix.Orchestration.Core/Capabilities/CapabilityDescriptor.cs` — ParameterSchema with Type/Min/Max/Options/Unit
- `Helix.Orchestration.Agent/Api/DeviceEndpoints.cs` — /devices/{id}/capabilities endpoint shape
- `Helix.Orchestration.Agent/Events/IRunspaceEventEmitter.cs`

### TypeScript types
- `helix-desktop/src/types/agent.ts` — CapabilityDescriptor, ParameterSchema, RunspaceEvent
- `helix-desktop/src/hooks/useAgentWebSocket.ts` — existing WebSocket dispatch

### Test patterns
- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.test.tsx` — current test patterns
- `helix-desktop/src/components/DeviceManualControl/DeviceManualControl.test.tsx` — CapabilityDescriptor-driven form test patterns
</canonical_refs>
