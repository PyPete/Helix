import { useState, useEffect, useRef } from "react";
import type { ClassicPreset } from "rete";
import type { DeviceAdapter, WslDocument } from "../../types/agent";
import { exportToWsl, StepNode } from "./nodes/HelixNodes";
import { useReteEditor } from "./useReteEditor";
import { saveWorkflow, loadWorkflow, listWorkflows } from "../../lib/workflowLibrary";

interface WorkflowBuilderProps {
  baseUrl: string;
  onExport: (wsl: WslDocument) => void;
}

export function WorkflowBuilder({ baseUrl, onExport }: WorkflowBuilderProps) {
  const [devices, setDevices] = useState<DeviceAdapter[]>([]);
  const [error, setError] = useState<string | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const { editor } = useReteEditor(containerRef);
  const [workflowName, setWorkflowName] = useState("Untitled Workflow");
  const [savedWorkflows, setSavedWorkflows] = useState<string[]>([]);
  const [saveStatus, setSaveStatus] = useState<string | null>(null);
  const [selectedCapability, setSelectedCapability] = useState<string>("");
  const [nodeCount, setNodeCount] = useState(0);

  useEffect(() => {
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
    return () => {
      cancelled = true;
    };
  }, [baseUrl]);

  const handleExport = () => {
    if (!editor) {
      const wsl = exportToWsl(
        { getNodes: () => [], getConnections: () => [] } as never,
        "My Workflow"
      );
      onExport(wsl);
      return;
    }
    setNodeCount(editor.getNodes().length);
    const wsl = exportToWsl(editor, "My Workflow");
    onExport(wsl);
  };

  const handleSave = async () => {
    if (!editor) return;
    setNodeCount(editor.getNodes().length);
    const wsl = exportToWsl(editor, workflowName);
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
    if (!editor) return;
    const nodes = editor.getNodes();
    const stepNode = nodes.find((n) => n instanceof StepNode) as StepNode | undefined;
    if (stepNode) {
      const ctrl = stepNode.controls["capability"] as ClassicPreset.InputControl<"text">;
      ctrl.setValue(cap);
    }
  };

  return (
    <section aria-label="Workflow Builder" data-testid="workflow-builder">
      {error && <p role="alert" className="error">{error}</p>}
      <div className="toolbar">
        <button onClick={handleExport} data-testid="export-wsl-btn">
          Export WSL
        </button>
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
          <select onChange={(e) => { if (e.target.value) handleLoad(e.target.value); }} data-testid="workflow-select">
            <option value="">-- select workflow --</option>
            {savedWorkflows.map(name => <option key={name} value={name}>{name}</option>)}
          </select>
        )}
        {devices.length > 0 && (
          <select aria-label="Device capabilities" data-testid="capability-picker" value={selectedCapability} onChange={(e) => handleCapabilityChange(e.target.value)}>
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
      <div style={{ position: "relative", width: "100%", height: "600px" }}>
        <div
          ref={containerRef}
          style={{ width: "100%", height: "100%" }}
          data-testid="rete-canvas"
        />
        {nodeCount === 0 && (
          <span
            data-testid="canvas-empty-hint"
            style={{
              position: "absolute",
              top: "50%",
              left: "50%",
              transform: "translate(-50%, -50%)",
              color: "#888",
              pointerEvents: "none",
              userSelect: "none",
              fontSize: "0.9rem",
            }}
          >
            Right-click to add nodes
          </span>
        )}
      </div>
    </section>
  );
}