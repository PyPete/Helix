import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useCommandEvidence } from '../../hooks/useCommandEvidence';
import type { CommandEvidenceHistoryState } from '../../types/agent';
import { ScientificWorkflowBoard } from './ScientificWorkflowBoard';

vi.mock('../../hooks/useCommandEvidence', () => ({ useCommandEvidence: vi.fn() }));

const commandEvidenceState: CommandEvidenceHistoryState = { records: [], loading: false, unavailable: false, error: null, refresh: vi.fn() };

beforeEach(() => {
  vi.mocked(useCommandEvidence).mockReturnValue(commandEvidenceState);
});

describe('ScientificWorkflowBoard reconciliation status', () => {
  it('shows exact If/Else ports and Fork/Join branches without inventing admission diagnostics', () => {
    render(
      <ScientificWorkflowBoard
        health={{ healthy: true, error: null, lastChecked: null }}
        runspaceEvents={[]}
        streamState={{ source: 'renderer-stream-state', connected: true, loading: false, error: null, partial: false }}
        selectedExecution={{ source: 'local-selection', executionId: null }}
        durableLifecycle={{ source: 'local-event-derived', value: null }}
        onShowEvidence={vi.fn()}
      />
    );

    expect(screen.getByRole('region', { name: 'If/Else proposal structure' })).toHaveTextContent('Condition evidence: unavailable');
    expect(screen.getByRole('group', { name: 'If/Else proposal ports' })).toHaveTextContent('True port');
    expect(screen.getByRole('group', { name: 'If/Else proposal ports' })).toHaveTextContent('False port');
    expect(screen.getByRole('region', { name: 'Fork/Join proposal structure' })).toHaveTextContent('Branch 1');
    expect(screen.getByRole('region', { name: 'Fork/Join proposal structure' })).toHaveTextContent('Branch 2');
    expect(screen.getByText('Agent resource and lease diagnostics: unavailable')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Modify If/Else proposal structure' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Modify Fork/Join proposal structure' })).toBeDisabled();
    expect(screen.queryByRole('button', { name: /execute|dispatch|approve|acknowledge|confirm/i })).toBeNull();
  });

  it('shows source-bounded capability metadata and read-only typed proposal fields', async () => {
    render(
      <ScientificWorkflowBoard
        health={{ healthy: true, error: null, lastChecked: null }}
        runspaceEvents={[]}
        streamState={{ source: 'renderer-stream-state', connected: true, loading: false, error: null, partial: false }}
        selectedExecution={{ source: 'local-selection', executionId: null }}
        durableLifecycle={{ source: 'local-event-derived', value: null }}
        onShowEvidence={vi.fn()}
      />
    );

    expect(await screen.findByText('Capability declarations are unavailable from the Agent.')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Typed proposal inspector' })).toHaveTextContent('Select an Agent-declared capability to inspect its typed fields.');
    expect(screen.getByText('Binding: unavailable')).toBeInTheDocument();
    expect(screen.getByText('Provenance: unavailable')).toBeInTheDocument();
    expect(screen.getByText('Quality and review state: unavailable')).toBeInTheDocument();
    expect(screen.getByText('Resources and leases: unavailable')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /add .* to proposal/i })).toBeNull();
    expect(screen.queryByRole('button', { name: /execute|dispatch|approve|acknowledge|confirm/i })).toBeNull();
  });

  it('keeps positive health read-only and offers only evidence navigation', () => {
    const onShowEvidence = vi.fn();
    render(
      <ScientificWorkflowBoard
        health={{ healthy: true, error: null, lastChecked: new Date('2026-11-03T12:00:00Z') }}
        runspaceEvents={[]}
        streamState={{ source: 'renderer-stream-state', connected: true, loading: false, error: null, partial: false }}
        selectedExecution={{ source: 'local-selection', executionId: null }}
        durableLifecycle={{ source: 'local-event-derived', value: null }}
        onShowEvidence={onShowEvidence}
      />
    );

    const status = screen.queryByRole('status');
    const evidenceLink = screen.queryByRole('button', { name: 'Review Agent evidence' });
    const hasAccessibleReadOnlyStatus = Boolean(
      status &&
      status.getAttribute('aria-live') === 'polite' &&
      status.textContent?.includes('Reconciliation evidence unavailable')
    );
    const hasNoAuthorityControls =
      screen.queryByRole('button', { name: /execute|dispatch|approve|acknowledge|confirm/i }) === null;

    expect(hasAccessibleReadOnlyStatus && evidenceLink !== null && hasNoAuthorityControls).toBe(true);
    if (evidenceLink) fireEvent.click(evidenceLink);
    expect(onShowEvidence).toHaveBeenCalledOnce();
  });

  it('reads command evidence only through the hook for the known selected execution ID', () => {
    const { rerender } = render(
      <ScientificWorkflowBoard
        health={{ healthy: true, error: null, lastChecked: null }}
        runspaceEvents={[]}
        streamState={{ source: 'renderer-stream-state', connected: false, loading: false, error: null, partial: false }}
        selectedExecution={{ source: 'local-selection', executionId: null }}
        durableLifecycle={{ source: 'local-event-derived', value: 'Running' }}
        onShowEvidence={vi.fn()}
      />
    );

    expect(useCommandEvidence).toHaveBeenLastCalledWith(null);
    rerender(
      <ScientificWorkflowBoard
        health={{ healthy: true, error: null, lastChecked: null }}
        runspaceEvents={[]}
        streamState={{ source: 'renderer-stream-state', connected: true, loading: false, error: null, partial: false }}
        selectedExecution={{ source: 'local-selection', executionId: 'execution-1' }}
        durableLifecycle={{ source: 'local-event-derived', value: 'Completed' }}
        onShowEvidence={vi.fn()}
      />
    );
    expect(useCommandEvidence).toHaveBeenLastCalledWith('execution-1');
  });

  it('reports operation evidence unavailable without a board-originated operation context', () => {
    render(
      <ScientificWorkflowBoard
        health={{ healthy: null, error: null, lastChecked: null }}
        runspaceEvents={[]}
        streamState={{ source: 'renderer-stream-state', connected: false, loading: true, error: null, partial: false }}
        selectedExecution={{ source: 'local-selection', executionId: null }}
        durableLifecycle={{ source: 'local-event-derived', value: null }}
        onShowEvidence={vi.fn()}
      />
    );

    expect(screen.getByText('No board-originated scientific operation is available.')).toBeInTheDocument();
  });

  it('renders six explicitly unavailable scientific views when no native view record is supplied', () => {
    render(
      <ScientificWorkflowBoard
        health={{ healthy: null, error: null, lastChecked: null }}
        runspaceEvents={[]}
        streamState={{ source: 'renderer-stream-state', connected: false, loading: true, error: null, partial: false }}
        selectedExecution={{ source: 'local-selection', executionId: null }}
        durableLifecycle={{ source: 'local-event-derived', value: null }}
        onShowEvidence={vi.fn()}
      />
    );

    for (const label of ['Setup view', 'Experiment view', 'Plan view', 'Run view', 'Results view', 'Samples and analysis view']) {
      const view = screen.queryByRole('region', { name: label });
      expect(view).not.toBeNull();
      expect(view).toHaveTextContent(/unavailable|no .* records were supplied/i);
    }
    expect(screen.queryByRole('button', { name: /confirm collection|review result|mark completed|approve/i })).toBeNull();
  });

  it('labels disconnected stream and local lifecycle as presentation context, never reconciliation', () => {
    render(
      <ScientificWorkflowBoard
        health={{ healthy: true, error: null, lastChecked: null }}
        runspaceEvents={[{ type: 'runspace-completed', runspaceId: 'run-1', timestamp: '2026-11-03', experimentId: 'execution-1' }]}
        streamState={{ source: 'renderer-stream-state', connected: false, loading: false, error: 'stream lost', partial: true }}
        selectedExecution={{ source: 'local-selection', executionId: 'execution-1' }}
        durableLifecycle={{ source: 'local-event-derived', value: 'Completed' }}
        onShowEvidence={vi.fn()}
      />
    );

    expect(screen.getByText(/Renderer stream: disconnected \(partial\)\. Error: stream lost/)).toHaveAttribute('data-source', 'renderer-stream-state');
    expect(screen.getByText(/Local execution selection: execution-1; selection is not an Agent receipt\./)).toHaveAttribute('data-source', 'local-selection');
    expect(screen.getByText(/Local event-derived lifecycle: Completed; not an Agent reconciliation disposition\./)).toHaveAttribute('data-source', 'local-event-derived');
    expect(screen.getByRole('status')).toHaveTextContent('Reconciliation evidence unavailable');
    expect(screen.queryByRole('button', { name: /execute|dispatch|approve|acknowledge|confirm/i })).toBeNull();
  });

  it('shows exact command-evidence identity without treating it as a scientific result', () => {
    const record = {
      evidenceId: 'evidence-exact-7', commandId: 'command-exact-7', executionId: 'execution-1', correlationId: 'correlation-7',
      sequence: 7, outcome: 'Held' as const, verifiedActor: 'Scientist', deviceId: 'simulator', capability: 'SetFlow',
      timestamp: '2026-11-03T12:00:00Z', parameters: '{"flow":2}', checks: [],
    };
    vi.mocked(useCommandEvidence).mockReturnValue({ ...commandEvidenceState, records: [record] });
    render(
      <ScientificWorkflowBoard
        health={{ healthy: true, error: null, lastChecked: null }}
        runspaceEvents={[]}
        streamState={{ source: 'renderer-stream-state', connected: true, loading: false, error: null, partial: false }}
        selectedExecution={{ source: 'local-selection', executionId: 'execution-1' }}
        durableLifecycle={{ source: 'local-event-derived', value: null }}
        onShowEvidence={vi.fn()}
      />
    );

    expect(screen.getByText('evidence-exact-7 — sequence 7; command command-exact-7; outcome Held.')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Run view' })).toHaveAttribute('data-source', 'agent-command-evidence');
    expect(screen.getByRole('region', { name: 'Results view' })).toHaveTextContent('Unavailable');
  });
});
