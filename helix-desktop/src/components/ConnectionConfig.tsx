import { useCallback, useEffect, useState } from "react";
import {
  bootstrapAgentSession,
  getAgentSessionStatus,
  type AgentSessionStatus,
} from "../lib/agentClient";
import type { AgentConnection } from "../types/agent";

interface ConnectionConfigProps {
  /** Retained for callers that still display an old setting; it cannot select the native target. */
  connection: AgentConnection;
  onSave: (connection: AgentConnection) => void;
}

const FIXED_LOCAL_CONNECTION: AgentConnection = {
  host: "127.0.0.1",
  port: 5080,
  protocol: "http",
};

export default function ConnectionConfig({ connection, onSave }: ConnectionConfigProps) {
  const [session, setSession] = useState<AgentSessionStatus | null>(null);
  const [sessionError, setSessionError] = useState<string | null>(null);
  const legacyDestination =
    connection.host !== FIXED_LOCAL_CONNECTION.host ||
    connection.port !== FIXED_LOCAL_CONNECTION.port ||
    connection.protocol !== FIXED_LOCAL_CONNECTION.protocol;

  useEffect(() => {
    let cancelled = false;
    void getAgentSessionStatus()
      .then((status) => {
        if (!cancelled) setSession(status);
      })
      .catch((reason: unknown) => {
        if (!cancelled) {
          setSessionError(reason instanceof Error ? reason.message : "Unable to inspect the local Agent session.");
        }
      });
    return () => { cancelled = true; };
  }, []);

  const restoreLocalSession = useCallback(async () => {
    setSessionError(null);
    try {
      const status = await bootstrapAgentSession();
      setSession(status);
      onSave(FIXED_LOCAL_CONNECTION);
    } catch (reason) {
      setSessionError(reason instanceof Error ? reason.message : "Unable to restore the local Agent session.");
    }
  }, [onSave]);

  return (
    <section aria-label="Agent Connection">
      <h2>Local Helix Agent</h2>
      <p>Native broker target: 127.0.0.1:5080</p>
      <p>Session identity is restored by the native broker. Credentials and remote destinations are never stored in this application.</p>
      {legacyDestination && (
        <p role="alert">
          A legacy remote connection setting was ignored. Helix now uses the fixed local Agent only.
        </p>
      )}
      {session === null && !sessionError && <p aria-live="polite">Loading local session status…</p>}
      {session?.authenticated && (
        <div aria-label="Verified local session">
          <p style={{ overflowWrap: "anywhere" }}>
            Verified scientist: {session.verifiedScientist ?? "local Windows session"}
          </p>
          <p>Session expiry: {session.expiresAt ?? "not reported by the local Agent"}</p>
        </div>
      )}
      {session && !session.authenticated && (
        <p role="status">No local Agent session is active. Restore the session before proposing commands.</p>
      )}
      {(sessionError ?? session?.restorationError) && (
        <p role="alert">{sessionError ?? session?.restorationError}</p>
      )}
      <button type="button" onClick={() => void restoreLocalSession()}>
        Restore local Agent session
      </button>
    </section>
  );
}
