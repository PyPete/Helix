import { useEffect, useState } from 'react';
import type { AgentHealthState } from '../../hooks/useAgentHealth';
import { useBoardInteractionPolicy } from '../../hooks/useBoardInteractionPolicy';
import { useCommandEvidence } from '../../hooks/useCommandEvidence';
import { useProposalEditability } from '../../hooks/useProposalEditability';
import {
  scientificWorkflowProposalStore,
  type ProposalInventory,
  type ProposalStorePort,
} from '../../lib/scientificWorkflowProposalStore';
import type { CapabilityDescriptor, RunspaceEvent } from '../../types/agent';
import { adaptCommandEvidence, adaptOperationEvidence } from './BoardEvidenceAdapter';
import { createBoardReadModels, type BoardReadModel } from './BoardReadModel';
import { CapabilityPalette } from './CapabilityPalette';
import { ReconciliationStrip } from './ReconciliationStrip';
import { TypedInspector } from './TypedInspector';
import { WorkflowStructure } from './WorkflowStructure';
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
  proposalStore?: ProposalStorePort & { listProposals: () => Promise<ProposalInventory> };
}

export function ScientificWorkflowBoard({
  health,
  runspaceEvents,
  streamState,
  selectedExecution,
  durableLifecycle,
  onShowEvidence,
  proposalStore = scientificWorkflowProposalStore,
}: ScientificWorkflowBoardProps) {
  const disposition = useBoardInteractionPolicy(health);
  const [inspectedCapability, setInspectedCapability] = useState<CapabilityDescriptor | null>(null);
  const operationEvidence = adaptOperationEvidence(null);
  const commandEvidenceState = useCommandEvidence(selectedExecution.executionId);
  const commandEvidence = adaptCommandEvidence(selectedExecution.executionId, commandEvidenceState);
  const readModels = createBoardReadModels({ run: commandEvidence });
  const proposal = useProposalEditability(proposalStore);
  const [inventory, setInventory] = useState<ProposalInventory>({ proposals: [], issues: [] });
  const [inventoryError, setInventoryError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;
    void proposalStore.listProposals().then((value) => {
      if (mounted) {
        setInventory(value);
        setInventoryError(null);
      }
    }).catch((cause: unknown) => {
      if (mounted) setInventoryError(cause instanceof Error ? cause.message : 'Saved local proposals are unavailable.');
    });
    return () => { mounted = false; };
  }, [proposalStore]);

  async function saveProposal() {
    const acknowledgement = await proposal.saveProposal();
    if (acknowledgement) {
      try {
        setInventory(await proposalStore.listProposals());
        setInventoryError(null);
      } catch (cause) {
        setInventoryError(cause instanceof Error ? cause.message : 'Saved local proposal inventory could not be refreshed.');
      }
    }
  }

  return (
    <main className="scientific-workflow-board" aria-labelledby="scientific-workflow-board-title" data-testid="scientific-workflow-board">
      <ReconciliationStrip disposition={disposition} onShowEvidence={onShowEvidence} />
      <header className="scientific-workflow-board__header">
        <p>Helix scientific workspace</p>
        <h1 id="scientific-workflow-board-title">Workflow Board</h1>
        <p>Proposal-only view. The local Helix Agent retains authority for validation and physical execution.</p>
      </header>
      <section aria-label="Local proposal workspace" data-testid="local-proposal-workspace">
        <h2>Local proposal</h2>
        {!proposal.proposal && <button type="button" onClick={proposal.beginCreate}>Create Proposal</button>}
        {proposal.proposal && !proposal.isEditing && <button type="button" onClick={proposal.beginEdit}>Edit Proposal</button>}
        {proposal.isEditing && proposal.proposal && (
          <>
            <label>
              Proposal title
              <input
                aria-label="Proposal title"
                value={proposal.proposal.content.title}
                onChange={(event) => proposal.updateContent((content) => ({ ...content, title: event.currentTarget.value }))}
              />
            </label>
            <button type="button" onClick={() => { void saveProposal(); }}>Save Proposal</button>
          </>
        )}
        {proposal.proposal && !proposal.isEditing && <p>{proposal.proposal.content.title}</p>}
        {proposal.proposal && <p>Local proposal revision {proposal.proposal.contentRevision}</p>}
        {proposal.status && <p role="status" aria-live="polite">{proposal.status}</p>}
        {proposal.error && <p role="alert">{proposal.error}</p>}
        {inventoryError && <p role="alert">{inventoryError}</p>}
        {inventory.issues.map((issue) => <p role="alert" key={issue.fileName}>{issue.fileName}: {issue.message}</p>)}
        {!proposal.proposal && inventory.proposals.map((item) => (
          <button type="button" key={item.proposalId} onClick={() => { void proposal.resumeProposal(item.proposalId); }}>
            Resume Proposal: {item.content.title}
          </button>
        ))}
      </section>
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
      <CapabilityPalette onInspect={setInspectedCapability} />
      <TypedInspector capability={inspectedCapability} canEdit={disposition.canEdit} />
      <WorkflowStructure
        canEdit={disposition.canEdit}
        resourceLeaseDiagnostics={commandEvidence.records.flatMap((record) => record.checks)}
      />
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
      {readModels.map((model) => renderReadModel(model))}
      <section className="scientific-workflow-board__health" aria-label="Agent eligibility observation" aria-live="polite">
        <h2>Agent health</h2>
        <p>{disposition.healthObservation}</p>
        <p>Health is not reconciliation authority. Scientific lifecycle and evidence are unavailable.</p>
      </section>
    </main>
  );
}

function renderReadModel(model: BoardReadModel) {
  const title = model.view === 'samples-analysis'
    ? 'Samples and analysis'
    : `${model.view[0].toUpperCase()}${model.view.slice(1)}`;
  const statusText = describeReadModel(model);

  return (
    <section
      key={model.view}
      aria-label={`${title} view`}
      data-testid={`board-view-${model.view}`}
      data-source={model.status === 'available' ? model.source : model.view === 'run' ? model.source : 'unavailable'}
      data-status={model.status}
    >
      <h2>{title}</h2>
      <p>{statusText}</p>
      {model.view === 'run' && model.error && <p role="alert">{model.error}</p>}
      {model.status === 'available' && (
        <pre aria-label={`${title} source record values`}>{JSON.stringify(model.records, null, 2)}</pre>
      )}
      {model.view === 'run' && model.records.length > 0 && model.status !== 'available' && (
        <pre aria-label="Partial Agent command evidence records">{JSON.stringify(model.records, null, 2)}</pre>
      )}
    </section>
  );
}

function describeReadModel(model: BoardReadModel): string {
  if (model.view === 'run') {
    switch (model.status) {
      case 'available':
        return 'Agent command evidence records are shown as supplied; they do not establish lifecycle reconciliation or approval.';
      case 'loading':
        return 'Loading cursor-backed Agent command evidence.';
      case 'empty':
        return 'No committed command evidence records were returned.';
      case 'error':
        return 'Unavailable — the Agent command evidence read failed.';
      case 'unavailable':
        return model.reason === 'execution-id-unavailable'
          ? 'Unavailable — no execution ID is selected for evidence lookup.'
          : 'Unavailable — Agent command evidence history is unavailable.';
    }
  }

  if (model.status === 'available') {
    return 'Agent-backed record values are shown as supplied; no related view or lifecycle is inferred.';
  }
  if (model.view === 'results') return 'Unavailable — no reviewed analytical-result record source is supported.';
  return `Unavailable — no Agent-native ${model.view} record was supplied.`;
}
