import { useEffect, useMemo, useState } from "react";
import type { RunspaceEvent, WslDocument } from "../../types/agent";
import {
  bootstrapAgentSession,
  controlAgentExperiment,
  submitSimulationWorkflow,
  type AdmissionCheck,
  type SimulationPolicy,
  type WorkflowSubmissionResult,
} from "../../lib/agentClient";

interface ExecutionPanelProps {
  baseUrl: string;
  definition: WslDocument | null;
  runspaceEvents: RunspaceEvent[];
}

const REQUIRED_CHECKS = ["Capability", "Safety", "Lease", "Resource", "Budget", "Termination", "Device health"];

const initialLimits = {
  flowRate: "0–10 mL/min in 0.1 steps",
  temperature: "20–80 °C in 1 °C steps",
  mode: "Isocratic, Gradient, or Purge",
  maximumMutations: 100,
  wallTimeMinutes: 10,
  leaseSeconds: 30,
};

function checksForDisplay(checks: AdmissionCheck[] | undefined): AdmissionCheck[] {
  return REQUIRED_CHECKS.map((check) => checks?.find((item) => item.check === check) ?? {
    check,
    status: "Unknown",
    code: "NOT_RECORDED",
    reason: "Not recorded",
  });
}

/** A desktop request never grants execution: runtime admission is rendered from Agent evidence. */
export function ExecutionPanel({ definition, runspaceEvents }: ExecutionPanelProps) {
  const [sessionReady, setSessionReady] = useState(false);
  const [policyApproved, setPolicyApproved] = useState(false);
  const [limits, setLimits] = useState(initialLimits);
  const [submitting, setSubmitting] = useState(false);
  const [controlPending, setControlPending] = useState<string | null>(null);
  const [result, setResult] = useState<WorkflowSubmissionResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [abortConfirmationOpen, setAbortConfirmationOpen] = useState(false);

  useEffect(() => {
    void bootstrapAgentSession()
      .then((session) => setSessionReady(session.authenticated))
      .catch(() => setError("The local Agent could not verify your desktop session. No command was dispatched. Restore the local session, then try again."));
  }, []);

  const hasExecutableNodes = definition !== null && definition.nodes.length > 0;
  const experimentId = result?.executionId ?? runspaceEvents.at(-1)?.experimentId ?? null;
  const runtimeChecks = checksForDisplay(result?.runtimeAdmission?.checks);
  const hasReturnedUnknownChecks = result !== null && runtimeChecks.some((check) => check.status === "Unknown");
  const policy: SimulationPolicy = { revision: "simulation-policy-v1", approved: policyApproved, limits };
  // Before an Agent response this button creates a proposal. Once evidence contains gaps, no retry is offered.
  const canSubmitProposal = sessionReady && hasExecutableNodes && policyApproved && !submitting && !hasReturnedUnknownChecks;
  const canControl = sessionReady && experimentId !== null && result?.runtimeAdmission?.state !== "Held" && result?.runtimeAdmission?.state !== "Rejected";

  const status = useMemo(() => {
    if (result?.runtimeAdmission?.state === "Held") return "Held for scientist review";
    if (result?.runtimeAdmission?.state === "Rejected") return "Rejected — not dispatched";
    if (result?.runtimeAdmission?.state === "Dispatched") return "Dispatched";
    return "Proposal not dispatched";
  }, [result]);

  function updateLimit<Key extends keyof typeof limits>(key: Key, value: typeof limits[Key]) {
    setLimits((current) => ({ ...current, [key]: value }));
    setPolicyApproved(false);
  }

  async function handleStart() {
    if (!definition || !canSubmitProposal) return;

    setSubmitting(true);
    setError(null);
    try {
      const response = await submitSimulationWorkflow({
        operationId: crypto.randomUUID(),
        executionId: crypto.randomUUID(),
        correlationId: crypto.randomUUID(),
        originalWsl: definition,
        definitionReference: definition.documentId,
        policy: { ...policy, approved: true },
      });
      setResult(response);

      if (response.runtimeAdmission?.state === "Rejected") {
        setError(`Command rejected: ${response.runtimeAdmission.reason ?? "Admission checks failed"}. No command was dispatched. Review the failed checks and correct the proposal.`);
      } else if (response.runtimeAdmission?.state === "Held") {
        setError("Outcome not confirmed. This command is held pending reconciliation; do not retry it.");
      } else if (checksForDisplay(response.runtimeAdmission?.checks).some((check) => check.status === "Unknown")) {
        setError("Admission checks are incomplete. The outcome is unavailable; do not retry this submission.");
      }
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "The local Agent could not verify your desktop session. No command was dispatched. Restore the local session, then try again.");
    } finally {
      setSubmitting(false);
    }
  }

  async function requestControl(control: "pause" | "hold" | "abort") {
    if (!experimentId || !canControl) return;

    setControlPending(control);
    setError(null);
    try {
      const outcome = await controlAgentExperiment(experimentId, control);
      if (outcome.uncertain) {
        setError("Outcome not confirmed. This command is held pending reconciliation; do not retry it.");
      }
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Control request could not be acknowledged.");
    } finally {
      setControlPending(null);
      setAbortConfirmationOpen(false);
    }
  }

  return (
    <section aria-label="Experiment Execution" data-testid="execution-panel">
      <h2>Simulation execution</h2>
      {error && <p role="alert">{error}</p>}
      <p role="status" data-testid="execution-status">{status}</p>
      <fieldset>
        <legend>Simulation limits</legend>
        <label>Flow <input aria-label="Flow limit" disabled={policyApproved} value={limits.flowRate} onChange={(event) => updateLimit("flowRate", event.target.value)} /></label>
        <label>Temperature <input aria-label="Temperature limit" disabled={policyApproved} value={limits.temperature} onChange={(event) => updateLimit("temperature", event.target.value)} /></label>
        <label>Mode <input aria-label="Mode limit" disabled={policyApproved} value={limits.mode} onChange={(event) => updateLimit("mode", event.target.value)} /></label>
        <label>Maximum mutations <input aria-label="Maximum mutations" type="number" min="1" disabled={policyApproved} value={limits.maximumMutations} onChange={(event) => updateLimit("maximumMutations", Number(event.target.value))} /></label>
        <label>Wall time (minutes) <input aria-label="Wall time" type="number" min="1" disabled={policyApproved} value={limits.wallTimeMinutes} onChange={(event) => updateLimit("wallTimeMinutes", Number(event.target.value))} /></label>
        <label>Lease (seconds) <input aria-label="Lease duration" type="number" min="1" disabled={policyApproved} value={limits.leaseSeconds} onChange={(event) => updateLimit("leaseSeconds", Number(event.target.value))} /></label>
        <label>
          <input type="checkbox" checked={policyApproved} onChange={(event) => setPolicyApproved(event.target.checked)} />
          I approve this frozen simulation policy revision
        </label>
      </fieldset>
      {!hasExecutableNodes && <p role="status">Workflow has no executable nodes — add steps in the Builder tab first.</p>}
      <section aria-label="Admission checks">
        <h3>Admission checks</h3>
        <ul>{runtimeChecks.map((check) => <li key={check.check}><strong>{check.check}:</strong> {check.status} <details><summary>Reason</summary><span>{check.reason ?? "Unavailable"} ({check.code})</span></details></li>)}</ul>
      </section>
      <div className="controls">
        <button onClick={handleStart} disabled={!canSubmitProposal} data-testid="start-btn">{submitting ? "Submitting…" : "Start simulation"}</button>
        <button onClick={() => void requestControl("pause")} disabled={!canControl || controlPending !== null}>{controlPending === "pause" ? "Pause pending…" : "Pause"}</button>
        <button onClick={() => void requestControl("hold")} disabled={!canControl || controlPending !== null}>{controlPending === "hold" ? "Hold pending…" : "Hold"}</button>
        <button onClick={() => setAbortConfirmationOpen(true)} disabled={!canControl || controlPending !== null}>Abort</button>
      </div>
      {abortConfirmationOpen && <div role="dialog" aria-modal="true" aria-labelledby="abort-title"><h3 id="abort-title">Abort simulation</h3><p>Abort this run? No new commands will start. An in-flight action may already have completed; Helix will record its observed outcome and will not replay it.</p><button onClick={() => void requestControl("abort")}>Abort simulation</button><button onClick={() => setAbortConfirmationOpen(false)}>Keep running</button></div>}
      {result && <p data-testid="execution-id">Execution: <code>{result.executionId}</code></p>}
    </section>
  );
}
