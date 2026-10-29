import { invoke } from '@tauri-apps/api/core';

export async function bootstrapAgentSession(): Promise<void> {
  await invoke('bootstrap_agent_session');
}

export async function getAgentStatus(): Promise<void> {
  await invoke('agent_status');
}
