import { invoke } from '@tauri-apps/api/core';
import type { CapabilityDescriptor } from '../types/agent';

const ID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const MAX_CURSOR_PAGE_SIZE = 100;
const MAX_CURSOR_PAGES = 20;
const CANONICAL_CAPABILITIES = new Set([
  'SetFlow',
  'GetFlow',
  'SetTemperature',
  'GetTemperature',
  'SetMode',
  'GetMode',
]);

export type CheckStatus = 'Passed' | 'Failed' | 'Unknown';
export type CommandState = 'Allowed' | 'Rejected' | 'Held' | 'Dispatched' | 'Completed';
export type ExperimentControl = 'pause' | 'hold' | 'resume' | 'abort';

export interface CommandEvidence {
  evidenceId?: string;
  commandId: string;
  state: CommandState;
  sequence: number;
  occurredAtUtc?: string;
  checks: Array<{ check: string; status: CheckStatus; code: string }>;
}

export interface CommandEvidenceRecord {
  intent: {
    commandId: string;
    executionId: string;
    correlationId: string;
    verifiedActorDisplayName: string;
    capability: string;
    deviceId: string;
    canonicalParameters?: string;
  };
  evidence: CommandEvidence;
}

export interface AgentSessionStatus {
  authenticated: boolean;
  verifiedScientist: string | null;
  expiresAt: string | null;
  restored: boolean;
  restorationError: string | null;
}

export interface CommandProposal {
  commandId: string;
  executionId: string;
  correlationId: string;
  deviceId: string;
  capability: 'SetFlow' | 'GetFlow' | 'SetTemperature' | 'GetTemperature' | 'SetMode' | 'GetMode';
  canonicalParameters: string;
  policyRevision: string;
}

export interface ExperimentRecord {
  experimentId: string;
  name: string;
  lifecycle: string;
  activePhase: string | null;
  activeRunspaceId: string | null;
}

export interface MutationOutcome<T> {
  /** The caller-generated id is retained when the native acknowledgement is lost. */
  operationId: string;
  state: 'Completed' | 'Held';
  uncertain: boolean;
  result?: T;
  error?: string;
}

export interface ExecutionControlAcknowledgement {
  executionId: string;
  state: string;
  revision: number;
}

export interface EvidenceCursorPage {
  cursor: string | null;
  nextCursor: string | null;
  records: CommandEvidenceRecord[];
}

export interface EvidencePollingOptions {
  cursor?: string | null;
  executionId?: string | null;
  pageSize?: number;
  maxPages?: number;
  signal?: AbortSignal;
}

export interface AgentDeviceDiscovery {
  adapterId: string;
  capabilities: string[];
}

export interface AgentCapabilityDiscovery {
  adapterId: string;
  capabilities: CapabilityDescriptor[];
}

function assertIdentifier(value: string, fieldName: string): void {
  if (!ID_PATTERN.test(value)) {
    throw new Error(`${fieldName} must be a UUID.`);
  }
}

function assertBoundedText(value: string, fieldName: string, maximum: number): void {
  if (value.length === 0 || value.length > maximum) {
    throw new Error(`${fieldName} must be between 1 and ${maximum} characters.`);
  }
}

function parseNativeJson<T>(response: string, operation: string): T {
  try {
    return JSON.parse(response) as T;
  } catch {
    throw new Error(`The local Agent returned an invalid ${operation} response.`);
  }
}

function assertProposal(proposal: CommandProposal): void {
  assertIdentifier(proposal.commandId, 'commandId');
  assertIdentifier(proposal.executionId, 'executionId');
  assertIdentifier(proposal.correlationId, 'correlationId');
  assertBoundedText(proposal.deviceId, 'deviceId', 128);
  assertBoundedText(proposal.policyRevision, 'policyRevision', 128);

  if (!CANONICAL_CAPABILITIES.has(proposal.capability)) {
    throw new Error('proposal capability is not a canonical Agent capability.');
  }

  if (proposal.canonicalParameters.length > 4096) {
    throw new Error('canonicalParameters exceeds the local Agent request limit.');
  }

  try {
    const parsed = JSON.parse(proposal.canonicalParameters) as unknown;
    if (parsed === null || Array.isArray(parsed) || typeof parsed !== 'object') {
      throw new Error();
    }
  } catch {
    throw new Error('canonicalParameters must be a JSON object.');
  }
}

function operationId(): string {
  return crypto.randomUUID();
}

async function nativeJson<T>(command: string, arguments_: Record<string, unknown>, operation: string): Promise<T> {
  return parseNativeJson<T>(await invoke<string>(command, arguments_), operation);
}

/** Opens or restores a session through the local native broker; no credential reaches the renderer. */
export async function bootstrapAgentSession(): Promise<AgentSessionStatus> {
  await invoke<void>('bootstrap_agent_session');
  return getAgentSessionStatus();
}

