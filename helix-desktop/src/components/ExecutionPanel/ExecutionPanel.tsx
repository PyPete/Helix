import { useState } from "react";
import type { WslDocument, RunspaceEvent } from "../../types/agent";

interface ExecutionPanelProps {
  baseUrl: string;
  definition: WslDocument | null;
  runspaceEvents: RunspaceEvent[];
}

export function ExecutionPanel({
  baseUrl,
  definition,
  runspaceEvents,
}: ExecutionPanelProps) {
  const [runspaceId, setRunspaceId] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const eventsForRunspace = runspaceEvents.filter(
    (e: RunspaceEvent) => e.runspaceId === runspaceId
  );

  // ES2020-compatible: get last event for current runspace
  const eventsForStatus = runspaceEvents.filter(
    (e: RunspaceEvent) => e.runspaceId === runspaceId
  );
  const lastEvent =
    eventsForStatus.length > 0
      ? eventsForStatus[eventsForStatus.length - 1]
      : undefined;
  const currentStatus = lastEvent?.type ?? "idle";

  const isRunning =
    currentStatus === "runspace-started" || currentStatus === "step-started";
  const isPaused = currentStatus === "runspace-paused";

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
      const res = await fetch(baseUrl + "/runspaces", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(runspaceDef),
      });
      if (!res.ok) {
        setError("Failed to start runspace: " + res.statusText);
        return;
      }
      const data = await res.json();
      setRunspaceId(data.runspaceId);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setSubmitting(false);
    }
  };

  const handlePause = async () => {
    if (!runspaceId) return;
    try {
      const res = await fetch(baseUrl + "/runspaces/" + runspaceId + "/pause", {
        method: "POST",
      });
      if (!res.ok) setError("Failed to pause: " + res.statusText);
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const handleResume = async () => {
    if (!runspaceId) return;
    try {
      const res = await fetch(
        baseUrl + "/runspaces/" + runspaceId + "/resume",
        { method: "POST" }
      );
      if (!res.ok) setError("Failed to resume: " + res.statusText);
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const handleAbort = async () => {
    if (!runspaceId) return;
    try {
      const res = await fetch(baseUrl + "/runspaces/" + runspaceId + "/abort", {
        method: "POST",
      });
      if (!res.ok) setError("Failed to abort: " + res.statusText);
    } catch (e) {
      setError((e as Error).message);
    }
  };

  return (
    <section aria-label="Experiment Execution" data-testid="execution-panel">
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
      <div className="controls">
        <button
          onClick={handleStart}
          disabled={!definition || submitting || isRunning || isPaused}
          data-testid="start-btn"
        >
          Start
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
          disabled={!runspaceId || (!isRunning && !isPaused)}
          data-testid="abort-btn"
        >
          Abort
        </button>
      </div>
      {runspaceId && (
        <p data-testid="runspace-id">Runspace: {runspaceId}</p>
      )}
      <ul data-testid="step-list">
        {eventsForRunspace.map((e, i) => (
          <li key={i} data-testid={`step-event-${e.type}`}>
            {e.type}
          </li>
        ))}
      </ul>
    </section>
  );
}
