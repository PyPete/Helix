import type { AgentHealthState } from '../../hooks/useAgentHealth';
import { useBoardInteractionPolicy } from '../../hooks/useBoardInteractionPolicy';
import { useCommandEvidence } from '../../hooks/useCommandEvidence';
import type { RunspaceEvent } from '../../types/agent';
import { adaptCommandEvidence, adaptOperationEvidence } from './BoardEvidenceAdapter';
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

export function ScientificWorkflowBoard({
  health,
  runspaceEvents,
  streamState,
  selectedExecution,
  durableLifecycle,
  onShowEvidence,
}: ScientificWorkflowBoardProps) {
  const disposition = useBoardInteractionPolicy(health);
  const operationEvidence = adaptOperationEvidence(null);
  const commandEvidenceState = useCommandEvidence(selectedExecution.executionId);
  const commandEvidence = adaptCommandEvidence(selectedExecution.executionId, commandEvidenceState);

  return (
    <main className="scientific-workflow-board" aria-labelledby="scientific-workflow-board-title" data-testid="scientific-workflow-board">
      <ReconciliationStrip disposition={disposition} onShowEvidence={onShowEvidence} />
      <header className="scientific-workflow-board__header">
        <p>Helix scientific workspace</p>
        <h1 id="scientific-workflow-board-title">Workflow Board</h1>
        <p>Proposal-only view. The local Helix Agent retains authority for validation and physical execution.</p>
      </header>
      <section aria-label="Source-labelled presentation context" data-testid="board-presentation-context">
        <h2>Presentation context</h2>
        <p data-source={streamState.source}>
          Renderer stream: {streamState.loading ? 'loading' : streamState.connected ? 'connected' : 'disconnected'}
          {streamState.partial ? ' (partial)' : ''}.
          {streamState.error ? ` Error: ${streamState.error}` : ''}
        </p>
        <p data-source={selectedExecution.source}>
          Local execution selection: {selectedExecution.executionId ?? 'none'}; selection is not an Agent receipt.
        </p>
        <p data-source={durableLifecycle.source}>
          Local event-derived lifecycle: {durableLifecycle.value ?? 'unavailable'}; not an Agent reconciliation disposition.
        </p>
        <p>Received runspace events: {runspaceEvents.length}; event presentation does not establish reconciliation.</p>
      </section>
      <section aria-label="Operation evidence" data-source={operationEvidence.source} data-status={operationEvidence.status}>
        <h2>Operation evidence</h2>
        <p>No board-originated scientific operation is available.</p>
      </section>
      <section aria-label="Command evidence" data-source={commandEvidence.source} data-status={commandEvidence.status}>
        <h2>Command evidence</h2>
        <p>
          {commandEvidence.status === 'unavailable' && commandEvidence.reason === 'execution-id-unavailable' && 'Command evidence is unavailable without a selected execution ID.'}
          {commandEvidence.status === 'unavailable' && commandEvidence.reason === 'history-unavailable' && 'Agent command evidence history is unavailable.'}
          {commandEvidence.status === 'loading' && 'Loading Agent command evidence.'}
          {commandEvidence.status === 'error' && 'The Agent command evidence read failed.'}
          {commandEvidence.status === 'empty' && 'No committed command evidence records were returned.'}
          {commandEvidence.status === 'available' && 'Agent command records are available; they do not establish lifecycle reconciliation or approval.'}
        </p>
        {commandEvidence.error && <p role="alert">{commandEvidence.error}</p>}
        {commandEvidence.records.length > 0 && (
          <ol aria-label="Agent command evidence records">
            {commandEvidence.records.map((record) => (
              <li key={record.evidenceId} data-evidence-id={record.evidenceId}>
                {record.evidenceId} — sequence {record.sequence}; command {record.commandId}; outcome {record.outcome}.
              </li>
            ))}
          </ol>
        )}
      </section>
      <section className="scientific-workflow-board__health" aria-label="Agent eligibility observation" aria-live="polite">
        <h2>Agent health</h2>
        <p>{disposition.healthObservation}</p>
        <p>Health is not reconciliation authority. Scientific lifecycle and evidence are unavailable.</p>
      </section>
    </main>
  );
}
