import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useCommandEvidence } from '../../hooks/useCommandEvidence';
import { scientificWorkflowProposalStore } from '../../lib/scientificWorkflowProposalStore';
import type { CommandEvidenceHistoryState } from '../../types/agent';
import { ScientificWorkflowBoard } from './ScientificWorkflowBoard';

vi.mock('../../hooks/useCommandEvidence', () => ({ useCommandEvidence: vi.fn() }));

const evidenceState: CommandEvidenceHistoryState = { records: [], loading: false, unavailable: false, error: null, refresh: vi.fn() };

function renderBoard(health: { healthy: boolean | null; error: string | null; lastChecked: Date | null } = { healthy: null, error: null, lastChecked: null }) {
  return render(
    <ScientificWorkflowBoard
      health={health}
      runspaceEvents={[]}
      streamState={{ source: 'renderer-stream-state', connected: false, loading: false, error: null, partial: false }}
      selectedExecution={{ source: 'local-selection', executionId: null }}
      durableLifecycle={{ source: 'local-event-derived', value: null }}
      onShowEvidence={vi.fn()}
    />
  );
}

beforeEach(() => {
  vi.mocked(useCommandEvidence).mockReturnValue(evidenceState);
  vi.spyOn(scientificWorkflowProposalStore, 'listProposals').mockResolvedValue({ proposals: [], issues: [] });
});

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

describe('ScientificWorkflowBoard accessibility and proposal-only interaction', () => {
  it('uses keyboard shortcuts for explicit local creation, save, and typed context resolution', async () => {
    const user = userEvent.setup();
    const save = vi.spyOn(scientificWorkflowProposalStore, 'saveProposal').mockImplementation(async (submitted) => ({
      proposalId: submitted.proposalId,
      schemaVersion: submitted.schemaVersion,
      contentRevision: submitted.contentRevision,
      content: submitted.content,
      document: submitted,
      idempotent: false,
    }));
    renderBoard();
    await screen.findByText('Capability declarations are unavailable from the Agent.');
    const board = screen.getByTestId('scientific-workflow-board');

    fireEvent.keyDown(board, { key: 'N', ctrlKey: true, shiftKey: true });
    expect(screen.getByRole('textbox', { name: 'Proposal title' })).toBeInTheDocument();
    await user.type(screen.getByRole('textbox', { name: 'Proposal title' }), 'Keyboard draft');
    fireEvent.keyDown(board, { key: 's', ctrlKey: true });
    await waitFor(() => expect(save).toHaveBeenCalledOnce());
    expect(screen.queryByText(/Unsaved proposal changes are held in memory/)).toBeNull();

    fireEvent.click(screen.getByRole('button', { name: 'Edit Proposal' }));
    await user.type(screen.getByRole('textbox', { name: 'Proposal title' }), ' changed');
    await user.type(screen.getByRole('textbox', { name: 'Context value' }), 'procedure-target');
    await user.selectOptions(screen.getByRole('combobox', { name: 'Context type' }), 'procedure');
    fireEvent.keyDown(board, { key: 'ArrowRight', altKey: true, shiftKey: true });

    expect(screen.getByRole('dialog')).toHaveTextContent('procedure: procedure-target');
    expect(save).toHaveBeenCalledOnce();
    const dialog = screen.getByRole('dialog');
    await waitFor(() => expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Save proposal and switch' })));
    fireEvent.keyDown(dialog, { key: 'Tab', shiftKey: true });
    expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Keep editing' }));
    fireEvent.keyDown(dialog, { key: 'Tab' });
    expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Save proposal and switch' }));
    await user.click(screen.getByRole('button', { name: 'Keep editing' }));
    await waitFor(() => expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Request context change' })));
    expect(screen.getByLabelText('Current typed context')).toHaveTextContent('revision current');
  });

  it('keeps the exact accessible loss disclosure after a Save failure and performs no close-time write', async () => {
    const save = vi.spyOn(scientificWorkflowProposalStore, 'saveProposal').mockRejectedValue(new Error('read-back unavailable'));
    const { unmount } = renderBoard();
    await screen.findByText('Capability declarations are unavailable from the Agent.');
    fireEvent.click(screen.getByRole('button', { name: 'Create Proposal' }));
    fireEvent.change(screen.getByRole('textbox', { name: 'Proposal title' }), { target: { value: 'Unsaved local content' } });
    const disclosure = 'Unsaved proposal changes are held in memory and may be lost if Helix closes. Save to keep this proposal across restarts.';
    expect(screen.getByText(disclosure)).toHaveAttribute('role', 'status');
    expect(screen.getByText(disclosure)).toHaveAttribute('aria-live', 'polite');

    fireEvent.click(screen.getByRole('button', { name: 'Save Proposal' }));
    await screen.findByRole('alert');
    expect(screen.getByText(disclosure)).toBeInTheDocument();
    expect(save).toHaveBeenCalledOnce();
    unmount();
    expect(save).toHaveBeenCalledOnce();
  });

  it('routes editable nodes and edges into proposal content while source evidence remains display-only', async () => {
    const user = userEvent.setup();
    const save = vi.spyOn(scientificWorkflowProposalStore, 'saveProposal');
    renderBoard({ healthy: true, error: null, lastChecked: null });
    await screen.findByText('Capability declarations are unavailable from the Agent.');
    expect(screen.getByRole('button', { name: 'Add proposal node' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Create Proposal' }));

    await user.click(screen.getByRole('button', { name: 'Add proposal node' }));
    await user.click(screen.getByRole('button', { name: 'Add proposal node' }));
    await user.clear(screen.getAllByRole('textbox', { name: 'step node label' })[0]);
    await user.type(screen.getAllByRole('textbox', { name: 'step node label' })[0], 'Revised local step');
    await user.click(screen.getByRole('button', { name: 'Add proposal edge' }));

    expect(screen.getAllByRole('textbox', { name: 'step node label' })[0]).toHaveValue('Revised local step');
    expect(screen.getByRole('list', { name: 'Proposal edges' })).toHaveTextContent('Local proposal edge');
    expect(screen.getByText('Condition evidence: unavailable — no Agent-authoritative condition evidence was supplied.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /execute|dispatch|approve|validate|publish/i })).toBeNull();
    expect(save).not.toHaveBeenCalled();
    expect(useCommandEvidence).toHaveBeenCalled();
  });
});
