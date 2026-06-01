# Phase 6.2 - Pattern Map

**Mapped:** 2026-06-01
**Files analyzed:** 4 target questions
**Analogs found:** 3 / 3 (drag/drop: no existing analog)

---

## 1. DeviceManualControl — ParameterType-to-widget pattern

**Analog:** `helix-desktop/src/components/DeviceManualControl/DeviceManualControl.tsx`

Per-parameter widget dispatch uses a `.map()` with an inline `param.type === "Numeric"` guard (lines 70-91):

```tsx
{descriptor.parameters.map((param) =>
  param.type === "Numeric" ? (
    <div key={param.name}>
      <label htmlFor={`${descriptor.verbName}.${param.name}`}>{param.displayName}</label>
      <input
        id={`${descriptor.verbName}.${param.name}`}
        type="number"
        min={param.min}
        max={param.max}
        step={param.step ?? 1}
        value={values[`${descriptor.verbName}.${param.name}`] ?? ""}
        onChange={(e) =>
          setValues((v) => ({
            ...v,
            [`${descriptor.verbName}.${param.name}`]: e.target.value,
          }))
        }
      />
      <span>{param.unit}</span>
    </div>
  ) : null
)}
```

Key observations for DeviceAction node parameter inputs:
- Value keyed as `` `${verbName}.${param.name}` `` — namespace per capability to avoid collisions
- State is `Record<string, string>` — all values as strings, cast on submit
- Only `"Numeric"` is handled; other types fall through to `null` (pattern to extend with `"Boolean"`, `"Text"`, etc.)
- Collected into `parameters: Record<string, string | number>` at execute time (lines 36-39)

---

## 2. Drag-and-drop — no existing analog

**Search result:** Zero matches for `draggable`, `onDragStart`, `onDrop`, `onDragOver` in `helix-desktop/src/**/*.{ts,tsx}`.

No drag/drop exists in the codebase. Use HTML5 native API directly:

```tsx
// Node palette item (draggable source)
<div
  draggable
  onDragStart={(e) => e.dataTransfer.setData("nodeType", "DeviceAction")}
>

// ReactFlow canvas drop target
<div
  onDragOver={(e) => e.preventDefault()}
  onDrop={(e) => {
    const nodeType = e.dataTransfer.getData("nodeType");
    // project screen coords to flow coords via reactFlowInstance.screenToFlowPosition
  }}
>
```

ReactFlow's `useReactFlow().screenToFlowPosition({ x: e.clientX, y: e.clientY })` converts drop coordinates to canvas space. This is the correct approach — no library needed.

---

## 3. useAgentWebSocket — dispatch pattern for useNodeExecutionState

**Analog:** `helix-desktop/src/hooks/useAgentWebSocket.ts`

Frame dispatch in `ws.onmessage` (lines 26-44):

```ts
ws.onmessage = (msg) => {
  try {
    const frame = JSON.parse(msg.data as string);
    if (frame.type === "trend-value") {
      setTrendValues((prev) => [...prev.slice(-200), frame as TrendValue]);
    } else if (frame.type === "safety-event") {
      setSafetyEvents((prev) => [...prev.slice(-50), frame as SafetyEvent]);
    } else if (frame.type === "device-status") {
      setDeviceStatusEvents((prev) => [...prev.slice(-50), frame as DeviceStatusEvent]);
    } else {
      setRunspaceEvents((prev) => [
        ...prev.slice(-100),
        frame as RunspaceEvent,
      ]);
    }
  } catch {
    // ignore malformed frames
  }
};
```

`runspaceEvents` is typed `RunspaceEvent[]` and bounded to 100 entries via `.slice(-100)`.

For `useNodeExecutionState`: subscribe to the same WebSocket (pass `baseUrl`), filter `runspaceEvents` by `frame.nodeId`, and map the latest matching event to `"idle" | "running" | "succeeded" | "failed"`. The hook returns the raw array; consumers derive per-node state by filtering externally.

---

## 4. ReactFlow edges — EdgeProps + data.executionState pattern

**Analog:** `helix-desktop/src/components/WorkflowBuilder/edges/AnimatedHelixEdge.tsx`

Typed edge data interface and props destructure (lines 8-23):

```tsx
export interface HelixEdgeData {
  executionState?: "idle" | "running" | "succeeded" | "failed";
}

export const AnimatedHelixEdge = memo(
  ({
    id, sourceX, sourceY, targetX, targetY,
    sourcePosition, targetPosition,
    data,          // <-- HelixEdgeData | undefined
    markerEnd,
  }: EdgeProps<HelixEdgeData>) => {
    const state = data?.executionState ?? "idle";
    // state drives strokeColour and animated dot visibility
  }
);
```

To update edge state reactively, call `setEdges` from the WorkflowBuilder parent:

```ts
setEdges((edges) =>
  edges.map((e) =>
    e.id === edgeId
      ? { ...e, data: { ...e.data, executionState: "running" } }
      : e
  )
);
```

`EDGE_TYPES` (line 82-84) must be passed to `<ReactFlow edgeTypes={EDGE_TYPES}>` for the custom renderer to activate — already wired in the existing WorkflowBuilder.

---

## Shared Patterns

| Pattern | Source | Apply to |
|---------|--------|----------|
| Namespaced value key `verbName.paramName` | `DeviceManualControl.tsx` line 38 | DeviceAction node param state |
| WebSocket frame type-switch + bounded array | `useAgentWebSocket.ts` lines 29-39 | `useNodeExecutionState` hook |
| `EdgeProps<T>` with optional `data` + null-coalesce | `AnimatedHelixEdge.tsx` lines 8-33 | Any new edge type |
| `setEdges` immutable spread for reactive data | ReactFlow convention | WorkflowBuilder execution sync |

## No Analog Found

| Pattern | Reason |
|---------|--------|
| Drag-and-drop (node palette → canvas) | No existing DnD anywhere in `helix-desktop/src/` |
