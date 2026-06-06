import { useState, useCallback } from "react";
import type { ExperimentRecord } from "../types/agent";

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

export function useExperiment(baseUrl: string): UseExperimentResult {
  const [experimentId, setExperimentId] = useState<string | null>(null);
  const [activePhase, setActivePhase] = useState<string | null>(null);
  const [isRunning, setIsRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const startExperiment = useCallback(async (name?: string): Promise<string | null> => {
    setError(null);
    try {
      const res = await fetch(`${baseUrl}/experiments`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: name ?? "Experiment" }),
      });
      if (!res.ok) { setError("Failed to start experiment: " + res.statusText); return null; }
      const data: ExperimentRecord = await res.json();
      setExperimentId(data.experimentId);
      setActivePhase(data.activePhase);
      setIsRunning(true);
      return data.experimentId;
    } catch (e) {
      setError((e as Error).message);
      return null;
    }
  }, [baseUrl]);

  const pauseExperiment = useCallback(async () => {
    if (!experimentId) return;
    await fetch(`${baseUrl}/experiments/${experimentId}/pause`, { method: "POST" });
    setIsRunning(false);
  }, [baseUrl, experimentId]);

  const resumeExperiment = useCallback(async () => {
    if (!experimentId) return;
    await fetch(`${baseUrl}/experiments/${experimentId}/resume`, { method: "POST" });
    setIsRunning(true);
  }, [baseUrl, experimentId]);

  const abortExperiment = useCallback(async () => {
    if (!experimentId) return;
    await fetch(`${baseUrl}/experiments/${experimentId}/abort`, { method: "POST" });
    setIsRunning(false);
    setExperimentId(null);
    setActivePhase(null);
  }, [baseUrl, experimentId]);

  return { experimentId, activePhase, isRunning, startExperiment, pauseExperiment, resumeExperiment, abortExperiment, error };
}
