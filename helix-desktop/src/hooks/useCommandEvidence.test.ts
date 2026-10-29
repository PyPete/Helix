import { renderHook, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const { pollCommittedEvidence } = vi.hoisted(() => ({ pollCommittedEvidence: vi.fn() }));
vi.mock('../lib/agentClient', () => ({ pollCommittedEvidence }));

import { useCommandEvidence } from './useCommandEvidence';

const record = (id: string, sequence: number) => ({
  intent: { commandId: id, executionId: '11111111-1111-4111-8111-111111111111', correlationId: id, verifiedActorDisplayName: 'Scientist', capability: 'SetFlow', deviceId: 'simulator', canonicalParameters: '{}' },
  evidence: { evidenceId: `${id}-evidence`, commandId: id, sequence, state: 'Allowed', occurredAtUtc: '2026-01-01T00:00:00Z', checks: [] },
});

describe('useCommandEvidence', () => {
  it('merges duplicate, out-of-order cursor facts by stable identity and committed sequence', async () => {
    pollCommittedEvidence.mockImplementation(async function* () {
      yield { cursor: null, nextCursor: 'next', records: [record('second', 2), record('first', 1)] };
      yield { cursor: 'next', nextCursor: null, records: [record('second', 2)] };
    });
    const { result } = renderHook(() => useCommandEvidence('11111111-1111-4111-8111-111111111111'));
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.records.map((item) => item.commandId)).toEqual(['first', 'second']);
  });

  it('retains partial rows and labels an unavailable authoritative history', async () => {
    pollCommittedEvidence.mockImplementation(async function* () {
      yield { cursor: null, nextCursor: 'next', records: [record('first', 1)] };
      throw new Error('SQL unavailable');
    });
    const { result } = renderHook(() => useCommandEvidence('11111111-1111-4111-8111-111111111111'));
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.records).toHaveLength(1);
    expect(result.current.unavailable).toBe(true);
    expect(result.current.error).toContain('SQL unavailable');
  });
});
