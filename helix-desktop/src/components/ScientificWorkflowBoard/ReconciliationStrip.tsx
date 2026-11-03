import type { BoardInteractionDisposition } from '../../hooks/useBoardInteractionPolicy';

export interface ReconciliationStripProps {
  disposition: BoardInteractionDisposition;
  onShowEvidence: () => void;
}

export function ReconciliationStrip({ disposition, onShowEvidence }: ReconciliationStripProps) {
  return (
    <aside
      className="scientific-workflow-board__reconciliation"
      role="status"
      aria-label="Reconciliation status"
      aria-live="polite"
      aria-atomic="true"
      data-testid="reconciliation-strip"
    >
      <div className="scientific-workflow-board__reconciliation-copy">
        <p className="scientific-workflow-board__eyebrow">Proposal-only · read-only</p>
        <h2>{disposition.label}</h2>
        <p>{disposition.message}</p>
      </div>
      <button type="button" onClick={onShowEvidence}>
        Review Agent evidence
      </button>
    </aside>
  );
}
