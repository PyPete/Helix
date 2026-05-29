import type { SafetyEvent } from "../../types/agent";

interface SafetyPanelProps {
  safetyEvents: SafetyEvent[];
}

export function SafetyPanel({ safetyEvents }: SafetyPanelProps) {
  // Derive latest state per rule (last event for each ruleId wins)
  const latestByRule = safetyEvents.reduce(
    (acc, evt) => {
      acc[evt.ruleId] = evt;
      return acc;
    },
    {} as Record<string, SafetyEvent>
  );

  const rules = Object.values(latestByRule);
  const anyTriggered = rules.some((r) => r.triggered);

  return (
    <section aria-label="Safety Status" data-testid="safety-panel">
      <h3>Safety Status</h3>
      {anyTriggered && (
        <p role="alert" className="error" data-testid="safety-alert">
          SAFETY TRIGGERED
        </p>
      )}
      {rules.length === 0 && (
        <p data-testid="no-rules">No interlock rules active</p>
      )}
      <ul data-testid="rule-list">
        {rules.map((rule) => (
          <li
            key={rule.ruleId}
            data-testid={`rule-${rule.ruleId}`}
            className={rule.triggered ? "triggered" : "safe"}
          >
            <strong>{rule.description}</strong> — {rule.severity} —{" "}
            {rule.triggered ? "TRIGGERED" : "Safe"}
          </li>
        ))}
      </ul>
    </section>
  );
}
