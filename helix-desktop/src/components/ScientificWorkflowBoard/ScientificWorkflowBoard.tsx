import type { AgentHealthState } from '../../hooks/useAgentHealth';

export interface ScientificWorkflowBoardProps {
  health: AgentHealthState;
}

function getHealthObservation(health: AgentHealthState): string {
  if (health.error) return `Unavailable — ${health.error}`;
  if (health.healthy === true) return 'Healthy — eligibility observation only';
  if (health.healthy === false) return 'Unavailable';
  return 'Pending';
}

export function ScientificWorkflowBoard({ health }: ScientificWorkflowBoardProps) {
  return (
    <main aria-labelledby="scientific-workflow-board-title" data-testid="scientific-workflow-board">
      <header>
        <p>Helix scientific workspace</p>
        <h1 id="scientific-workflow-board-title">Workflow Board</h1>
        <p>Proposal-only view. The local Helix Agent retains authority for validation and physical execution.</p>
      </header>
      <section aria-label="Agent eligibility observation" aria-live="polite">
        <h2>Agent health</h2>
        <p>{getHealthObservation(health)}</p>
        <p>Health is not reconciliation authority. Scientific lifecycle and evidence are unavailable.</p>
      </section>
      <section aria-label="Reconciliation status">
        <h2>Read-only</h2>
        <p>No Agent-authoritative reconciliation evidence is available to enable proposal changes.</p>
      </section>
    </main>
  );
}
