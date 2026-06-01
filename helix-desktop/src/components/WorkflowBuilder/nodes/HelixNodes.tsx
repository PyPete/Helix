import { memo } from "react";
import type React from "react";
import { Handle, Position, type NodeProps, useEdges } from "reactflow";
import type { ParameterSchema, ParameterType } from "../../../types/agent";

// ---- Shared types ----

export type HelixNodeType =
  | "Step"
  | "Condition"
  | "Branch"
  | "Loop"
  | "Parallel"
  | "Delay"
  | "WaitFor"
  | "DeviceAction";

export type ExecutionState = "idle" | "running" | "succeeded" | "failed";

export interface HelixNodeData {
  nodeType: HelixNodeType;
  label: string;
  capability?: string;
  condition?: string;
  iterations?: number;
  durationMs?: number;
  executionState?: ExecutionState;
  startedAt?: string;
  estimatedCompletionAt?: string;
  finishedAt?: string;
  faultMessage?: string;
}

// ---- Shared node styles ----

function borderColour(state: ExecutionState | undefined): string {
  switch (state) {
    case "running":   return "#f5a623";   // amber
    case "succeeded": return "#27ae60";   // green
    case "failed":    return "#e74c3c";   // red
    default:          return "#3a3a4a";   // idle dark
  }
}

function nodeOpacity(state: ExecutionState | undefined): number {
  return state === "succeeded" ? 0.6 : 1;
}

const baseNode: React.CSSProperties = {
  background: "#1e1e2e",
  borderRadius: 6,
  minWidth: 160,
  fontFamily: "Inter, monospace, sans-serif",
  fontSize: 12,
  color: "#cdd6f4",
  boxShadow: "0 2px 8px rgba(0,0,0,0.5)",
};

function nodeStyle(state: ExecutionState | undefined, extra?: React.CSSProperties): React.CSSProperties {
  return {
    ...baseNode,
    border: `1.5px solid ${borderColour(state)}`,
    opacity: nodeOpacity(state),
    boxShadow: state === "running"
      ? `0 0 10px 2px rgba(245,166,35,0.35), 0 2px 8px rgba(0,0,0,0.5)`
      : state === "failed"
      ? `0 0 10px 2px rgba(231,76,60,0.35), 0 2px 8px rgba(0,0,0,0.5)`
      : "0 2px 8px rgba(0,0,0,0.5)",
    ...extra,
  };
}

const headerStyle = (colour: string): React.CSSProperties => ({
  background: colour,
  padding: "5px 10px",
  borderRadius: "4px 4px 0 0",
  fontWeight: 600,
  fontSize: 11,
  letterSpacing: "0.05em",
  textTransform: "uppercase",
  color: "#fff",
  display: "flex",
  alignItems: "center",
  gap: 6,
});

const bodyStyle: React.CSSProperties = {
  padding: "8px 10px",
  display: "flex",
  flexDirection: "column",
  gap: 5,
};

// ---- NodeTimingFooter (internal, not exported) ----

function formatHHMMSS(ms: number): string {
  const totalSec = Math.max(0, Math.floor(ms / 1000));
  const h = Math.floor(totalSec / 3600);
  const m = Math.floor((totalSec % 3600) / 60);
  const s = totalSec % 60;
  return [h, m, s].map((v) => String(v).padStart(2, "0")).join(":");
}

interface NodeTimingFooterProps {
  executionState?: ExecutionState;
  startedAt?: string;
  estimatedCompletionAt?: string;
  finishedAt?: string;
  faultMessage?: string;
}

const timingFooterBase: React.CSSProperties = {
  padding: "4px 10px",
  fontSize: 10,
  borderTop: "1px solid #2a2a3e",
  color: "#6c7086",
  fontFamily: "monospace",
};

function NodeTimingFooter({
  executionState,
  startedAt,
  estimatedCompletionAt,
  finishedAt,
}: NodeTimingFooterProps) {
  if (!executionState || executionState === "idle") return null;

  const now = Date.now();

  if (executionState === "running") {
    const startMs = startedAt ? Date.parse(startedAt) : NaN;
    const elapsed = !isNaN(startMs) ? now - startMs : 0;
    const etaMs = estimatedCompletionAt ? Date.parse(estimatedCompletionAt) : NaN;
    const etaPart =
      !isNaN(etaMs) && etaMs > now
        ? `  ETA ~${formatHHMMSS(etaMs - now)}`
        : "";
    return (
      <div style={{ ...timingFooterBase, color: "#f5a623" }} data-testid="timing-footer-running">
        {`▶ ${formatHHMMSS(elapsed)}${etaPart}`}
      </div>
    );
  }

  if (executionState === "succeeded") {
    const startMs = startedAt ? Date.parse(startedAt) : NaN;
    const finMs = finishedAt ? Date.parse(finishedAt) : NaN;
    const durationMs = !isNaN(startMs) && !isNaN(finMs) ? finMs - startMs : 0;
    const totalSec = Math.max(0, Math.floor(durationMs / 1000));
    const mins = Math.floor(totalSec / 60);
    const secs = totalSec % 60;
    const label = mins > 0 ? `${mins}m ${secs}s` : `${secs}s`;
    return (
      <div style={{ ...timingFooterBase, color: "#27ae60" }} data-testid="timing-footer-succeeded">
        {`✓ Finished in ${label}`}
      </div>
    );
  }

  if (executionState === "failed") {
    const finMs = finishedAt ? Date.parse(finishedAt) : NaN;
    const timeStr = !isNaN(finMs)
      ? new Date(finMs).toTimeString().slice(0, 8)
      : "--:--:--";
    return (
      <div style={{ ...timingFooterBase, color: "#e74c3c" }} data-testid="timing-footer-failed">
        {`✗ Failed at ${timeStr}`}
      </div>
    );
  }

  return null;
}