export async function getAgentSessionStatus(): Promise<AgentSessionStatus> {
  return nativeJson<AgentSessionStatus>('agent_session_status', {}, 'session status');
}

export async function logoutAgentSession(): Promise<void> {
  await invoke<void>('agent_logout');
}

export async function revokeAgentSession(): Promise<void> {
  await invoke<void>('agent_revoke_session');
}

/** Health is a read operation and may be explicitly requested again after a restored session. */
export async function getAgentStatus(): Promise<string> {
  return invoke<string>('agent_status');
}

/** Discovery is a session-bound native read; the renderer never selects an Agent target. */
export async function getAgentDevices(): Promise<AgentDeviceDiscovery[]> {
  return nativeJson<AgentDeviceDiscovery[]>('agent_devices', {}, 'device discovery');
}

/** Capability metadata comes from the Agent and is not an authorization decision. */
export async function getAgentDeviceCapabilities(deviceId: string): Promise<AgentCapabilityDiscovery> {
  assertBoundedText(deviceId, 'deviceId', 128);
  return nativeJson<AgentCapabilityDiscovery>(
    'agent_device_capabilities',
    { deviceId },
    'device capability discovery',
  );
}

export async function proposeAgentCommand(proposal: CommandProposal): Promise<CommandEvidence> {
  assertProposal(proposal);
  return nativeJson<CommandEvidence>(
    'agent_propose',
    { proposal: JSON.stringify(proposal) },
    'command proposal',
  );
}

export async function approveAgentPolicy(commandId: string): Promise<void> {
  assertIdentifier(commandId, 'commandId');
  await invoke<string>('agent_approve_policy', {
    commandId,
    approval: JSON.stringify({ policyRevision: 'simulation-policy-v1' }),
  });
}

export async function dispatchAgentCommand(commandId: string): Promise<CommandEvidence> {
  assertIdentifier(commandId, 'commandId');
  return nativeJson<CommandEvidence>('agent_dispatch', { commandId }, 'command dispatch');
}

export async function readAgentEvidence(commandId: string): Promise<CommandEvidenceRecord> {
  assertIdentifier(commandId, 'commandId');
  return nativeJson<CommandEvidenceRecord>('agent_evidence', { commandId }, 'command evidence');
}

/** A safe status lookup for a stable command id; it never re-dispatches the command. */
export async function getAgentOperationStatus(operationId_: string): Promise<CommandEvidenceRecord> {
  assertIdentifier(operationId_, 'operationId');
  return nativeJson<CommandEvidenceRecord>(
    'agent_operation_status',
    { operationId: operationId_ },
    'operation status',
  );
}

export async function getExperimentStatus(experimentId: string): Promise<ExperimentRecord> {
  assertIdentifier(experimentId, 'experimentId');
  return nativeJson<ExperimentRecord>(
    'agent_experiment_status',
    { experimentId },
    'experiment status',
  );
}

/**
 * Mutations deliberately return Held when the broker cannot prove an acknowledgement.
 * Callers may look up operationId, but must never repeat the mutation automatically.
 */
async function mutation<T>(
  operationId_: string,
  request: () => Promise<T>,
): Promise<MutationOutcome<T>> {
  try {
    return { operationId: operationId_, state: 'Completed', uncertain: false, result: await request() };
  } catch (error) {
    return {
      operationId: operationId_,
      state: 'Held',
      uncertain: true,
      error: error instanceof Error ? error.message : 'The local Agent acknowledgement was unavailable.',
    };
  }
}

export function startAgentExperiment(name = 'Experiment', stableOperationId = operationId()): Promise<MutationOutcome<ExperimentRecord>> {
  assertIdentifier(stableOperationId, 'operationId');
  assertBoundedText(name, 'name', 256);
  return mutation(stableOperationId, () => nativeJson<ExperimentRecord>(
    'agent_experiment_start',
    { request: JSON.stringify({ name, operationId: stableOperationId }) },
    'experiment start',
  ));
}

export function controlAgentExperiment(
  experimentId: string,
  control: ExperimentControl,
  stableOperationId = operationId(),
): Promise<MutationOutcome<ExecutionControlAcknowledgement>> {
  assertIdentifier(experimentId, 'experimentId');
  assertIdentifier(stableOperationId, 'operationId');
  if (!['pause', 'hold', 'resume', 'abort'].includes(control)) {
    throw new Error('Experiment control is invalid.');
  }

  return mutation(stableOperationId, () => nativeJson<ExecutionControlAcknowledgement>('agent_experiment_control', {
      experimentId,
      control,
      operationId: stableOperationId,
    }, 'execution control'));
}

/**
 * Reads committed evidence pages through the native broker. It has no streaming protocol and
 * terminates after bounded pages, at the committed cursor, or when the caller cancels it.
 */
