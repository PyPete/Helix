---
created: 2026-06-01T00:00:00Z
title: Agent WebSocket — Add nodeId, startedAt, estimatedCompletionAt to RunspaceEvent
area: backend
priority: high
seed: SEED-005
files:
  - Helix.Orchestration.Agent/Streaming/RunspaceEvent.cs (or equivalent)
  - Helix.Orchestration.Core/Capabilities/CapabilityDescriptor.cs
  - helix-desktop/src/types/agent.ts
  - helix-desktop/src/hooks/useAgentWebSocket.ts
---

## Goal

Extend the Agent WebSocket execution events to carry timing metadata so the
ReactFlow node canvas (SEED-005) can display per-node execution state with
live start time and ETA.

## Problem

The current `RunspaceEvent` (dispatched by `IRunspaceEventEmitter`) carries
execution state transitions but no timing information. The frontend canvas
cannot show "started at 14:32:07 — ETA 14:34:30" without this data from the
Agent.

## Changes Required

### C# — RunspaceEvent record

Add two optional fields to the `RunspaceEvent` record (location:
`Helix.Orchestration.Agent/Events/` or `Streaming/` — verify path):

```csharp
public record RunspaceEvent(
    string RunspaceId,
    string NodeId,
    NodeExecutionState State,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? EstimatedCompletionAt = null
);
```

- `StartedAt`: set when `State` transitions to `Running`. UTC timestamp.
- `EstimatedCompletionAt`: set when `State` transitions to `Running`, derived
  from `CapabilityDescriptor.EstimatedDurationMs` if available. Null if unknown.

### C# — CapabilityDescriptor

Add optional `EstimatedDurationMs` field:

```csharp
public record CapabilityDescriptor(
    string Name,
    string Description,
    string? ParameterSchema = null,
    string? ResultSchema = null,
    int? EstimatedDurationMs = null   // NEW — optional, set by driver authors
);
```

Driver authors who know typical response times (e.g. `VapourtecR4.SetTemperature`
takes ~2s) can populate this. Leave null for unknown.

### TypeScript — agent.ts

Mirror the new fields in the `RunspaceEvent` TypeScript interface:

```typescript
export interface RunspaceEvent {
  runspaceId: string;
  nodeId: string;
  state: NodeExecutionState;
  startedAt?: string;               // ISO 8601
  estimatedCompletionAt?: string;   // ISO 8601
}
```

### TypeScript — useAgentWebSocket.ts

Parse the new fields when dispatching `RunspaceEvent` frames from the
WebSocket. No structural changes needed — the fields are additive.

## Success Criteria

- [ ] `RunspaceEvent` JSON serialised to WebSocket includes `startedAt` and
  `estimatedCompletionAt` when `state = running`
- [ ] Fields are `null`/absent when ETA is unknown — no fake precision
- [ ] `CapabilityDescriptor` record compiles with optional `EstimatedDurationMs`
- [ ] TypeScript types updated; `useAgentWebSocket` test updated if assertions
  check the event shape
- [ ] Existing tests pass — fields are additive, no breaking changes

## Notes

- This is a backend prerequisite for SEED-005 sub-phase C (live execution state).
  Sub-phases A and B (ReactFlow migration + device palette) can proceed without it.
- `EstimatedDurationMs` on `CapabilityDescriptor` does not need to be populated
  for every driver immediately — null ETA shows elapsed time only (graceful).
- See SEED-005 for the full frontend design spec.
