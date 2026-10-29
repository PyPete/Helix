import { useEffect, useState } from "react";
import { bootstrapAgentSession, getAgentSessionStatus, logoutAgentSession, type AgentSessionStatus } from "../../lib/agentClient";
import "./AgentSession.css";

interface AgentSessionProps {
  onStatusChange?: (status: AgentSessionStatus | null) => void;
}

export function AgentSession({ onStatusChange }: AgentSessionProps) {
  const [session, setSession] = useState<AgentSessionStatus | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  async function restore() {
    setLoading(true); setError(null);
    try {
      const restored = await bootstrapAgentSession();
      setSession(restored);
      onStatusChange?.(restored);
    }
    catch {
      setSession(null);
      onStatusChange?.(null);
      setError("The local Agent could not verify your desktop session. No command was dispatched. Restore the local session, then try again.");
    }
    finally { setLoading(false); }
  }
  useEffect(() => {
    void getAgentSessionStatus()
      .then((status) => {
        setSession(status);
        onStatusChange?.(status);
      })
      .catch(() => void restore())
      .finally(() => setLoading(false));
  // Session status is loaded once; restoration is explicitly initiated by the scientist.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
  async function logout() {
    try {
      await logoutAgentSession();
      setSession(null);
      onStatusChange?.(null);
      setError("Local session signed out. Command dispatch is disabled.");
    } catch {
      setError("The local Agent could not sign out the desktop session. Command dispatch remains disabled until the status is restored.");
    }
  }
  if (loading) return <section className="agent-session" aria-label="Local Agent session"><p role="status">Local Agent � Checking session�</p></section>;
  return <section className="agent-session" aria-label="Local Agent session"><p><strong>Local Agent — {session?.authenticated ? "Connected" : "Unavailable"}</strong></p>{session?.authenticated && session.verifiedScientist ? <p>Session: <span className="agent-session__identity">{session.verifiedScientist}</span> (verified scientist)</p> : <p role="alert">{error ?? "The local Agent could not verify your desktop session. No command was dispatched. Restore the local session, then try again."}</p>}{session?.restorationError && <p role="alert">{session.restorationError}</p>}<button onClick={() => void restore()} disabled={loading}>Restore local session</button>{session?.authenticated && <button onClick={() => void logout()}>Sign out</button>}</section>;
}
