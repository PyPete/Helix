import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ScientificWorkflowBoard } from './ScientificWorkflowBoard';

describe('ScientificWorkflowBoard reconciliation status', () => {
  it('keeps positive health read-only and offers only evidence navigation', () => {
    const onShowEvidence = vi.fn();
    render(
      <ScientificWorkflowBoard
        health={{ healthy: true, error: null, lastChecked: new Date('2026-11-03T12:00:00Z') }}
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
});
