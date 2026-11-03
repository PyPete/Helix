import type { AgentHealthState } from '../../hooks/useAgentHealth';
import { useBoardInteractionPolicy } from '../../hooks/useBoardInteractionPolicy';
import type { RunspaceEvent } from '../../types/agent';
import { ReconciliationStrip } from './ReconciliationStrip';
import './scientificWorkflowBoard.css';

export interface BoardStreamState {
  source: 'renderer-stream-state';
  connected: boolean;
  loading: boolean;
  error: string | null;
  partial: boolean;
}

export interface BoardLocalSelection {
  source: 'local-selection';
  executionId: string | null;
}

export interface BoardLocalLifecycle {
  source: 'local-event-derived';
  value: string | null;
}

export interface ScientificWorkflowBoardProps {
  health: AgentHealthState;
  runspaceEvents: RunspaceEvent[];
  streamState: BoardStreamState;
  selectedExecution: BoardLocalSelection;
  durableLifecycle: BoardLocalLifecycle;
  onShowEvidence: () => void;
}

export function ScientificWorkflowBoard({ health, onShowEvidence }: ScientificWorkflowBoardProps) {
  const disposition = useBoardInteractionPolicy(health);

  return (
    <main className="scientific-workflow-board" aria-labelledby="scientific-workflow-board-title" data-testid="scientific-workflow-board">
      <ReconciliationStrip disposition={disposition} onShowEvidence={onShowEvidence} />
      <header className="scientific-workflow-board__header">
        <p>Helix scientific workspace</p>
        <h1 id="scientific-workflow-board-title">Workflow Board</h1>
        <p>Proposal-only view. The local Helix Agent retains authority for validation and physical execution.</p>
      </header>
      <section className="scientific-workflow-board__health" aria-label="Agent eligibility observation" aria-live="polite">
        <h2>Agent health</h2>
        <p>{disposition.healthObservation}</p>
        <p>Health is not reconciliation authority. Scientific lifecycle and evidence are unavailable.</p>
      </section>
    </main>
  );
}
