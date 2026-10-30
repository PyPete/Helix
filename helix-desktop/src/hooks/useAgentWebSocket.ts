import { useEffect, useRef, useState } from "react";
import { pollCommittedEvidence, type CommandEvidenceRecord } from "../lib/agentClient";
import type { DeviceStatusEvent, ExperimentEvent, RunspaceEvent, SafetyEvent, TrendValue } from "../types/agent";

export interface AgentWebSocketState {
  runspaceEvents: RunspaceEvent[];
  trendValues: TrendValue[];
  safetyEvents: SafetyEvent[];
  deviceStatusEvents: DeviceStatusEvent[];
  experimentEvents: ExperimentEvent[];
  activeExperimentId: string | null;
  connected: boolean;
  loading: boolean;
  error: string | null;
  partial: boolean;
}

const POLL_INTERVAL_MS = 2_000;

function evidenceIdentity(record: CommandEvidenceRecord): string {
  return `${record.evidence.commandId}:${record.evidence.sequence}`;
}

function asRunspaceEvent(record: CommandEvidenceRecord): RunspaceEvent {
  return {
    type: "command-evidence",
    runspaceId: record.intent.commandId,
    timestamp: String(record.evidence.sequence),
    commandId: record.intent.commandId,
    executionId: record.intent.correlationId,
    state: record.evidence.state,
    checks: record.evidence.checks,
  };
}

/**
 * Retains the display contract of the former WebSocket hook while receiving only committed,
 * session-authenticated facts. Cursors survive reconnects and identities prevent replayed facts.
 */
export function useAgentWebSocket(_baseUrl: string): AgentWebSocketState {
  const [runspaceEvents, setRunspaceEvents] = useState<RunspaceEvent[]>([]);
  const [connected, setConnected] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [partial, setPartial] = useState(false);
  const cursorRef = useRef<string | null>(null);
  const seenRef = useRef(new Set<string>());
  const requestRef = useRef(0);

  useEffect(() => {
    let disposed = false;
    let controller: AbortController | null = null;

    const read = async () => {
      const request = ++requestRef.current;
      controller?.abort();
      const currentController = new AbortController();
      controller = currentController;
      setLoading(true);
      try {
        let lastCursor = cursorRef.current;
        const received: RunspaceEvent[] = [];
        for await (const page of pollCommittedEvidence({
          cursor: cursorRef.current,
          signal: currentController.signal,
        })) {
          if (disposed || currentController.signal.aborted || request !== requestRef.current) return;
          lastCursor = page.nextCursor ?? page.cursor;
          for (const record of page.records) {
            const identity = evidenceIdentity(record);
            if (!seenRef.current.has(identity)) {
              seenRef.current.add(identity);
              received.push(asRunspaceEvent(record));
            }
          }
          if (page.nextCursor === null) break;
        }
        if (!disposed && request === requestRef.current) {
          cursorRef.current = lastCursor;
          if (received.length > 0) setRunspaceEvents((previous) => [...previous, ...received].slice(-100));
          setConnected(true);
          setError(null);
          setPartial(false);
        }
      } catch (reason) {
        if (!disposed && !currentController.signal.aborted && request === requestRef.current) {
          setConnected(false);
          setPartial(runspaceEvents.length > 0);
          setError(reason instanceof Error ? reason.message : "Local Agent events are unavailable.");
        }
      } finally {
        if (!disposed && request === requestRef.current) setLoading(false);
      }
    };

    void read();
    const interval = window.setInterval(() => { void read(); }, POLL_INTERVAL_MS);
    return () => {
      disposed = true;
      controller?.abort();
      window.clearInterval(interval);
    };
  }, []);

  return {
    runspaceEvents,
    trendValues: [],
    safetyEvents: [],
    deviceStatusEvents: [],
    experimentEvents: [],
    activeExperimentId: null,
    connected,
    loading,
    error,
    partial,
  };
}
