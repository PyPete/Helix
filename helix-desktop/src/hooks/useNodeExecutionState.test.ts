import { describe, it, expect } from "vitest";
import { renderHook } from "@testing-library/react";
import { useNodeExecutionState } from "./useNodeExecutionState";
import type { RunspaceEvent } from "../types/agent";
import type { Edge } from "reactflow";

// Helper to build a RunspaceEvent
function makeEvent(overrides: Partial<RunspaceEvent> & { type: string }): RunspaceEvent {
  return {
    runspaceId: "r1",
    timestamp: "2026-01-01T00:00:00.000Z",
    ...overrides,
  };
}

describe("useNodeExecutionState", () => {
  it("step-started sets node to running with startedAt", () => {
    const events: RunspaceEvent[] = [
      makeEvent({
        type: "step-started",
        stepId: "node_1",
        stepLabel: "A",
      }),
    ];
    const { result } = renderHook(() => useNodeExecutionState(events));
    expect(result.current.nodeStates["node_1"].executionState).toBe("running");
    expect(result.current.nodeStates["node_1"].startedAt).toBeInstanceOf(Date);
  });

  it("step-started with estimatedDurationMs sets estimatedCompletionAt", () => {
    const events: RunspaceEvent[] = [
      makeEvent({
        type: "step-started",
        stepId: "node_1",
        stepLabel: "A",
        estimatedDurationMs: 5000,
        timestamp: "2026-01-01T00:00:00.000Z",
      }),
    ];
    const { result } = renderHook(() => useNodeExecutionState(events));
    const expectedCompletion = new Date("2026-01-01T00:00:05.000Z");
    expect(result.current.nodeStates["node_1"].estimatedCompletionAt).toEqual(expectedCompletion);
  });

  it("step-started without estimatedDurationMs leaves estimatedCompletionAt undefined", () => {
    const events: RunspaceEvent[] = [
      makeEvent({
        type: "step-started",
        stepId: "node_1",
        stepLabel: "A",
        // no estimatedDurationMs
      }),
    ];
    const { result } = renderHook(() => useNodeExecutionState(events));
    expect(result.current.nodeStates["node_1"].estimatedCompletionAt).toBeUndefined();
  });

  it("step-completed sets node to succeeded with finishedAt", () => {
    const events: RunspaceEvent[] = [
      makeEvent({ type: "step-started", stepId: "node_1", stepLabel: "A" }),
      makeEvent({ type: "step-completed", stepId: "node_1" }),
    ];
    const { result } = renderHook(() => useNodeExecutionState(events));
    expect(result.current.nodeStates["node_1"].executionState).toBe("succeeded");
    expect(result.current.nodeStates["node_1"].finishedAt).toBeInstanceOf(Date);
  });

  it("step-faulted sets node to failed with faultMessage", () => {
    const events: RunspaceEvent[] = [
      makeEvent({ type: "step-started", stepId: "node_1", stepLabel: "A" }),
      makeEvent({ type: "step-faulted", stepId: "node_1", faultMessage: "timeout" }),
    ];
    const { result } = renderHook(() => useNodeExecutionState(events));
    expect(result.current.nodeStates["node_1"].executionState).toBe("failed");
    expect(result.current.nodeStates["node_1"].faultMessage).toBe("timeout");
  });

  it("runspace-started clears all prior node states", () => {
    const events: RunspaceEvent[] = [
      makeEvent({ type: "step-started", stepId: "node_1", stepLabel: "A" }),
      makeEvent({ type: "runspace-started" }),
      makeEvent({ type: "step-started", stepId: "node_2", stepLabel: "B" }),
    ];
    const { result } = renderHook(() => useNodeExecutionState(events));
    expect(result.current.nodeStates["node_1"]).toBeUndefined();
    expect(result.current.nodeStates["node_2"].executionState).toBe("running");
  });

  it("edgeExecutionStates maps source node running state to edge", () => {
    const events: RunspaceEvent[] = [
      makeEvent({ type: "step-started", stepId: "node_1", stepLabel: "A" }),
    ];
    const { result } = renderHook(() => useNodeExecutionState(events));
    const edges: Edge[] = [{ id: "e1", source: "node_1", target: "node_2" } as Edge];
    const edgeStates = result.current.edgeExecutionStates(edges);
    expect(edgeStates["e1"]).toBe("running");
  });

  it("edgeExecutionStates returns idle for edges with non-running source", () => {
    const events: RunspaceEvent[] = [];
    const { result } = renderHook(() => useNodeExecutionState(events));
    const edges: Edge[] = [{ id: "e1", source: "node_1", target: "node_2" } as Edge];
    const edgeStates = result.current.edgeExecutionStates(edges);
    expect(edgeStates["e1"]).toBe("idle");
  });
});
