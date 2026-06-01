import { useMemo } from "react";
import type { RunspaceEvent } from "../types/agent";
import type { ExecutionState } from "../components/WorkflowBuilder/nodes/HelixNodes";
import type { Edge } from "@xyflow/react";

export interface NodeExecutionState {
  executionState: ExecutionState;
  startedAt?: Date;
  estimatedCompletionAt?: Date;
  finishedAt?: Date;
  faultMessage?: string;
}

export interface UseNodeExecutionStateResult {
  nodeStates: Record<string, NodeExecutionState>;
  edgeExecutionStates: (edges: Edge[]) => Record<string, ExecutionState>;
}

export function useNodeExecutionState(
  runspaceEvents: RunspaceEvent[]
): UseNodeExecutionStateResult {
  const nodeStates = useMemo(() => {
    const states: Record<string, NodeExecutionState> = {};
    for (const event of runspaceEvents) {
      const stepId = String(event["stepId"] ?? "");
      if (event.type === "runspace-started") {
        // Clear all states — per T-6.2-04: reset on new runspace
        for (const key of Object.keys(states)) delete states[key];
      } else if (event.type === "step-started" && stepId) {
        const estimatedMs = event.estimatedDurationMs;
        const tsMs = Date.parse(event.timestamp);
        states[stepId] = {
          executionState: "running",
          startedAt: new Date(event.timestamp),
          estimatedCompletionAt:
            estimatedMs != null && !isNaN(tsMs)
              ? new Date(tsMs + estimatedMs)
              : undefined,
        };
      } else if (event.type === "step-completed" && stepId) {
        states[stepId] = {
          ...states[stepId],
          executionState: "succeeded",
          finishedAt: new Date(event.timestamp),
        };
      } else if (event.type === "step-faulted" && stepId) {
        states[stepId] = {
          ...states[stepId],
          executionState: "failed",
          finishedAt: new Date(event.timestamp),
          faultMessage: String(event["faultMessage"] ?? ""),
        };
      }
      // runspace-completed, runspace-aborted, and all other event types: no change
    }
    return states;
  }, [runspaceEvents]);

  const edgeExecutionStates = useMemo(
    () =>
      (edges: Edge[]): Record<string, ExecutionState> => {
        const result: Record<string, ExecutionState> = {};
        for (const edge of edges) {
          result[edge.id] = nodeStates[edge.source]?.executionState ?? "idle";
        }
        return result;
      },
    [nodeStates]
  );

  return { nodeStates, edgeExecutionStates };
}
