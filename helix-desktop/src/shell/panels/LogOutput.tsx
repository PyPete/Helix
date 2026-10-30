import type { RunspaceEvent, SafetyEvent } from '../../types/agent';
import './panels.css';

export interface LogOutputProps {
  runspaceEvents: RunspaceEvent[];
  safetyEvents: SafetyEvent[];
}

export function LogOutput({ runspaceEvents, safetyEvents }: LogOutputProps) {
  const triggeredSafetyEvents = safetyEvents.filter(e => e.triggered);

  return (
    <section aria-label="Log Output" data-testid="log-output">
      <ul className="log-list">
        {runspaceEvents.map((e, i) => (
          <li
            key={i}
            className="log-entry log-entry--runspace"
            data-testid={`log-runspace-${e.type}`}
          >
            {e.timestamp} {e.type}
          </li>
        ))}
        {triggeredSafetyEvents.map((e, i) => (
          <li
            key={`safety-${i}`}
            className="log-entry log-entry--safety"
            data-testid={`log-safety-${e.ruleId}`}
          >
            {e.timestamp} {e.ruleId} [{e.severity}] {e.description}
          </li>
        ))}
      </ul>
    </section>
  );
}
