import { memo } from "react";
import {
  BaseEdge,
  getBezierPath,
  type EdgeProps,
} from "reactflow";

export interface HelixEdgeData {
  executionState?: "idle" | "running" | "succeeded" | "failed";
}

export const AnimatedHelixEdge = memo(
  ({
    id,
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
    data,
    markerEnd,
  }: EdgeProps<HelixEdgeData>) => {
    const [edgePath] = getBezierPath({
      sourceX,
      sourceY,
      sourcePosition,
      targetX,
      targetY,
      targetPosition,
    });

    const state = data?.executionState ?? "idle";

    const strokeColour =
      state === "running"   ? "#f5a623" :
      state === "succeeded" ? "#27ae60" :
      state === "failed"    ? "#e74c3c" :
      "#4a4a6a";

    const strokeWidth = state === "idle" ? 1.5 : 2;

    return (
      <>
        <BaseEdge
          id={id}
          path={edgePath}
          markerEnd={markerEnd}
          style={{ stroke: strokeColour, strokeWidth, opacity: 0.85 }}
        />
        {state === "running" && (
          <g>
            <path
              id={`motion-path-${id}`}
              d={edgePath}
              fill="none"
              stroke="transparent"
              strokeWidth={1}
            />
            <circle r={4} fill="#f5a623" filter="url(#dot-glow)">
              <animateMotion dur="1.2s" repeatCount="indefinite" rotate="auto">
                <mpath href={`#motion-path-${id}`} />
              </animateMotion>
            </circle>
          </g>
        )}
        <defs>
          <filter id="dot-glow" x="-50%" y="-50%" width="200%" height="200%">
            <feGaussianBlur stdDeviation="2" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>
      </>
    );
  }
);
AnimatedHelixEdge.displayName = "AnimatedHelixEdge";

export const EDGE_TYPES = {
  helixEdge: AnimatedHelixEdge,
} as const;

