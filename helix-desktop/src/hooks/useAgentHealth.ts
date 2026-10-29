import { useEffect, useState } from "react";
import { getAgentStatus } from "../lib/agentClient";

export interface AgentHealthState {
  healthy: boolean | null;
  error: string | null;
  lastChecked: Date | null;
}

/**
 * The URL parameter remains for compatibility with existing consumers. Health always travels
 * through the fixed, authenticated native broker rather than renderer networking.
 */
export function useAgentHealth(_baseUrl: string, intervalMs = 5000): AgentHealthState {
  const [state, setState] = useState<AgentHealthState>({
    healthy: null,
    error: null,
    lastChecked: null,
  });

  useEffect(() => {
    let disposed = false;
    const check = async () => {
      try {
        const status = await getAgentStatus();
        if (!disposed) {
          setState({
            healthy: status.trim().toLowerCase() === "healthy",
            error: status.trim().toLowerCase() === "healthy" ? null : "Local Agent is unavailable.",
            lastChecked: new Date(),
          });
        }
      } catch (error) {
        if (!disposed) {
          setState({
            healthy: false,
            error: error instanceof Error ? error.message : "Local Agent is unavailable.",
            lastChecked: new Date(),
          });
        }
      }
    };

    void check();
    const id = window.setInterval(() => { void check(); }, intervalMs);
    return () => {
      disposed = true;
      window.clearInterval(id);
    };
  }, [intervalMs]);

  return state;
}
