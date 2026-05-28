import { useState, useEffect } from "react";

export interface AgentHealthState {
  healthy: boolean | null;
  error: string | null;
  lastChecked: Date | null;
}

export function useAgentHealth(baseUrl: string, intervalMs = 5000): AgentHealthState {
  const [state, setState] = useState<AgentHealthState>({
    healthy: null,
    error: null,
    lastChecked: null,
  });

  useEffect(() => {
    const check = async () => {
      try {
        const res = await fetch(`${baseUrl}/health`, {
          signal: AbortSignal.timeout(3000),
        });
        setState({
          healthy: res.ok,
          error: res.ok ? null : `HTTP ${res.status}`,
          lastChecked: new Date(),
        });
      } catch (e) {
        setState({
          healthy: false,
          error: (e as Error).message,
          lastChecked: new Date(),
        });
      }
    };
    check();
    const id = setInterval(check, intervalMs);
    return () => clearInterval(id);
  }, [baseUrl, intervalMs]);

  return state;
}
