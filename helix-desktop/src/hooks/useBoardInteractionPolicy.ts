import type { AgentHealthState } from './useAgentHealth';

export type BoardInteractionCause =
  | 'health-missing'
  | 'health-pending'
  | 'health-unhealthy'
  | 'health-error'
  | 'health-malformed'
  | 'reconciliation-unavailable';

export interface BoardInteractionDisposition {
  cause: BoardInteractionCause;
  canEdit: false;
  evidenceDestination: 'experiment-timeline';
  healthObservation: string;
  label: string;
  message: string;
}

function readOnlyDisposition(
  cause: BoardInteractionCause,
  label: string,
  message: string,
  healthObservation = label
): BoardInteractionDisposition {
  return { cause, canEdit: false, evidenceDestination: 'experiment-timeline', healthObservation, label, message };
}

/**
 * Maps only the native health observation to a truthful board disposition.
 * No Agent-authoritative reconciliation or scientific-record input exists here,
 * so even healthy status cannot grant local proposal mutation authority.
 */
export function resolveBoardInteractionDisposition(health?: AgentHealthState | null): BoardInteractionDisposition {
  if (health === undefined || health === null) {
    return readOnlyDisposition(
      'health-missing',
      'Agent health unavailable',
      'No native Agent health observation was supplied. The board remains read-only.'
    );
  }

  if (typeof health !== 'object') {
    return readOnlyDisposition(
      'health-malformed',
      'Agent health unavailable',
      'The native Agent health observation is malformed. The board remains read-only.'
    );
  }

  const candidate = health as AgentHealthState;
  const healthyIsValid = candidate.healthy === null || typeof candidate.healthy === 'boolean';
  const errorIsValid = candidate.error === null || typeof candidate.error === 'string';
  const lastCheckedIsValid = candidate.lastChecked === null ||
    (candidate.lastChecked instanceof Date && !Number.isNaN(candidate.lastChecked.getTime()));

  if (!healthyIsValid || !errorIsValid || !lastCheckedIsValid) {
    return readOnlyDisposition(
      'health-malformed',
      'Agent health unavailable',
      'The native Agent health observation is malformed. The board remains read-only.'
    );
  }

  if (candidate.error !== null) {
    return readOnlyDisposition(
      'health-error',
      'Agent health check failed',
      `Native Agent health could not be confirmed: ${candidate.error}. The board remains read-only.`,
      `Error — ${candidate.error}`
    );
  }

  if (candidate.healthy === null) {
    return readOnlyDisposition(
      'health-pending',
      'Agent health check pending',
      'Waiting for the authenticated native Agent health observation. The board remains read-only.'
    );
  }

  if (candidate.healthy === false) {
    return readOnlyDisposition(
      'health-unhealthy',
      'Agent unavailable',
      'The native Agent health observation is unavailable. The board remains read-only.'
    );
  }

  return readOnlyDisposition(
      'reconciliation-unavailable',
      'Reconciliation evidence unavailable',
    'Agent health is an eligibility observation only. Agent-authoritative lifecycle and scientific evidence are unavailable, so the board remains read-only.',
    'Healthy — eligibility observation only'
  );
}

export function useBoardInteractionPolicy(health?: AgentHealthState | null): BoardInteractionDisposition {
  return resolveBoardInteractionDisposition(health);
}
