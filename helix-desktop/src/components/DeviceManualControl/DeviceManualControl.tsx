import { useEffect, useState } from "react";
import { bootstrapAgentSession, executeApprovedSetFlow, type CommandEvidenceRecord } from "../../lib/agentClient";

interface DeviceManualControlProps {
  deviceId: string;
  baseUrl: string;
}

/** The renderer creates an untrusted proposal only; the native broker owns the session. */
export default function DeviceManualControl({ deviceId }: DeviceManualControlProps) {
  const [flowRate, setFlowRate] = useState("1.0");
  const [evidence, setEvidence] = useState<CommandEvidenceRecord | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void bootstrapAgentSession().catch(() => setError("Unable to establish the local desktop session."));
  }, []);

  async function handleSetFlow() {
    const value = Number(flowRate);
    if (!Number.isFinite(value)) {
      setError("Flow rate must be a finite number.");
      return;
    }
    setError(null);
    try {
      setEvidence(await executeApprovedSetFlow(value));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Agent command failed.");
    }
  }

  return (
    <section data-testid="device-manual-control">
      <h3>{deviceId} — Manual Control</h3>
      <label htmlFor="SetFlow.flowRate">Flow Rate</label>
      <input id="SetFlow.flowRate" type="number" min="0" max="10" step="0.1" value={flowRate}
        onChange={(event) => setFlowRate(event.target.value)} />
      <span>mL/min</span>
      <button onClick={handleSetFlow}>Set Flow</button>
      {error && <p role="alert">{error}</p>}
      {evidence && (
        <output data-testid="command-evidence" aria-label="Command evidence">
          {evidence.evidence.state} command {evidence.intent.commandId} by {evidence.intent.verifiedActorDisplayName}
        </output>
      )}
    </section>
  );
}
