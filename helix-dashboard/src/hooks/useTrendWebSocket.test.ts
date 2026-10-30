import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { renderHook, waitFor, act } from "@testing-library/react";
import { useTrendWebSocket } from "./useTrendWebSocket";

const { mockClose } = vi.hoisted(() => ({ mockClose: vi.fn() }));

interface MockWs {
  onopen: (() => void) | null;
  onclose: (() => void) | null;
  onerror: (() => void) | null;
  onmessage: ((e: { data: string }) => void) | null;
  close: typeof mockClose;
}

let lastWs: MockWs | null = null;

describe("useTrendWebSocket", () => {
  beforeEach(() => {
    lastWs = null;
    vi.stubGlobal("WebSocket", vi.fn(function (this: MockWs) {
      this.onopen = null;
      this.onclose = null;
      this.onerror = null;
      this.onmessage = null;
      this.close = mockClose;
      lastWs = this;
    }));
  });

  afterEach(() => {
    vi.restoreAllMocks();
    mockClose.mockReset();
  });

  it("starts disconnected", () => {
    const { result } = renderHook(() => useTrendWebSocket("http://localhost:5000"));
    expect(result.current.connected).toBe(false);
  });

  it("sets connected=true on open", async () => {
    const { result } = renderHook(() => useTrendWebSocket("http://localhost:5000"));
    act(() => lastWs!.onopen?.());
    await waitFor(() => expect(result.current.connected).toBe(true));
  });

  it("appends trend-value frame to trendValues", async () => {
    const { result } = renderHook(() => useTrendWebSocket("http://localhost:5000"));
    act(() => lastWs!.onopen?.());
    act(() => lastWs!.onmessage?.({
      data: JSON.stringify({
        type: "trend-value", trendName: "Temp",
        value: 37.5, unit: "C", timestamp: "2026-06-02T00:00:00Z",
      }),
    }));
    await waitFor(() => expect(result.current.trendValues).toHaveLength(1));
    expect(result.current.trendValues[0].trendName).toBe("Temp");
  });

  it("ignores unknown frame types", async () => {
    const { result } = renderHook(() => useTrendWebSocket("http://localhost:5000"));
    act(() => lastWs!.onopen?.());
    act(() => lastWs!.onmessage?.({ data: JSON.stringify({ type: "unknown" }) }));
    await waitFor(() => expect(result.current.trendValues).toHaveLength(0));
  });

  it("calls ws.close on unmount", () => {
    const { unmount } = renderHook(() => useTrendWebSocket("http://localhost:5000"));
    unmount();
    expect(mockClose).toHaveBeenCalledOnce();
  });

  it("constructs URL with ws:// and /api/trends/ws path", () => {
    renderHook(() => useTrendWebSocket("http://localhost:5000"));
    expect(vi.mocked(WebSocket)).toHaveBeenCalledWith("ws://localhost:5000/api/trends/ws");
  });
});
