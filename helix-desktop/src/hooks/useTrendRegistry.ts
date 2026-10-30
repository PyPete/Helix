import { useState, useEffect, useRef } from "react";
import type { TrendValue } from "../types/agent";

/**
 * Accumulates unique trendName strings from a streaming TrendValue array.
 * Returns a stable sorted string[] that only grows (never shrinks within a session).
 *
 * Uses useRef for the Set to avoid including it in useEffect deps,
 * preventing re-subscription on every render.
 */
export function useTrendRegistry(trendValues: TrendValue[]): string[] {
  const [trendNames, setTrendNames] = useState<string[]>([]);
  const seenRef = useRef<Set<string>>(new Set());

  useEffect(() => {
    let changed = false;
    for (const tv of trendValues) {
      if (!seenRef.current.has(tv.trendName)) {
        seenRef.current.add(tv.trendName);
        changed = true;
      }
    }
    if (changed) {
      // Snapshot the Set into a stable sorted array
      setTrendNames(Array.from(seenRef.current).sort());
    }
    // trendValues is the only dep — seenRef is stable by design
  }, [trendValues]);

  return trendNames;
}
