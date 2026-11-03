import { describe, expect, it } from 'vitest';
import type { AgentHealthState } from './useAgentHealth';
import { resolveBoardInteractionDisposition } from './useBoardInteractionPolicy';

const healthy = (overrides: Partial<AgentHealthState> = {}): AgentHealthState => ({
  healthy: true,
  error: null,
  lastChecked: new Date('2026-11-03T12:00:00Z'),
  ...overrides,
});

describe('board interaction disposition', () => {
  it('keeps a positive health-only observation read-only until reconciliation evidence exists', () => {
    const disposition = resolveBoardInteractionDisposition(healthy());
    expect(
      disposition.cause === 'reconciliation-unavailable' &&
      disposition.canEdit === false &&
      disposition.evidenceDestination === 'experiment-timeline'
    ).toBe(true);
  });

  it.each([
    ['absent health', undefined, 'health-missing'],
    ['pending health', healthy({ healthy: null, lastChecked: null }), 'health-pending'],
    ['unhealthy Agent', healthy({ healthy: false }), 'health-unhealthy'],
    ['health read error', healthy({ error: 'health read failed' }), 'health-error'],
    ['malformed health', { healthy: 'yes', error: null, lastChecked: null }, 'health-malformed'],
  ] as const)('%s remains read-only', (_name, health, cause) => {
    const disposition = resolveBoardInteractionDisposition(health as AgentHealthState | undefined);
    expect(
      disposition.cause === cause &&
      disposition.canEdit === false &&
      disposition.evidenceDestination === 'experiment-timeline'
    ).toBe(true);
  });
});