export async function* pollCommittedEvidence(options: EvidencePollingOptions = {}): AsyncGenerator<EvidenceCursorPage> {
  const pageSize = options.pageSize ?? 50;
  const maxPages = options.maxPages ?? MAX_CURSOR_PAGES;
  if (!Number.isInteger(pageSize) || pageSize < 1 || pageSize > MAX_CURSOR_PAGE_SIZE) {
    throw new Error(`pageSize must be between 1 and ${MAX_CURSOR_PAGE_SIZE}.`);
  }
  if (!Number.isInteger(maxPages) || maxPages < 1 || maxPages > MAX_CURSOR_PAGES) {
    throw new Error(`maxPages must be between 1 and ${MAX_CURSOR_PAGES}.`);
  }

  let cursor = options.cursor ?? null;
  for (let pageNumber = 0; pageNumber < maxPages && !options.signal?.aborted; pageNumber += 1) {
    const page = await nativeJson<EvidenceCursorPage>(
      'agent_evidence_cursor',
      options.executionId ? { cursor, pageSize, executionId: options.executionId } : { cursor, pageSize },
      'evidence cursor',
    );
    if (options.signal?.aborted) return;
    yield page;
    if (page.nextCursor === null || page.nextCursor === cursor) return;
    cursor = page.nextCursor;
  }
}

export interface ReviewNote {
  noteId: string;
  operationId: string;
  commandId: string;
  verifiedActorDisplayName: string;
  text: string;
  occurredAtUtc: string;
}

/** Appends context to a fact; acknowledgement loss must be resolved by operation-id lookup, never a retry. */
export async function appendCommandReviewNote(commandId: string, text: string, stableOperationId = operationId()): Promise<ReviewNote> {
  assertIdentifier(commandId, 'commandId');
  assertIdentifier(stableOperationId, 'operationId');
  assertBoundedText(text.trim(), 'review note', 2000);
  return nativeJson<ReviewNote>(
    'agent_append_review_note',
    { commandId, note: JSON.stringify({ operationId: stableOperationId, text: text.trim() }) },
    'review note',
  );
}

export async function executeApprovedSetFlow(flowRate: number): Promise<CommandEvidenceRecord> {
  if (!Number.isFinite(flowRate) || flowRate < 0 || flowRate > 10) {
    throw new Error('flowRate must be a finite value between 0 and 10 mL/min.');
  }

  const commandId = operationId();
  const executionId = operationId();
  const correlationId = operationId();
  const outcome = await mutation(commandId, async () => {
    await proposeAgentCommand({
      commandId,
      executionId,
      correlationId,
      deviceId: 'simulator',
      capability: 'SetFlow',
      canonicalParameters: JSON.stringify({ flowRate }),
      policyRevision: 'simulation-policy-v1',
    });
    await approveAgentPolicy(commandId);
    await dispatchAgentCommand(commandId);
    return readAgentEvidence(commandId);
  });
  if (outcome.result) return outcome.result;

  // Preserve the prior caller contract while making the unknown acknowledgement explicit.
  // The command id is the stable operation id for later authoritative lookup.
  return {
    intent: {
      commandId,
      executionId,
      correlationId,
      verifiedActorDisplayName: '',
      capability: 'SetFlow',
      deviceId: 'simulator',
    },
    evidence: {
      commandId,
      state: 'Held',
      sequence: 0,
      checks: [{ check: 'Acknowledgement', status: 'Unknown', code: 'ACK_UNKNOWN' }],
    },
  };
}

export interface AdmissionCheck {
  check: string;
  status: CheckStatus;
  code: string;
  reason?: string;
}

export interface SimulationPolicy {
  revision: string;
  approved: boolean;
  limits: {
    flowRate: string;
    temperature: string;
    mode: string;
    maximumMutations: number;
    wallTimeMinutes: number;
    leaseSeconds: number;
  };
}

export interface WorkflowSubmission {
  operationId: string;
  executionId: string;
  correlationId: string;
  originalWsl: unknown;
  definitionReference: string;
  policy: SimulationPolicy;
}

export interface RuntimeAdmission {
  state: "Dispatched" | "Rejected" | "Held";
  reason?: string;
  checks: AdmissionCheck[];
}

export interface WorkflowSubmissionResult {
  executionId: string;
  canonicalDefinitionReference?: string;
  canonicalDefinitionHash?: string;
  compileDiagnostics?: AdmissionCheck[];
  runtimeAdmission?: RuntimeAdmission;
}

/** Submits the original nonempty WSL and a scientist-approved, frozen simulation policy via the native broker. */
export async function submitSimulationWorkflow(request: WorkflowSubmission): Promise<WorkflowSubmissionResult> {
  assertIdentifier(request.operationId, "operationId");
  assertIdentifier(request.executionId, "executionId");
  assertIdentifier(request.correlationId, "correlationId");
  assertBoundedText(request.definitionReference, "definitionReference", 256);
  if (!request.policy.approved) throw new Error("The simulation policy must be approved before dispatch.");
  return nativeJson<WorkflowSubmissionResult>(
    "agent_submit_simulation",
    { request: JSON.stringify(request) },
    "simulation submission",
  );
}
