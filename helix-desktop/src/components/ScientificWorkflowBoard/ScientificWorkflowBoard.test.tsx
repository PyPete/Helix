import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useCommandEvidence } from '../../hooks/useCommandEvidence';
import { ScientificWorkflowBoard } from './ScientificWorkflowBoard';

vi.mock('../../hooks/useCommandEvidence', () => ({ useCommandEvidence: vi.fn() }));

const commandEvidenceState = { records: [], loading: false, unavailable: false, error: null, refresh: vi.fn() };

beforeEach(() => {
  vi.mocked(useCommandEvidence).mockReturnValue(commandEvidenceState);
});

describe('ScientificWorkflowBoard reconciliation status', () => {
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
});
