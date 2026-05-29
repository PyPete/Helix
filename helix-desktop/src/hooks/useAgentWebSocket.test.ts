import { renderHook, waitFor, act } from "@testing-library/react";
import { describe, it, expect, vi, afterEach, beforeEach } from "vitest";
import { useAgentWebSocket } from "./useAgentWebSocket";

// Use vi.hoisted so mockClose is available when vi.stubGlobal factory runs
const { mockClose } = vi.hoisted(() => ({
  mockClose: vi.fn(),
}));

// Track last WebSocket instance so tests can simulate messages
let lastWsInstance: {
  onopen: (() => void) | null;
  onclose: (() => void) | null;
  onerror: (() => void) | null;
  onmessage: ((evt: { data: string }) => void) | null;
  close: () => void;
} | null = null;

describe("useAgentWebSocket", () => {
  beforeEach(() => {
    lastWsInstance = null;
    vi.stubGlobal(
      "WebSocket",
      vi.fn(function (this: typeof lastWsInstance) {
        this!.onopen = null;
        this!.onclose = null;
        this!.onerror = null;
        this!.onmessage = null;
        this!.close = mockClose;
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        lastWsInstance = this as any;
      })
    );
  });

  afterEach(() => {
    vi.restoreAllMocks();
    mockClose.mockReset();
  });

  it("starts with empty state and connected=false before WS opens", () => {
    const { result } = renderHook(() =>
      useAgentWebSocket("http://localhost:5080")
    );
    expect(result.current.connected).toBe(false);
    expect(result.current.runspaceEvents).toHaveLength(0);
    expect(result.current.trendValues).toHaveLength(0);
    expect(result.current.safetyEvents).toHaveLength(0);
  });

  it("dispatches trend-value frame to trendValues array", async () => {
    const { result } = renderHook(() =>
      useAgentWebSocket("http://localhost:5080")
    );

    const frame = {
      type: "trend-value",
      trendName: "temperature",
      value: 25.3,
      unit: "C",
      timestamp: "2026-05-29T00:00:00Z",
    };

    act(() => {
      lastWsInstance!.onmessage!({ data: JSON.stringify(frame) });
    });

    await waitFor(() =>
      expect(result.current.trendValues).toHaveLength(1)
    );
    expect(result.current.trendValues[0].trendName).toBe("temperature");
    expect(result.current.trendValues[0].value).toBe(25.3);
  });

  it("dispatches safety-event frame to safetyEvents array", async () => {
    const { result } = renderHook(() =>
      useAgentWebSocket("http://localhost:5080")
    );

    const frame = {
      type: "safety-event",
      ruleId: "temp-limit",
      description: "Temperature exceeded",
      severity: "High",
      triggered: true,
      timestamp: "2026-05-29T00:00:00Z",
    };

    act(() => {
      lastWsInstance!.onmessage!({ data: JSON.stringify(frame) });
    });

    await waitFor(() =>
      expect(result.current.safetyEvents).toHaveLength(1)
    );
    expect(result.current.safetyEvents[0].ruleId).toBe("temp-limit");
  });

  it("dispatches RunspaceEvent (non-trend, non-safety) to runspaceEvents array", async () => {
    const { result } = renderHook(() =>
      useAgentWebSocket("http://localhost:5080")
    );

    const frame = {
      type: "runspace-started",
      runspaceId: "rs-123",
      timestamp: "2026-05-29T00:00:00Z",
    };

    act(() => {
      lastWsInstance!.onmessage!({ data: JSON.stringify(frame) });
    });

    await waitFor(() =>
      expect(result.current.runspaceEvents).toHaveLength(1)
    );
    expect(result.current.runspaceEvents[0].runspaceId).toBe("rs-123");
  });

  it("ignores malformed JSON frames without throwing", async () => {
    const { result } = renderHook(() =>
      useAgentWebSocket("http://localhost:5080")
    );

    act(() => {
      lastWsInstance!.onmessage!({ data: "not valid json {{{{" });
    });

    // State remains empty -- no throw
    expect(result.current.runspaceEvents).toHaveLength(0);
    expect(result.current.trendValues).toHaveLength(0);
    expect(result.current.safetyEvents).toHaveLength(0);
  });
});