const handleStyle = (colour: string): React.CSSProperties => ({
  background: colour,
  border: "2px solid #1e1e2e",
  width: 10,
  height: 10,
  borderRadius: "50%",
});

const execHandle = handleStyle("#a6adc8");   // grey-white — control flow
const fieldStyle: React.CSSProperties = {
  background: "#2a2a3e",
  border: "1px solid #3a3a4a",
  borderRadius: 4,
  padding: "3px 6px",
  color: "#cdd6f4",
  fontSize: 12,
  width: "100%",
  boxSizing: "border-box",
};

// ---- Step node ----

export const StepNodeComponent = memo(({ data, id }: NodeProps<HelixNodeData>) => {
  const state = data.executionState;
  return (
    <div style={nodeStyle(state)} data-testid={`node-step-${id}`}>
      <Handle type="target" position={Position.Left} id="exec-in" style={execHandle} />
      <div style={headerStyle("#5b5ea6")}>⚡ Step</div>
      <div style={bodyStyle}>
        <input
          className="nodrag"
          style={fieldStyle}
          placeholder="capability"
          defaultValue={data.capability ?? ""}
          data-testid="step-capability-input"
        />
      </div>
      <NodeTimingFooter
        executionState={data.executionState}
        startedAt={data.startedAt}
        estimatedCompletionAt={data.estimatedCompletionAt}
        finishedAt={data.finishedAt}
        faultMessage={data.faultMessage}
      />
      <Handle type="source" position={Position.Right} id="exec-out" style={execHandle} />
    </div>
  );
});
StepNodeComponent.displayName = "StepNode";

// ---- Condition node ----

export const ConditionNodeComponent = memo(({ data, id }: NodeProps<HelixNodeData>) => {
  const state = data.executionState;
  return (
    <div style={nodeStyle(state)} data-testid={`node-condition-${id}`}>
      <Handle type="target" position={Position.Left} id="exec-in" style={execHandle} />
      <div style={headerStyle("#c0392b")}>⟨⟩ Condition</div>
      <div style={bodyStyle}>
        <input
          className="nodrag"
          style={fieldStyle}
          placeholder="condition expression"
          defaultValue={data.condition ?? ""}
          data-testid="condition-input"
        />
      </div>
      <NodeTimingFooter
        executionState={data.executionState}
        startedAt={data.startedAt}
        estimatedCompletionAt={data.estimatedCompletionAt}
        finishedAt={data.finishedAt}
        faultMessage={data.faultMessage}
      />
      <Handle type="source" position={Position.Right} id="true-out"
        style={{ ...handleStyle("#27ae60"), top: "60%" }} />
      <Handle type="source" position={Position.Right} id="false-out"
        style={{ ...handleStyle("#e74c3c"), top: "80%" }} />
    </div>
  );
});
ConditionNodeComponent.displayName = "ConditionNode";

// ---- Branch node ----

export const BranchNodeComponent = memo(({ data, id }: NodeProps<HelixNodeData>) => {
  const state = data.executionState;
  return (
    <div style={nodeStyle(state)} data-testid={`node-branch-${id}`}>
      <Handle type="target" position={Position.Left} id="exec-in" style={execHandle} />
      <div style={headerStyle("#6c3483")}>⑂ Branch</div>
      <div style={bodyStyle}>
        <span style={{ color: "#6c7086", fontSize: 11 }}>Splits execution into two paths</span>
      </div>
      <NodeTimingFooter
        executionState={data.executionState}
        startedAt={data.startedAt}
        estimatedCompletionAt={data.estimatedCompletionAt}
        finishedAt={data.finishedAt}
        faultMessage={data.faultMessage}
      />
      <Handle type="source" position={Position.Right} id="branch-a"
        style={{ ...execHandle, top: "55%" }} />
      <Handle type="source" position={Position.Right} id="branch-b"
        style={{ ...execHandle, top: "75%" }} />
    </div>
  );
});
BranchNodeComponent.displayName = "BranchNode";

