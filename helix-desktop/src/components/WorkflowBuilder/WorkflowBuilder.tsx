import { useState, useEffect, useRef } from "react";
import type { DeviceAdapter, WslDocument } from "../../types/agent";
import { exportToWsl } from "./nodes/HelixNodes";
import { useReteEditor } from "./useReteEditor";

interface WorkflowBuilderProps {
  baseUrl: string;
  onExport: (wsl: WslDocument) => void;
}

export function WorkflowBuilder({ baseUrl, onExport }: WorkflowBuilderProps) {
  const [devices, setDevices] = useState<DeviceAdapter[]>([]);
  const [error, setError] = useState<string | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const { editor } = useReteEditor(containerRef);

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

  return (
    <section aria-label="Workflow Builder" data-testid="workflow-builder">
      {error && <p role="alert" className="error">{error}</p>}
      <div className="toolbar">
        <button onClick={handleExport} data-testid="export-wsl-btn">
          Export WSL
        </button>
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
