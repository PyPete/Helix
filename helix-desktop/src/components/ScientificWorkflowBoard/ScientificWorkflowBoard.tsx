import type { AgentHealthState } from '../../hooks/useAgentHealth';
import { useBoardInteractionPolicy } from '../../hooks/useBoardInteractionPolicy';

export interface ScientificWorkflowBoardProps {
  health: AgentHealthState;
}

export function ScientificWorkflowBoard({ health }: ScientificWorkflowBoardProps) {
  const disposition = useBoardInteractionPolicy(health);

  return (
    <main aria-labelledby="scientific-workflow-board-title" data-testid="scientific-workflow-board">
      <header>
        <p>Helix scientific workspace</p>
        <h1 id="scientific-workflow-board-title">Workflow Board</h1>
        <p>Proposal-only view. The local Helix Agent retains authority for validation and physical execution.</p>
      </header>
      <section aria-label="Agent eligibility observation" aria-live="polite">
        <h2>Agent health</h2>
        <p>{disposition.healthObservation}</p>
        <p>Health is not reconciliation authority. Scientific lifecycle and evidence are unavailable.</p>
      </section>
      <section aria-label="Reconciliation status">
        <h2>{disposition.label}</h2>
        <p>{disposition.message}</p>
      </section>
    </main>
  );
}
