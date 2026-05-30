import { useState, useEffect, useRef } from "react";
import type { DeviceAdapter, WslDocument } from "../../types/agent";
import { exportToWsl } from "./nodes/HelixNodes";
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
      // Editor not yet initialised -- export empty WSL document
      const wsl = exportToWsl(
        { getNodes: () => [], getConnections: () => [] } as never,
        "My Workflow"
      );
      onExport(wsl);
      return;
    }
    const wsl = exportToWsl(editor, "My Workflow");
    onExport(wsl);
  };

  const handleSave = async () => {
    if (!editor) return;
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
    // Phase 6 scope: load triggers onExport callback with loaded doc
    // (editor re-population is Phase 7 polish)
    onExport(doc);
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
          <select aria-label="Device capabilities" data-testid="capability-picker">
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
      <div
        ref={containerRef}
        style={{ width: "100%", height: "600px" }}
        data-testid="rete-canvas"
      />
    </section>
  );
}