import { renderHook, act, waitFor } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { useTrendRegistry } from "./useTrendRegistry";
import type { TrendValue } from "../types/agent";

function makeTrendValue(trendName: string, value = 1.0): TrendValue {
  return {
    type: "trend-value",
    trendName,
    value,
    unit: "°C",
    timestamp: new Date().toISOString(),
  };
}

describe("useTrendRegistry", () => {
  // Test 1: Given trendValues=[], returns [].
  it("returns empty array when trendValues is empty", () => {
    const { result } = renderHook(() => useTrendRegistry([]));
    expect(result.current).toEqual([]);
  });

  // Test 2: Given trendValues with one entry, returns ["temperature"].
  it("returns single trendName from one entry", async () => {
    const trendValues = [makeTrendValue("temperature")];
    const { result } = renderHook(() => useTrendRegistry(trendValues));
    await waitFor(() => {
      expect(result.current).toEqual(["temperature"]);
    });
  });

  // Test 3: Given multiple entries with same trendName repeated, returns deduplicated list (length 1).
  it("deduplicates repeated trendNames", async () => {
    const trendValues = [
      makeTrendValue("temperature"),
      makeTrendValue("temperature"),
      makeTrendValue("temperature"),
    ];
    const { result } = renderHook(() => useTrendRegistry(trendValues));
    await waitFor(() => {
      expect(result.current).toHaveLength(1);
      expect(result.current).toEqual(["temperature"]);
    });
  });

  // Test 4: Given entries with three distinct trendNames, returns sorted array of all three.
  it("returns sorted array of all distinct trendNames", async () => {
    const trendValues = [
      makeTrendValue("pressure"),
      makeTrendValue("temperature"),
      makeTrendValue("flow"),
    ];
    const { result } = renderHook(() => useTrendRegistry(trendValues));
    await waitFor(() => {
      expect(result.current).toEqual(["flow", "pressure", "temperature"]);
    });
  });

  // Test 5: After initial render with two names, adding a third name triggers re-render and appends it.
  it("appends new trendName when trendValues reference changes with new entry", async () => {
    const initial = [makeTrendValue("alpha"), makeTrendValue("beta")];
    const { result, rerender } = renderHook(
      ({ values }: { values: TrendValue[] }) => useTrendRegistry(values),
      { initialProps: { values: initial } }
    );
    await waitFor(() => {
      expect(result.current).toEqual(["alpha", "beta"]);
    });

    const updated = [...initial, makeTrendValue("gamma")];
    act(() => {
      rerender({ values: updated });
    });

    await waitFor(() => {
      expect(result.current).toEqual(["alpha", "beta", "gamma"]);
    });
  });

  // Test 6: Given trendValues reference changes (same names, new array), does NOT produce a new state update.
  it("does not update state when new array has same trendNames", async () => {
    const initial = [makeTrendValue("alpha")];
    const { result, rerender } = renderHook(
      ({ values }: { values: TrendValue[] }) => useTrendRegistry(values),
      { initialProps: { values: initial } }
    );
    await waitFor(() => {
      expect(result.current).toEqual(["alpha"]);
    });

    // Capture the reference to the result array
    const firstResultRef = result.current;

    // New array reference but same trendNames
    const sameNames = [makeTrendValue("alpha"), makeTrendValue("alpha")];
    act(() => {
      rerender({ values: sameNames });
    });

    // The state list should be the same reference (no new setState call with same content)
    expect(result.current).toEqual(["alpha"]);
    expect(result.current).toBe(firstResultRef);
  });
});
