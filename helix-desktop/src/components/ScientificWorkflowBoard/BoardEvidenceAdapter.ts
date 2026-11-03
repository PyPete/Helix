import type { ScientificOperationResult } from '../../hooks/useScientificOperation';
import type { CommandEvidenceHistoryRecord, CommandEvidenceHistoryState } from '../../types/agent';
import type { ScientificReceipt } from '../../types/scientific';

export type OperationEvidenceView =
  | {
      source: 'originating-operation';
      status: 'unavailable';
      reason: 'no-originating-operation' | 'operation-not-committed' | 'receipt-unavailable' | 'receipt-mismatch' | 'malformed-operation';
    }
  | {
      source: 'agent-operation-receipt';
      status: 'committed';
      operationId: string;
      receipt: ScientificReceipt;
    };

export type CommandEvidenceView =
  | {
      source: 'agent-command-evidence';
      status: 'unavailable';
      reason: 'execution-id-unavailable' | 'history-unavailable';
      records: readonly CommandEvidenceHistoryRecord[];
      error: string | null;
    }
  | {
      source: 'agent-command-evidence';
      status: 'loading' | 'error' | 'empty' | 'available';
      records: readonly CommandEvidenceHistoryRecord[];
      error: string | null;
    };

function unavailableOperation(
  reason: Extract<OperationEvidenceView, { status: 'unavailable' }>['reason']
): OperationEvidenceView {
  return { source: 'originating-operation', status: 'unavailable', reason };
}

/** Only a receipt matching the exact caller-owned operation can be shown as committed evidence. */
export function adaptOperationEvidence(operation: ScientificOperationResult | null | undefined): OperationEvidenceView {
  if (!operation || typeof operation !== 'object') return unavailableOperation('no-originating-operation');
  if (!['idle', 'pending', 'uncertain', 'committed'].includes(operation.state)) {
    return unavailableOperation('malformed-operation');
  }
  if (operation.state !== 'committed') return unavailableOperation('operation-not-committed');

  const context = operation.context;
  if (!context || !['procedure', 'plan'].includes(context.kind) || !context.operationId.trim()) {
    return unavailableOperation('malformed-operation');
  }

  const receipt = operation.receipt;
  if (!receipt) return unavailableOperation('receipt-unavailable');
  if (
    receipt.operationId !== context.operationId ||
    receipt.kind !== context.kind ||
    receipt.status !== 'Committed' ||
    typeof receipt.resourceId !== 'string' ||
    !receipt.resourceId
  ) {
    return unavailableOperation('receipt-mismatch');
  }

  return {
    source: 'agent-operation-receipt',
    status: 'committed',
    operationId: context.operationId,
    receipt,
  };
}

/** Keeps hook records and partial status intact; local selection never becomes a receipt. */
export function adaptCommandEvidence(
  executionId: string | null,
  history: CommandEvidenceHistoryState | null
): CommandEvidenceView {
  if (typeof executionId !== 'string' || !executionId.trim()) {
    return {
      source: 'agent-command-evidence',
      status: 'unavailable',
      reason: 'execution-id-unavailable',
      records: [],
      error: null,
    };
  }

  if (!history || !Array.isArray(history.records)) {
    return {
      source: 'agent-command-evidence',
      status: 'unavailable',
      reason: 'history-unavailable',
      records: [],
      error: null,
    };
  }

  // The hook can briefly retain rows from the prior selection while the new cursor read starts.
  // Bind every rendered immutable fact to this exact selected ID rather than relabelling stale rows.
  const records = history.records.filter((record) => record.executionId === executionId);
  if (history.unavailable) {
    return { source: 'agent-command-evidence', status: 'unavailable', reason: 'history-unavailable', records, error: history.error };
  }
  if (history.error !== null) return { source: 'agent-command-evidence', status: 'error', records, error: history.error };
  if (history.loading) return { source: 'agent-command-evidence', status: 'loading', records, error: null };
  if (records.length === 0) return { source: 'agent-command-evidence', status: 'empty', records, error: null };
  return { source: 'agent-command-evidence', status: 'available', records, error: null };
}