// ---- Loop node ----

export const LoopNodeComponent = memo(({ data, id }: NodeProps<HelixNodeData>) => {
  const state = data.executionState;
  return (
    <div style={nodeStyle(state)} data-testid={`node-loop-${id}`}>
      <Handle type="target" position={Position.Left} id="exec-in" style={execHandle} />
      <div style={headerStyle("#1a6b4a")}>↻ Loop</div>
      <div style={bodyStyle}>
        <label style={{ color: "#6c7086", fontSize: 11 }}>Iterations</label>
        <input
          className="nodrag"
          type="number"
          style={fieldStyle}
          defaultValue={data.iterations ?? 1}
          min={1}
          data-testid="loop-iterations-input"
        />
      </div>
      <NodeTimingFooter
        executionState={data.executionState}
        startedAt={data.startedAt}
        estimatedCompletionAt={data.estimatedCompletionAt}
        finishedAt={data.finishedAt}
        faultMessage={data.faultMessage}
      />
      <Handle type="source" position={Position.Right} id="body"
        style={{ ...execHandle, top: "55%" }} />
      <Handle type="source" position={Position.Right} id="exec-out"
        style={{ ...execHandle, top: "75%" }} />
    </div>
  );
});
LoopNodeComponent.displayName = "LoopNode";

// ---- Parallel node ----

export const ParallelNodeComponent = memo(({ data, id }: NodeProps<HelixNodeData>) => {
  const state = data.executionState;
  return (
    <div style={nodeStyle(state)} data-testid={`node-parallel-${id}`}>
      <Handle type="target" position={Position.Left} id="exec-in" style={execHandle} />
      <div style={headerStyle("#1a5276")}>⫸ Parallel</div>
      <div style={bodyStyle}>
        <span style={{ color: "#6c7086", fontSize: 11 }}>Runs branches concurrently</span>
      </div>
      <NodeTimingFooter
        executionState={data.executionState}
        startedAt={data.startedAt}
        estimatedCompletionAt={data.estimatedCompletionAt}
        finishedAt={data.finishedAt}
        faultMessage={data.faultMessage}
      />
      <Handle type="source" position={Position.Right} id="branch-a"
        style={{ ...execHandle, top: "50%" }} />
      <Handle type="source" position={Position.Right} id="branch-b"
        style={{ ...execHandle, top: "65%" }} />
      <Handle type="source" position={Position.Right} id="exec-out"
        style={{ ...execHandle, top: "80%" }} />
    </div>
  );
});
ParallelNodeComponent.displayName = "ParallelNode";

// ---- Delay node ----

export const DelayNodeComponent = memo(({ data, id }: NodeProps<HelixNodeData>) => {
  const state = data.executionState;
  return (
    <div style={nodeStyle(state)} data-testid={`node-delay-${id}`}>
      <Handle type="target" position={Position.Left} id="exec-in" style={execHandle} />
      <div style={headerStyle("#7d6608")}>⏱ Delay</div>
      <div style={bodyStyle}>
        <label style={{ color: "#6c7086", fontSize: 11 }}>Duration (ms)</label>
        <input
          className="nodrag"
          type="number"
          style={fieldStyle}
          defaultValue={data.durationMs ?? 1000}
          min={0}
          data-testid="delay-duration-input"
        />
      </div>
      <NodeTimingFooter
        executionState={data.executionState}
        startedAt={data.startedAt}
        estimatedCompletionAt={data.estimatedCompletionAt}
        finishedAt={data.finishedAt}
        faultMessage={data.faultMessage}
      />
      <Handle type="source" position={Position.Right} id="exec-out" style={execHandle} />
    </div>
  );
});
DelayNodeComponent.displayName = "DelayNode";

// ---- WaitFor node ----

export const WaitForNodeComponent = memo(({ data, id }: NodeProps<HelixNodeData>) => {
  const state = data.executionState;
  return (
    <div style={nodeStyle(state)} data-testid={`node-waitfor-${id}`}>
      <Handle type="target" position={Position.Left} id="exec-in" style={execHandle} />
      <div style={headerStyle("#117a65")}>⏸ WaitFor</div>
      <div style={bodyStyle}>
        <input
          className="nodrag"
          style={fieldStyle}
          placeholder="condition"
          defaultValue={data.condition ?? ""}
          data-testid="waitfor-condition-input"
        />
      </div>
      <NodeTimingFooter
        executionState={data.executionState}
        startedAt={data.startedAt}
        estimatedCompletionAt={data.estimatedCompletionAt}
        finishedAt={data.finishedAt}
        faultMessage={data.faultMessage}
      />
      <Handle type="source" position={Position.Right} id="exec-out" style={execHandle} />
    </div>
  );
});
WaitForNodeComponent.displayName = "WaitForNode";

