import { describe, expect, it, vi } from 'vitest';
import type { ScientificOperationResult } from '../../hooks/useScientificOperation';
import type { CommandEvidenceHistoryRecord, CommandEvidenceHistoryState } from '../../types/agent';
import { adaptCommandEvidence, adaptOperationEvidence } from './BoardEvidenceAdapter';

const operation = (overrides: Partial<ScientificOperationResult> = {}): ScientificOperationResult => ({
  state: 'idle',
  begin: vi.fn(),
  markUncertain: vi.fn(),
  reconcile: vi.fn(),
  ...overrides,
});

const commandRecord = (evidenceId: string, sequence: number): CommandEvidenceHistoryRecord => ({
  evidenceId,
  commandId: `command-${sequence}`,
  executionId: 'execution-1',
  correlationId: `correlation-${sequence}`,
  sequence,
  outcome: 'Allowed',
  verifiedActor: 'Scientist',
  deviceId: 'simulator',
  capability: 'SetFlow',
  timestamp: `2026-11-03T12:00:0${sequence}Z`,
  parameters: '{}',
  checks: [],
});

const history = (overrides: Partial<CommandEvidenceHistoryState> = {}): CommandEvidenceHistoryState => ({
  records: [],
  loading: false,
  unavailable: false,
  error: null,
  refresh: vi.fn(),
  ...overrides,
});

describe('adaptOperationEvidence', () => {
  it('does not create an operation outcome when the board did not originate one', () => {
    expect(adaptOperationEvidence(null)).toEqual({
      source: 'originating-operation',
      status: 'unavailable',
      reason: 'no-originating-operation',
    });
  });

  it.each(['idle', 'pending', 'uncertain'] as const)(
    'keeps a caller-local %s operation context from becoming a committed outcome',
    (state) => {
      const result = adaptOperationEvidence(operation({
        state,
        context: { kind: 'plan', operationId: 'original-operation', request: '{}' },
      }));
      expect(result.status).toBe('unavailable');
      expect(result.source).toBe('originating-operation');
    }
  );

  it('rejects malformed or restored local contexts as operation evidence', () => {
    const malformed = operation({
      state: 'committed',
      context: { kind: 'plan', operationId: '', request: '{}' },
      receipt: { operationId: 'other-operation', kind: 'plan', resourceId: 'plan-1', status: 'Committed' },
    });
    const restored = operation({ state: 'uncertain', context: { kind: 'plan', operationId: 'restored-op', request: '{}' } });

    expect(adaptOperationEvidence(malformed).status).toBe('unavailable');
    expect(adaptOperationEvidence(restored).status).toBe('unavailable');
  });

  it('passes through only the exact committed receipt for its originating operation ID', () => {
    const receipt = { operationId: 'original-operation', kind: 'plan' as const, resourceId: 'plan-1', status: 'Committed' as const };
    const result = adaptOperationEvidence(operation({
      state: 'committed',
      context: { kind: 'plan', operationId: 'original-operation', request: '{}' },
      receipt,
    }));

    expect(result).toEqual({
      source: 'agent-operation-receipt',
      status: 'committed',
      operationId: 'original-operation',
      receipt,
    });
    expect(result.status === 'committed' && result.receipt).toBe(receipt);
    expect(adaptOperationEvidence(operation({
      state: 'committed',
      context: { kind: 'plan', operationId: 'original-operation', request: '{}' },
    })).status).toBe('unavailable');
  });
});

describe('adaptCommandEvidence', () => {
  it('treats an absent execution ID as unavailable and ignores any stale hook rows', () => {
    const staleRecord = commandRecord('stale', 1);
    const adapted = adaptCommandEvidence(null, history({ records: [staleRecord] }));

    expect(adapted.status).toBe('unavailable');
    expect(adapted.reason).toBe('execution-id-unavailable');
    expect(adapted.records).toEqual([]);
  });

  it('retains loading state and ordered record identities without sorting or rewriting', () => {
    const records = [commandRecord('second', 2), commandRecord('first', 1)];
    const adapted = adaptCommandEvidence('execution-1', history({ records, loading: true }));

    expect(adapted.status).toBe('loading');
    expect(adapted.records.map((record) => record.evidenceId)).toEqual(['second', 'first']);
    expect(adapted.records[0]).toBe(records[0]);
    expect(adapted.records[1]).toBe(records[1]);
  });

  it('excludes retained cursor rows belonging to a previous selected execution', () => {
    const exact = commandRecord('exact', 1);
    const stale = { ...commandRecord('stale', 2), executionId: 'previous-execution' };
    const adapted = adaptCommandEvidence('execution-1', history({ records: [stale, exact] }));

    expect(adapted.status).toBe('available');
    expect(adapted.records).toEqual([exact]);
    expect(adapted.records[0]).toBe(exact);
  });

  it('preserves partial records and error details when authoritative history is unavailable', () => {
    const records = [commandRecord('partial', 3)];
    const adapted = adaptCommandEvidence('execution-1', history({ records, unavailable: true, error: 'cursor unavailable' }));

    expect(adapted.status).toBe('unavailable');
    expect(adapted.records[0]).toBe(records[0]);
    expect(adapted.error).toBe('cursor unavailable');
  });

  it('distinguishes an empty history, a read error, and available source records', () => {
    const record = commandRecord('exact-evidence-id', 4);

    expect(adaptCommandEvidence('execution-1', history()).status).toBe('empty');
    expect(adaptCommandEvidence('execution-1', history({ error: 'read failed' })).status).toBe('error');
    const available = adaptCommandEvidence('execution-1', history({ records: [record] }));
    expect(available.status).toBe('available');
    expect(available.records[0]).toBe(record);
  });
});
