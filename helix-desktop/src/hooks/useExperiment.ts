import { useCallback, useEffect, useRef, useState } from "react";
import {
  controlAgentExperiment,
  getExperimentStatus,
  startAgentExperiment,
  type ExperimentRecord,
  type MutationOutcome,
} from "../lib/agentClient";

export interface UseExperimentResult {
  experimentId: string | null;
  activePhase: string | null;
  isRunning: boolean;
  startExperiment: (name?: string) => Promise<string | null>;
  pauseExperiment: () => Promise<void>;
  resumeExperiment: () => Promise<void>;
  abortExperiment: () => Promise<void>;
  error: string | null;
}

function isRunningRecord(record: ExperimentRecord): boolean {
  return record.lifecycle.toLowerCase() === "running";
}

/**
 * baseUrl remains optional for source compatibility, but experiment authority is always the
 * fixed native Agent broker. No renderer-provided destination is used.
 */
export function useExperiment(_baseUrl?: string): UseExperimentResult {
  const [experimentId, setExperimentId] = useState<string | null>(null);
  const [activePhase, setActivePhase] = useState<string | null>(null);
  const [isRunning, setIsRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const mounted = useRef(true);
  const selectionSequence = useRef(0);

  useEffect(() => () => {
    mounted.current = false;
    selectionSequence.current += 1;
  }, []);

  const applyRecord = useCallback((record: ExperimentRecord, sequence: number) => {
    if (!mounted.current || sequence !== selectionSequence.current) return;
    setExperimentId(record.experimentId);
    setActivePhase(record.activePhase);
    setIsRunning(isRunningRecord(record));
  }, []);

  const showUncertainMutation = useCallback((outcome: MutationOutcome<unknown>) => {
    if (!mounted.current) return;
    setError(
      `The Agent did not acknowledge operation ${outcome.operationId}. It is held and will not be retried automatically. ` +
      `Review its authoritative status before taking another action.`,
    );
  }, []);

  const refresh = useCallback(async (id: string, sequence: number): Promise<ExperimentRecord | null> => {
    try {
      const record = await getExperimentStatus(id);
      applyRecord(record, sequence);
      return record;
    } catch (reason) {
      if (mounted.current && sequence === selectionSequence.current) {
        setError(reason instanceof Error ? reason.message : "Unable to load the authoritative experiment status.");
      }
      return null;
    }
  }, [applyRecord]);

  const startExperiment = useCallback(async (name?: string): Promise<string | null> => {
    const sequence = selectionSequence.current + 1;
    selectionSequence.current = sequence;
    setError(null);

    const outcome = await startAgentExperiment(name ?? "Experiment");
    if (!mounted.current || sequence !== selectionSequence.current) return null;
    if (outcome.uncertain || !outcome.result) {
      showUncertainMutation(outcome);
      return null;
    }

    applyRecord(outcome.result, sequence);
    // A post-mutation status read is safe. It never replays a start request.
    await refresh(outcome.result.experimentId, sequence);
    return outcome.result.experimentId;
  }, [applyRecord, refresh, showUncertainMutation]);

  const runControl = useCallback(async (control: "pause" | "resume" | "abort") => {
    if (!experimentId) return;
    const selectedId = experimentId;
    const sequence = selectionSequence.current + 1;
    selectionSequence.current = sequence;
    setError(null);

    const outcome = await controlAgentExperiment(selectedId, control);
    if (!mounted.current || sequence !== selectionSequence.current) return;
    if (outcome.uncertain) {
      showUncertainMutation(outcome);
      return;
    }

    // Keep even completed or aborted executions selected for attributable history.
    await refresh(selectedId, sequence);
  }, [experimentId, refresh, showUncertainMutation]);

  const pauseExperiment = useCallback(() => runControl("pause"), [runControl]);
  const resumeExperiment = useCallback(() => runControl("resume"), [runControl]);
  const abortExperiment = useCallback(() => runControl("abort"), [runControl]);

  return { experimentId, activePhase, isRunning, startExperiment, pauseExperiment, resumeExperiment, abortExperiment, error };
}