// ---- DeviceAction node ----

export interface DeviceActionNodeData {
  nodeType: "DeviceAction";
  adapterId: string;
  capability: string;
  parameters: ParameterSchema[];
  fieldValues: Record<string, unknown>;
  executionState?: ExecutionState;
  label: string;
  startedAt?: string;
  estimatedCompletionAt?: string;
  finishedAt?: string;
  faultMessage?: string;
}

export const PORT_COLOURS: Record<ParameterType | "exec", string> = {
  Numeric:  "#5b8dd9",
  Enum:     "#e67e22",
  Boolean:  "#27ae60",
  String:   "#e91e8c",
  Duration: "#5b8dd9",
  exec:     "#a6adc8",
};

export const DeviceActionNodeComponent = memo(({ data, id }: NodeProps<DeviceActionNodeData>) => {
  const state = data.executionState;
  const edges = useEdges();

  return (
    <div style={nodeStyle(state)} data-testid={`node-deviceaction-${id}`}>
      <Handle
        type="target"
        position={Position.Left}
        id="exec-in"
        style={handleStyle(PORT_COLOURS.exec)}
      />
      <div style={headerStyle("#7c3aed")}>{data.adapterId} › {data.capability}</div>
      <div style={bodyStyle}>
        {(data.parameters ?? []).map((param) => {
          const isWired = edges.some(
            (e) => e.targetHandle === param.name && e.target === id
          );
          return (
            <div key={param.name} style={{ display: "flex", alignItems: "center", gap: 6, position: "relative" }}>
              <Handle
                type="target"
                position={Position.Left}
                id={param.name}
                style={handleStyle(PORT_COLOURS[param.type] ?? PORT_COLOURS.exec)}
              />
              {isWired ? (
                <span
                  data-testid={`wire-indicator-${param.name}`}
                  style={{ color: "#6c7086", fontSize: 11 }}
                >
                  ↤ wired
                </span>
              ) : (
                <>
                  <label style={{ color: "#6c7086", fontSize: 11, minWidth: 60 }}>
                    {param.displayName || param.name}
                  </label>
                  {param.type === "Numeric" && (
                    <>
                      <input
                        type="number"
                        className="nodrag"
                        style={fieldStyle}
                        min={param.min}
                        max={param.max}
                        step={param.step ?? 1}
                        defaultValue={param.defaultValue as number | undefined}
                      />
                      {param.unit && <span style={{ color: "#6c7086", fontSize: 11 }}>{param.unit}</span>}
                    </>
                  )}
                  {param.type === "Duration" && (
                    <>
                      <input
                        type="number"
                        className="nodrag"
                        style={fieldStyle}
                        min={param.min}
                        max={param.max}
                        step={param.step ?? 1}
                        defaultValue={param.defaultValue as number | undefined}
                      />
                      <span style={{ color: "#6c7086", fontSize: 11 }}>ms</span>
                    </>
                  )}
                  {param.type === "Enum" && (
                    <select
                      className="nodrag"
                      style={fieldStyle}
                      defaultValue={param.defaultValue as string | undefined}
                    >
                      {param.options?.map((o) => (
                        <option key={o} value={o}>{o}</option>
                      ))}
                    </select>
                  )}
                  {param.type === "Boolean" && (
                    <input
                      type="checkbox"
                      className="nodrag"
                      defaultChecked={Boolean(param.defaultValue)}
                    />
                  )}
                  {param.type === "String" && (
                    <input
                      type="text"
                      className="nodrag"
                      style={fieldStyle}
                      defaultValue={String(param.defaultValue ?? "")}
                    />
                  )}
                </>
              )}
            </div>
          );
        })}
      </div>
      <NodeTimingFooter
        executionState={data.executionState}
        startedAt={data.startedAt}
        estimatedCompletionAt={data.estimatedCompletionAt}
        finishedAt={data.finishedAt}
        faultMessage={data.faultMessage}
      />
      <Handle
        type="source"
        position={Position.Right}
        id="exec-out"
        style={handleStyle(PORT_COLOURS.exec)}
      />
    </div>
  );
});
DeviceActionNodeComponent.displayName = "DeviceActionNode";

// ---- nodeTypes map (pass to <ReactFlow nodeTypes={NODE_TYPES}>) ----

export const NODE_TYPES = {
  Step: StepNodeComponent,
  Condition: ConditionNodeComponent,
  Branch: BranchNodeComponent,
  Loop: LoopNodeComponent,
  Parallel: ParallelNodeComponent,
  Delay: DelayNodeComponent,
  WaitFor: WaitForNodeComponent,
  DeviceAction: DeviceActionNodeComponent,
} as const;
