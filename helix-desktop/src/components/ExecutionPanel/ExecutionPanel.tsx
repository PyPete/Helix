import { useState } from "react";
import type { WslDocument, RunspaceEvent } from "../../types/agent";

interface ExecutionPanelProps {
  baseUrl: string;
  definition: WslDocument | null;
  runspaceEvents: RunspaceEvent[];
}

const STATUS_LABELS: Record<string, string> = {
  idle: "Idle",
  "runspace-started": "Running",
  "step-started": "Running",
  "runspace-paused": "Paused",
  "runspace-completed": "Completed",
  "runspace-aborted": "Aborted",
  "runspace-faulted": "Faulted",
};

const STATUS_COLORS: Record<string, string> = {
  idle: "#6c7086",
  "runspace-started": "#f5a623",
  "step-started": "#f5a623",
  "runspace-paused": "#89b4fa",
  "runspace-completed": "#27ae60",
  "runspace-aborted": "#6c7086",
  "runspace-faulted": "#e74c3c",
};

export function ExecutionPanel({
  baseUrl,
  definition,
  runspaceEvents,
}: ExecutionPanelProps) {
  const [experimentId, setExperimentId] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const eventsForRunspace = runspaceEvents.filter(
    (e: RunspaceEvent) => e.experimentId === experimentId
  );

  const eventsForStatus = runspaceEvents.filter(
    (e: RunspaceEvent) => e.experimentId === experimentId
  );
  const lastEvent =
    eventsForStatus.length > 0
      ? eventsForStatus[eventsForStatus.length - 1]
      : undefined;
  const currentStatus = lastEvent?.type ?? "idle";

  const isRunning =
    currentStatus === "runspace-started" || currentStatus === "step-started";
  const isPaused = currentStatus === "runspace-paused";

  const hasExecutableNodes =
    definition !== null &&
    definition.nodes.some((n) =>
      ["Step", "DeviceAction", "Delay", "WaitFor", "Condition", "Branch", "Loop", "Parallel"].includes(n.nodeType)
    );

  const handleCompile = async (): Promise<unknown | null> => {
    if (!definition) {
      setError("No workflow loaded");
      return null;
    }
    const compileRes = await fetch(baseUrl + "/workflows/compile", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(definition),
    });
    if (compileRes.status === 422) {
      const diagnostics = await compileRes.json();
      setError("Compile errors: " + JSON.stringify(diagnostics));
      return null;
    }
    if (!compileRes.ok) {
      setError("Compilation failed: " + compileRes.statusText);
      return null;
    }
    const runspaceDef = await compileRes.json();
    return runspaceDef;
  };

  const handleStart = async () => {
    if (!definition) {
      setError("No workflow loaded");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const runspaceDef = await handleCompile();
      if (runspaceDef === null) {
        return;
      }
      const res = await fetch(baseUrl + "/experiments", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: definition.name ?? "Experiment" }),
      });
      if (!res.ok) {
        setError("Failed to start experiment: " + res.statusText);
        return;
      }
      const data = await res.json();
      setExperimentId(data.experimentId);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setSubmitting(false);
    }
  };

  const handlePause = async () => {
    if (!experimentId) return;
    try {
      const res = await fetch(baseUrl + "/experiments/" + experimentId + "/pause", {
        method: "POST",
      });
      if (!res.ok) setError("Failed to pause: " + res.statusText);
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const handleResume = async () => {
    if (!experimentId) return;
    try {
      const res = await fetch(
        baseUrl + "/experiments/" + experimentId + "/resume",
        { method: "POST" }
      );
      if (!res.ok) setError("Failed to resume: " + res.statusText);
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const handleAbort = async () => {
    if (!experimentId) return;
    try {
      const res = await fetch(baseUrl + "/experiments/" + experimentId + "/abort", {
        method: "POST",
      });
      if (!res.ok) setError("Failed to abort: " + res.statusText);
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const statusColor = STATUS_COLORS[currentStatus] ?? "#6c7086";
  const statusLabel = STATUS_LABELS[currentStatus] ?? currentStatus;

  return (
    <section aria-label="Experiment Execution" data-testid="execution-panel">
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
      {definition && !hasExecutableNodes && (
        <p
          role="status"
          data-testid="no-nodes-warning"
          style={{ color: "#f5a623", fontSize: "0.85rem", margin: "4px 0" }}
        >
          Workflow has no executable nodes — add steps in the Builder tab first.
        </p>
      )}
      <div className="controls">
        <button
          onClick={handleStart}
          disabled={!definition || submitting || isRunning || isPaused}
          data-testid="start-btn"
        >
          {submitting ? "Starting…" : "Start"}
        </button>
        <button
          onClick={handlePause}
          disabled={!isRunning}
          data-testid="pause-btn"
        >
          Pause
        </button>
        <button
          onClick={handleResume}
          disabled={!isPaused}
          data-testid="resume-btn"
        >
          Resume
        </button>
        <button
          onClick={handleAbort}
          disabled={!experimentId || (!isRunning && !isPaused)}
          data-testid="abort-btn"
        >
          Abort
        </button>
        <span
          data-testid="runspace-status"
          style={{
            marginLeft: 12,
            color: statusColor,
            fontWeight: 600,
            fontSize: "0.9rem",
          }}
        >
          {statusLabel}
        </span>
      </div>
      {experimentId && (
        <p data-testid="experiment-id" style={{ fontSize: "0.75rem", color: "#6c7086" }}>
          Experiment: {experimentId}
        </p>
      )}
      <ul data-testid="step-list" style={{ listStyle: "none", padding: 0, margin: "8px 0" }}>
        {eventsForRunspace.map((e, i) => {
          const isStep = e.type === "step-started" || e.type === "step-completed" || e.type === "step-faulted";
          const stepLabel = isStep ? String(e["stepLabel"] ?? e["stepId"] ?? "") : "";
          const eventColor =
            e.type === "step-completed" || e.type === "runspace-completed"
              ? "#27ae60"
              : e.type === "step-faulted" || e.type === "runspace-faulted"
              ? "#e74c3c"
              : e.type === "step-started" || e.type === "runspace-started"
              ? "#f5a623"
              : "#6c7086";
          return (
            <li
              key={i}
              data-testid={`step-event-${e.type}`}
              style={{ color: eventColor, fontSize: "0.8rem", padding: "2px 0" }}
            >
              {e.type}{stepLabel ? ` — ${stepLabel}` : ""}
            </li>
          );
        })}
      </ul>
    </section>
  );
}
