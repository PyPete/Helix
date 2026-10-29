import { useCallback, useEffect, useRef, useState } from 'react';
import { pollCommittedEvidence, type CommandEvidenceRecord } from '../lib/agentClient';
import type { CommandEvidenceHistoryRecord, CommandEvidenceHistoryState } from '../types/agent';

function toHistoryRecord(record: CommandEvidenceRecord): CommandEvidenceHistoryRecord {
  return {
    evidenceId: record.evidence.evidenceId ?? `${record.intent.commandId}:${record.evidence.sequence}`,
    commandId: record.intent.commandId,
    executionId: record.intent.executionId,
    correlationId: record.intent.correlationId || null,
    sequence: record.evidence.sequence,
    outcome: record.evidence.state,
    verifiedActor: record.intent.verifiedActorDisplayName || null,
    deviceId: record.intent.deviceId || null,
    capability: record.intent.capability || null,
    timestamp: record.evidence.occurredAtUtc ?? '',
    parameters: record.intent.canonicalParameters ?? null,
    checks: record.evidence.checks,
  };
}

function mergeRecords(current: CommandEvidenceHistoryRecord[], incoming: CommandEvidenceHistoryRecord[]): CommandEvidenceHistoryRecord[] {
  const unique = new Map(current.map((record) => [record.evidenceId, record]));
  for (const record of incoming) unique.set(record.evidenceId, record);
  return [...unique.values()].sort((left, right) =>
    left.sequence - right.sequence || left.evidenceId.localeCompare(right.evidenceId));
}

/** Reads authoritative cursor pages. Notifications should call refresh; they never become timeline facts. */
export function useCommandEvidence(executionId: string | null): CommandEvidenceHistoryState {
  const [records, setRecords] = useState<CommandEvidenceHistoryRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [unavailable, setUnavailable] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const requestVersion = useRef(0);

  const refresh = useCallback(async () => {
    const version = requestVersion.current + 1;
    requestVersion.current = version;
    if (!executionId) {
      setRecords([]);
      setLoading(false);
      setUnavailable(false);
      setError(null);
      return;
    }

    setLoading(true);
    setUnavailable(false);
    setError(null);
    const controller = new AbortController();
    try {
      for await (const page of pollCommittedEvidence({ executionId, signal: controller.signal })) {
        if (version !== requestVersion.current) return;
        setRecords((existing) => mergeRecords(existing, page.records.map(toHistoryRecord)));
      }
    } catch (reason) {
      if (version === requestVersion.current) {
        setUnavailable(true);
        setError(reason instanceof Error ? reason.message : 'Committed evidence history is unavailable.');
      }
    } finally {
      if (version === requestVersion.current) setLoading(false);
      controller.abort();
    }
  }, [executionId]);

  useEffect(() => {
    void refresh();
    return () => { requestVersion.current += 1; };
  }, [refresh]);

  return { records, loading, unavailable, error, refresh };
}
