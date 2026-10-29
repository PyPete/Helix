import { invoke } from '@tauri-apps/api/core';

export interface CommandEvidence {
  commandId: string;
  state: string;
  sequence: number;
  checks: Array<{ check: string; status: string; code: string }>;
}

export interface CommandEvidenceRecord {
  intent: { commandId: string; correlationId: string; verifiedActorDisplayName: string; capability: string; deviceId: string };
  evidence: CommandEvidence;
}

export async function bootstrapAgentSession(): Promise<void> {
  await invoke('bootstrap_agent_session');
}

export async function getAgentStatus(): Promise<string> {
  return invoke<string>('agent_status');
}

export async function proposeAgentCommand(proposal: object): Promise<CommandEvidence> {
  return JSON.parse(await invoke<string>('agent_propose', { proposal: JSON.stringify(proposal) })) as CommandEvidence;
}

export async function approveAgentPolicy(commandId: string): Promise<void> {
  await invoke<string>('agent_approve_policy', {
    commandId,
    approval: JSON.stringify({ policyRevision: 'simulation-policy-v1' }),
  });
}

export async function dispatchAgentCommand(commandId: string): Promise<CommandEvidence> {
  return JSON.parse(await invoke<string>('agent_dispatch', { commandId })) as CommandEvidence;
}

export async function readAgentEvidence(commandId: string): Promise<CommandEvidenceRecord> {
  return JSON.parse(await invoke<string>('agent_evidence', { commandId })) as CommandEvidenceRecord;
}

export async function executeApprovedSetFlow(flowRate: number): Promise<CommandEvidenceRecord> {
  const commandId = crypto.randomUUID();
  const executionId = crypto.randomUUID();
  const correlationId = crypto.randomUUID();
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
}
