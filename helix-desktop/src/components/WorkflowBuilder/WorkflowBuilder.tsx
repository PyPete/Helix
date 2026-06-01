import { useState, useCallback, useRef, useEffect } from "react";
import { ReactFlow,
  Background,
  BackgroundVariant,
  Controls,
  MiniMap,
  type ReactFlowInstance,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";

import type { CapabilityDescriptor, DeviceAdapter, WslDocument } from "../../types/agent";
import { NODE_TYPES, type HelixNodeType } from "./nodes/HelixNodes";
import { EDGE_TYPES } from "./edges/AnimatedHelixEdge";
import { useHelixFlow, type HelixNode, type HelixEdge } from "./useHelixFlow";
import { saveWorkflow, loadWorkflow, listWorkflows } from "../../lib/workflowLibrary";
import { DevicePalette } from "./DevicePalette";
import { useAgentWebSocket } from "../../hooks/useAgentWebSocket";
import { useNodeExecutionState, type NodeExecutionState } from "../../hooks/useNodeExecutionState";

interface WorkflowBuilderProps {
  baseUrl: string;
  onExport: (wsl: WslDocument) => void;
}

// Context menu state
interface CtxMenu {
  x: number;
  y: number;
  flowPosition: { x: number; y: number };
}

const CANVAS_STYLE: React.CSSProperties = {
  background: "#141420",
  width: "100%",
  height: "100%",
  borderRadius: 6,
  border: "1px solid #2a2a3e",
};

const MINIMAP_STYLE: React.CSSProperties = {
  background: "#1e1e2e",
  border: "1px solid #3a3a4a",
};

const NODE_MENU_TYPES: HelixNodeType[] = [
  "Step", "Condition", "Branch", "Loop", "Parallel", "Delay", "WaitFor",
];

export function WorkflowBuilder({ baseUrl, onExport }: WorkflowBuilderProps) {
  const [devices, setDevices] = useState<DeviceAdapter[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [ctxMenu, setCtxMenu] = useState<CtxMenu | null>(null);
  const canvasWrapperRef = useRef<HTMLDivElement>(null);

  // Pointer-event drag state (works in both browser and Tauri WebView2)
  const [activeDrag, setActiveDrag] = useState<{
    adapterId: string;
    capability: string;
    descriptor: CapabilityDescriptor;
    x: number;
    y: number;
  } | null>(null);
  // Ref keeps drag data accessible inside stable event listeners without re-registering on every move.
  const activeDragRef = useRef<typeof activeDrag>(null);
  const [workflowName, setWorkflowName] = useState("Untitled Workflow");
  const [savedWorkflows, setSavedWorkflows] = useState<string[]>([]);
  const [saveStatus, setSaveStatus] = useState<string | null>(null);
  const [selectedCapability, setSelectedCapability] = useState<string>("");
  const rfInstance = useRef<ReactFlowInstance<HelixNode, HelixEdge> | null>(null);

  const { nodes, edges, onNodesChange, onEdgesChange, onConnect, addNode, addDeviceActionNode, exportWsl, setNodes, setEdges } =
    useHelixFlow();

  const { runspaceEvents } = useAgentWebSocket(baseUrl);
  const { nodeStates, edgeExecutionStates } = useNodeExecutionState(runspaceEvents);

  // Merge execution state + timing into node data.
  // Always write (even undefined) so that runspace-started correctly clears previous run's colours.
  useEffect(() => {
    setNodes((nds) =>
      nds.map((n) => {
        const ns: NodeExecutionState | undefined = nodeStates[n.id];
        const isRunning = ns?.executionState === "running";
        return {
          ...n,
          deletable: !isRunning,
          data: {
            ...n.data,
            executionState: ns?.executionState,
            startedAt: ns?.startedAt?.toISOString(),
            estimatedCompletionAt: ns?.estimatedCompletionAt?.toISOString(),
            finishedAt: ns?.finishedAt?.toISOString(),
            faultMessage: ns?.faultMessage,
          },
        };
      })
    );
  }, [nodeStates, setNodes]);

  // Update edge execution states to mirror source node state
  useEffect(() => {
    setEdges((eds) => {
      const states = edgeExecutionStates(eds);
      return eds.map((e) => ({
        ...e,
        data: { ...e.data, executionState: states[e.id] ?? "idle" },
      }));
    });
  }, [nodeStates, edgeExecutionStates, setEdges]);

  // Load devices
  useState(() => {
    let cancelled = false;
    const load = async () => {
      try {
        const res = await fetch(baseUrl + "/devices");
        if (!res.ok) throw new Error("Agent returned " + res.status);
        const data: DeviceAdapter[] = await res.json();
        if (!cancelled) setDevices(data);
      } catch (e) {
        if (!cancelled) setError((e as Error).message);
      }
    };
    load();
    return () => { cancelled = true; };
  });

  // Auto-export whenever the canvas changes so ExecutionPanel is always armed.
  useEffect(() => {
    const wsl = exportWsl(workflowName);
    onExport(wsl);
  }, [exportWsl, workflowName, onExport]);

  const handleExport = () => {
    const wsl = exportWsl(workflowName);
    console.log("[WSL Export]", JSON.stringify(wsl, null, 2));
    onExport(wsl);
  };

  const handleSave = async () => {
    const wsl = exportWsl(workflowName);
    await saveWorkflow(workflowName, wsl);
    setSaveStatus("Saved");
    setTimeout(() => setSaveStatus(null), 2000);
  };

  const handleLoadList = async () => {
    const names = await listWorkflows();
    setSavedWorkflows(names);
  };

  const handleLoad = async (name: string) => {
    const doc = await loadWorkflow(name);
    onExport(doc);
  };

  const handleCapabilityChange = (cap: string) => {
    setSelectedCapability(cap);
  };

  // Right-click on canvas pane opens node-type context menu
  const onPaneContextMenu = useCallback(
    (e: MouseEvent | React.MouseEvent) => {
      e.preventDefault();
      const bounds = (e.currentTarget as HTMLElement).getBoundingClientRect();
      const flowPos = rfInstance.current?.screenToFlowPosition({
        x: e.clientX,
        y: e.clientY,
      }) ?? { x: 0, y: 0 };
      setCtxMenu({ x: e.clientX - bounds.left, y: e.clientY - bounds.top, flowPosition: flowPos });
    },
    []
  );

  const onPaneClick = useCallback(() => setCtxMenu(null), []);

  // Only allow exec?exec or data?parameter connections; block exec?parameter cross-wiring.
  const isValidConnection = useCallback(
    (connection: { sourceHandle?: string | null; targetHandle?: string | null }) => {
      const src = connection.sourceHandle ?? "";
      const tgt = connection.targetHandle ?? "";
      // Exec source: anything except ret-* (return-value data ports).
      // Exec target: only exec-in. This allows branch-a/branch-b/body/true-out/false-out ? exec-in.
      const srcIsExec = !src.startsWith("ret-");
      const tgtIsExec = tgt === "exec-in";
      // exec?exec or data?data; block exec?param and param?exec
      return srcIsExec === tgtIsExec;
    },
    []
  );

  // Keep ref in sync so stable event listeners always read current drag data.
  useEffect(() => { activeDragRef.current = activeDrag; }, [activeDrag]);

  // Register listeners once; read drag data via ref to avoid re-registration on every pointermove.
  useEffect(() => {
    const onMove = (e: PointerEvent) => {
      if (!activeDragRef.current) return;
      setActiveDrag((d) => d ? { ...d, x: e.clientX, y: e.clientY } : null);
    };

    const onUp = (e: PointerEvent) => {
      const d = activeDragRef.current;
      if (!d) return;
      const canvas = canvasWrapperRef.current;
      if (canvas) {
        const rect = canvas.getBoundingClientRect();
        if (e.clientX >= rect.left && e.clientX <= rect.right &&
            e.clientY >= rect.top  && e.clientY <= rect.bottom) {
          const position = rfInstance.current?.screenToFlowPosition({
            x: e.clientX,
            y: e.clientY,
          }) ?? { x: 200, y: 200 };
          addDeviceActionNode(d.adapterId, d.capability, d.descriptor, position);
        }
      }
      setActiveDrag(null);
    };

    window.addEventListener("pointermove", onMove);
    window.addEventListener("pointerup", onUp);
    return () => {
      window.removeEventListener("pointermove", onMove);
      window.removeEventListener("pointerup", onUp);
    };
  }, [addDeviceActionNode]); // stable — never re-registers

  const handleAddNode = (type: HelixNodeType) => {
    addNode(type, ctxMenu?.flowPosition);
    setCtxMenu(null);
  };

  const nodeCount = nodes.length;

  return (
    <section aria-label="Workflow Builder" data-testid="workflow-builder">
      {error && <p role="alert" className="error">{error}</p>}

      {/* Toolbar */}
      <div className="toolbar">
        <button onClick={handleExport} data-testid="export-wsl-btn">Export WSL</button>
        <input
          type="text"
          value={workflowName}
          onChange={(e) => setWorkflowName(e.target.value)}
          placeholder="Workflow name"
          data-testid="workflow-name-input"
        />
        <button onClick={handleSave} data-testid="save-workflow-btn">Save</button>
        {saveStatus && <span data-testid="save-status">{saveStatus}</span>}
        <button onClick={handleLoadList} data-testid="list-workflows-btn">Load...</button>
        {savedWorkflows.length > 0 && (
          <select
            onChange={(e) => { if (e.target.value) handleLoad(e.target.value); }}
            data-testid="workflow-select"
          >
            <option value="">-- select workflow --</option>
            {savedWorkflows.map((name) => (
              <option key={name} value={name}>{name}</option>
            ))}
          </select>
        )}
        {devices.length > 0 && (
          <select
            aria-label="Device capabilities"
            data-testid="capability-picker"
            value={selectedCapability}
            onChange={(e) => handleCapabilityChange(e.target.value)}
          >
            {devices.flatMap((d) =>
              d.capabilities.map((cap) => (
                <option key={d.adapterId + "-" + cap} value={cap}>
                  {d.adapterId}: {cap}
                </option>
              ))
            )}
          </select>
        )}
      </div>

      {/* Canvas with device palette */}
      <div style={{ display: "flex", height: "600px" }}>
        <DevicePalette
          baseUrl={baseUrl}
          onDragStart={(adapterId, capability, descriptor) =>
            setActiveDrag({ adapterId, capability, descriptor, x: 0, y: 0 })
          }
        />
        <div
          ref={canvasWrapperRef}
          style={{ flex: 1, position: "relative" }}
          data-testid="rete-canvas"
        >
          <ReactFlow
            nodes={nodes}
            edges={edges}
            onNodesChange={onNodesChange}
            onEdgesChange={onEdgesChange}
            onConnect={onConnect}
            onInit={(instance: ReactFlowInstance<HelixNode, HelixEdge>) => { rfInstance.current = instance; }}
            nodeTypes={NODE_TYPES}
            edgeTypes={EDGE_TYPES}
            defaultEdgeOptions={{ type: "helixEdge" }}
            style={CANVAS_STYLE}
            onPaneContextMenu={onPaneContextMenu}
            onPaneClick={onPaneClick}
            isValidConnection={isValidConnection}
            deleteKeyCode={["Backspace", "Delete"]}
            fitView
            proOptions={{ hideAttribution: true }}
          >
            <Background
              variant={BackgroundVariant.Dots}
              color="#2a2a3e"
              gap={20}
              size={1}
            />
            <Controls
              style={{ background: "#1e1e2e", border: "1px solid #3a3a4a", color: "#cdd6f4" }}
            />
            <MiniMap
              style={MINIMAP_STYLE}
              nodeColor={(node) => {
                const state = (node.data as { executionState?: string }).executionState;
                if (state === "running")   return "#f5a623";
                if (state === "succeeded") return "#27ae60";
                if (state === "failed")    return "#e74c3c";
                return "#5b5ea6";
              }}
              maskColor="rgba(20,20,32,0.7)"
            />

            {/* Empty hint */}
            {nodeCount === 0 && (
              <div
                data-testid="canvas-empty-hint"
                style={{
                  position: "absolute",
                  top: "50%",
                  left: "50%",
                  transform: "translate(-50%,-50%)",
                  color: "#45475a",
                  pointerEvents: "none",
                  userSelect: "none",
                  fontSize: "0.85rem",
                  textAlign: "center",
                }}
              >
                Right-click to add nodes
              </div>
            )}
          </ReactFlow>

          {/* Context menu */}
          {ctxMenu && (
            <div
              data-testid="node-context-menu"
              style={{
                position: "absolute",
                top: ctxMenu.y,
                left: ctxMenu.x,
                background: "#1e1e2e",
                border: "1px solid #3a3a4a",
                borderRadius: 6,
                padding: "4px 0",
                zIndex: 10,
                minWidth: 140,
                boxShadow: "0 4px 16px rgba(0,0,0,0.5)",
              }}
            >
              <div style={{ padding: "4px 12px", color: "#6c7086", fontSize: 11, fontWeight: 600 }}>
                ADD NODE
              </div>
              {NODE_MENU_TYPES.map((type) => (
                <button
                  key={type}
                  onClick={() => handleAddNode(type)}
                  data-testid={`add-node-${type.toLowerCase()}`}
                  style={{
                    display: "block",
                    width: "100%",
                    background: "transparent",
                    border: "none",
                    color: "#cdd6f4",
                    padding: "6px 12px",
                    textAlign: "left",
                    cursor: "pointer",
                    fontSize: 13,
                  }}
                  onMouseEnter={(e) => { (e.target as HTMLElement).style.background = "#2a2a3e"; }}
                  onMouseLeave={(e) => { (e.target as HTMLElement).style.background = "transparent"; }}
                >
                  {type}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>
      {/* Drag ghost — follows cursor while a capability card is being dragged */}
      {activeDrag && activeDrag.x !== 0 && (
        <div
          style={{
            position: "fixed",
            left: activeDrag.x + 12,
            top: activeDrag.y + 12,
            background: "#2a2a3e",
            border: "1px solid #7c3aed",
            borderRadius: 4,
            padding: "4px 10px",
            fontSize: 12,
            color: "#cdd6f4",
            pointerEvents: "none",
            zIndex: 9999,
            boxShadow: "0 4px 12px rgba(0,0,0,0.5)",
          }}
        >
          {activeDrag.adapterId} › {activeDrag.capability}
        </div>
      )}
    </section>
  );
}

