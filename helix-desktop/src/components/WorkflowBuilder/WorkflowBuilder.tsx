import { useState, useCallback, useRef } from "react";
import ReactFlow, {
  Background,
  BackgroundVariant,
  Controls,
  MiniMap,
  type ReactFlowInstance,
} from "reactflow";
import "reactflow/dist/style.css";

import type { CapabilityDescriptor, DeviceAdapter, WslDocument } from "../../types/agent";
import { NODE_TYPES, type HelixNodeType } from "./nodes/HelixNodes";
import { EDGE_TYPES } from "./edges/AnimatedHelixEdge";
import { useHelixFlow } from "./useHelixFlow";
import { saveWorkflow, loadWorkflow, listWorkflows } from "../../lib/workflowLibrary";
import { DevicePalette } from "./DevicePalette";

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
  const [workflowName, setWorkflowName] = useState("Untitled Workflow");
  const [savedWorkflows, setSavedWorkflows] = useState<string[]>([]);
  const [saveStatus, setSaveStatus] = useState<string | null>(null);
  const [selectedCapability, setSelectedCapability] = useState<string>("");
  const rfInstance = useRef<ReactFlowInstance | null>(null);

  const { nodes, edges, onNodesChange, onEdgesChange, onConnect, addNode, addDeviceActionNode, exportWsl } =
    useHelixFlow();

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

  const handleExport = () => {
    const wsl = exportWsl(workflowName);
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
    (e: React.MouseEvent) => {
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

  const handleDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      const raw = e.dataTransfer.getData("application/helix-capability");
      if (!raw) return;
      try {
        const parsed = JSON.parse(raw) as {
          adapterId: string;
          capability: string;
          descriptor: CapabilityDescriptor;
        };
        const { adapterId, capability, descriptor } = parsed;
        if (!adapterId || !capability || !descriptor) return;
        const position = rfInstance.current?.screenToFlowPosition({
          x: e.clientX,
          y: e.clientY,
        }) ?? { x: 200, y: 200 };
        addDeviceActionNode(adapterId, capability, descriptor, position);
      } catch {
        // Invalid JSON payload — ignore drop per T-6.2-01
      }
    },
    [addDeviceActionNode]
  );

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
        <DevicePalette baseUrl={baseUrl} onDragStart={() => {}} />
        <div
          style={{ flex: 1, position: "relative" }}
          data-testid="rete-canvas"
          onDrop={handleDrop}
          onDragOver={(e) => e.preventDefault()}
        >
          <ReactFlow
            nodes={nodes}
            edges={edges}
            onNodesChange={onNodesChange}
            onEdgesChange={onEdgesChange}
            onConnect={onConnect}
            onInit={(instance) => { rfInstance.current = instance; }}
            nodeTypes={NODE_TYPES}
            edgeTypes={EDGE_TYPES}
            defaultEdgeOptions={{ type: "helixEdge" }}
            style={CANVAS_STYLE}
            onPaneContextMenu={onPaneContextMenu}
            onPaneClick={onPaneClick}
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
    </section>
  );
}
