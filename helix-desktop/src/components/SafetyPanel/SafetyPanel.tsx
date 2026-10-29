import type { SafetyEvent } from "../../types/agent";
import "./SafetyPanel.css";

export type SafetySnapshotState = "known" | "unknown" | "loading" | "error";

interface SafetyPanelProps {
  safetyEvents: SafetyEvent[];
  snapshotState?: SafetySnapshotState;
  snapshotError?: string | null;
}

export function SafetyPanel({ safetyEvents, snapshotState = "unknown", snapshotError = null }: SafetyPanelProps) {
  const latestByRule = safetyEvents.reduce<Record<string, SafetyEvent>>((acc, event) => ({ ...acc, [event.ruleId]: event }), {});
  const rules = Object.values(latestByRule);
  const anyTriggered = rules.some((rule) => rule.triggered);
  const stateText = snapshotState === "loading" ? "Safety snapshot loading" : snapshotState === "error" ? "Safety snapshot unavailable" : snapshotState === "unknown" ? "Safety state unknown" : "Safety snapshot recorded";

  return <section aria-label="Safety Status" data-testid="safety-panel" className="safety-panel">
    <h3>Safety Status</h3>
    <p role={snapshotState === "known" ? "status" : "alert"} className={snapshotState === "known" ? "safety-panel__known" : "safety-panel__unknown"}>{stateText}{snapshotError ? `: ${snapshotError}` : ''}</p>
    {anyTriggered && <p role="alert" className="safety-panel__triggered" data-testid="safety-alert">? SAFETY TRIGGERED — review interlock state</p>}
    {rules.length === 0 && <p data-testid="no-rules">No interlock rules active. Safety state is {snapshotState === "known" ? "recorded separately" : "unknown"}; this does not indicate it is safe to proceed.</p>}
    <ul data-testid="rule-list" className="safety-panel__rules">
      {rules.map((rule) => <li key={rule.ruleId} data-testid={`rule-${rule.ruleId}`} className={rule.triggered ? "safety-panel__rule--triggered" : "safety-panel__rule"}>
        <strong>{rule.triggered ? "TRIGGERED" : "Observed — not triggered"}</strong>: {rule.description || "Not recorded"} — Severity: {rule.severity || "Unavailable"}
        <span className="safety-panel__identifier"> Rule ID: {rule.ruleId || "Not recorded"}</span>
        {rule.currentValue !== undefined ? <span data-testid={`rule-value-${rule.ruleId}`}> — Current value: {rule.currentValue.toFixed(2)}</span> : <span> — Current value: Not recorded</span>}
      </li>)}
    </ul>
  </section>;
}
