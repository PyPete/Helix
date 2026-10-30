import { useEffect, useState } from "react";
import {
  approveAgentPolicy,
  bootstrapAgentSession,
  dispatchAgentCommand,
  proposeAgentCommand,
  readAgentEvidence,
  type CommandProposal,
} from "../../lib/agentClient";

interface DeviceManualControlProps {
  deviceId: string;
  baseUrl: string;
}

type Capability = CommandProposal["capability"];

interface ManualCapability {
  capability: Capability;
  parameter?: string;
  unit?: string;
  options?: string[];
  minimum?: number;
  maximum?: number;
}

const capabilities: ManualCapability[] = [
  { capability: "SetFlow", parameter: "flowRate", unit: "mL/min", minimum: 0, maximum: 10 },
  { capability: "GetFlow" },
  { capability: "SetTemperature", parameter: "temperature", unit: "°C", minimum: 20, maximum: 80 },
  { capability: "GetTemperature" },
  { capability: "SetMode", parameter: "mode", options: ["Isocratic", "Gradient", "Purge"] },
  { capability: "GetMode" },
];

const genericAgentError = "Agent command failed.";
const sensitiveAssignment = /(["']?\b(?:password|passwd|token|access[_-]?token|refresh[_-]?token|api[_-]?key|secret)\b["']?\s*[:=]\s*)(?:"[^"]*"|'[^']*'|[^\s,;&}]+)/gi;
const bearerCredential = /\b(Bearer\s+)[A-Za-z0-9._~+/-]+=*/gi;

function normalizeAgentError(reason: unknown): string {
  const message = reason instanceof Error ? reason.message : typeof reason === "string" ? reason : "";
  if (!message.trim()) return genericAgentError;

  return message.trim()
    .replace(sensitiveAssignment, "$1[REDACTED]")
    .replace(bearerCredential, "$1[REDACTED]");
}

/** Manual input forms an Agent proposal only; the Agent remains execution authority. */
export default function DeviceManualControl({ deviceId }: DeviceManualControlProps) {
  const [sessionReady, setSessionReady] = useState(false);
  const [capability, setCapability] = useState<Capability>("SetFlow");
  const [value, setValue] = useState("1.0");
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    void bootstrapAgentSession()
      .then((session) => setSessionReady(session.authenticated))
      .catch(() => setMessage("The local Agent could not verify your desktop session. No command was dispatched. Restore the local session, then try again."));
  }, []);

  const selected = capabilities.find((item) => item.capability === capability) ?? capabilities[0];

  function changeCapability(nextCapability: Capability) {
    const next = capabilities.find((item) => item.capability === nextCapability);
    setCapability(nextCapability);
    setValue(next?.options?.[0] ?? (nextCapability === "SetTemperature" ? "20" : "1.0"));
    setMessage(null);
  }

  function parametersForSelection(): Record<string, number | string> | null {
    if (!selected.parameter) return {};
    if (selected.options) return { [selected.parameter]: value };

    const numericValue = Number(value);
    if (!Number.isFinite(numericValue) ||
      (selected.minimum !== undefined && numericValue < selected.minimum) ||
      (selected.maximum !== undefined && numericValue > selected.maximum)) {
      setMessage(`${selected.capability} requires a value between ${selected.minimum} and ${selected.maximum} ${selected.unit}. No command was dispatched.`);
      return null;
    }

    return { [selected.parameter]: numericValue };
  }

  async function submit() {
    const parameters = parametersForSelection();
    if (!parameters) return;

    setPending(true);
    setMessage(null);
    const commandId = crypto.randomUUID();

    try {
      const proposal = await proposeAgentCommand({
        commandId,
        executionId: crypto.randomUUID(),
        correlationId: crypto.randomUUID(),
        deviceId,
        capability,
        canonicalParameters: JSON.stringify(parameters),
        policyRevision: "simulation-policy-v1",
      });

      if (proposal.state !== "Allowed") {
        setMessage(proposal.state === "Held" ? "Held for scientist review" : "Rejected — not dispatched");
        return;
      }

      await approveAgentPolicy(commandId);
      const dispatch = await dispatchAgentCommand(commandId);
      if (dispatch.state !== "Dispatched" && dispatch.state !== "Completed") {
        setMessage(dispatch.state === "Held" ? "Held pending reconciliation" : "Rejected — not dispatched");
        return;
      }

      // Evidence is read after the Agent acknowledges dispatch; a proposal is never displayed as executed.
      const evidence = await readAgentEvidence(commandId);
      setMessage(`${evidence.evidence.state} command ${evidence.intent.commandId}`);
    } catch (reason) {
      setMessage(normalizeAgentError(reason));
    } finally {
      setPending(false);
    }
  }

  return (
    <section data-testid="device-manual-control">
      <h3>{deviceId} — Manual Control</h3>
      <label>
        Capability
        <select value={capability} onChange={(event) => changeCapability(event.target.value as Capability)}>
          {capabilities.map((item) => <option key={item.capability} value={item.capability}>{item.capability}</option>)}
        </select>
      </label>
      {selected.parameter && (selected.options ? (
        <label>
          Value
          <select value={value} onChange={(event) => setValue(event.target.value)}>
            {selected.options.map((option) => <option key={option}>{option}</option>)}
          </select>
        </label>
      ) : (
        <label>
          Value
          <input aria-label="Value" type="number" min={selected.minimum} max={selected.maximum} value={value} onChange={(event) => setValue(event.target.value)} /> {selected.unit}
        </label>
      ))}
      <button disabled={!sessionReady || pending} onClick={() => void submit()}>
        {pending ? "Awaiting acknowledgement…" : capability === "SetFlow" ? "Set Flow" : `Submit ${capability} proposal`}
      </button>
      {message && <p role={message.includes("Held") || message.includes("Rejected") || message.includes("No command") ? "alert" : "status"}>{message}</p>}
    </section>
  );
}
