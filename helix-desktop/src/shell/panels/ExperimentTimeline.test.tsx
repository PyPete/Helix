import { fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ExperimentTimeline } from './ExperimentTimeline';

const appendNote = vi.fn().mockResolvedValue(undefined);
vi.mock('../../hooks/useCommandEvidence', () => ({ useCommandEvidence: () => ({ records: [], loading: false, unavailable: false, error: null, refresh: vi.fn() }) }));
vi.mock('../../lib/agentClient', () => ({ appendCommandReviewNote: (...args: unknown[]) => appendNote(...args) }));

describe('ExperimentTimeline', () => {
  it('uses the required empty evidence copy', () => {
    render(<ExperimentTimeline runspaceEvents={[]} experimentEvents={[]} activeExperimentId={null} />);
    expect(screen.getByRole('heading', { name: 'No command evidence yet' })).toBeInTheDocument();
    expect(screen.getByText(/Proposed simulated actions/)).toBeInTheDocument();
  });

  it('orders evidence by durable sequence and shows critical meaning without color', () => {
    render(<ExperimentTimeline runspaceEvents={[]} experimentEvents={[]} activeExperimentId={null} commandEvidence={[
      { evidenceId: 'second', commandId: '22222222-2222-4222-8222-222222222222', executionId: 'execution', sequence: 2, state: 'Allowed', verifiedActor: 'Scientist', capability: 'SetFlow', deviceId: 'simulator', timestamp: 'later', checks: [] },
      { evidenceId: 'first', commandId: '11111111-1111-4111-8111-111111111111', executionId: 'execution', sequence: 1, state: 'Rejected', verifiedActor: 'Scientist', capability: 'SetFlow', deviceId: 'simulator', timestamp: 'earlier', checks: [{ check: 'Safety', status: 'Failed', code: 'unsafe' }] },
    ]} />);
    const rows = screen.getAllByTestId('timeline-command-evidence');
    expect(rows[0]).toHaveTextContent('Rejected � not dispatched');
    expect(rows[0]).toHaveTextContent('?');
    expect(rows[1]).toHaveTextContent('Allowed');
  });

  it('expands original facts and appends a review note without editing them', async () => {
    render(<ExperimentTimeline runspaceEvents={[]} experimentEvents={[]} activeExperimentId={null} commandEvidence={[{
      evidenceId: 'evidence-1', commandId: '11111111-1111-4111-8111-111111111111', executionId: 'execution-1', sequence: 1, state: 'Held', verifiedActor: 'Scientist', capability: 'SetFlow', deviceId: 'simulator', timestamp: 'now', checks: [{ check: 'health', status: 'Unknown', code: 'health.unknown' }],
    }]} />);
    fireEvent.click(screen.getByRole('button', { name: 'Show original facts' }));
    expect(screen.getByText('Command ID')).toBeInTheDocument();
    const evidenceRow = screen.getByTestId('timeline-command-evidence');
    expect(within(evidenceRow).getByText(/health:\s*health\.unknown/)).toHaveTextContent('Unknown health: health.unknown');
    fireEvent.change(screen.getByLabelText('Add review note'), { target: { value: 'Observed fumehood condition' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add review note' }));
    expect(appendNote).toHaveBeenCalledWith('11111111-1111-4111-8111-111111111111', 'Observed fumehood condition');
  });
});
