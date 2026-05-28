import { renderHook, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";
import { useAgentHealth } from "./useAgentHealth";

describe("useAgentHealth", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("healthy is null before first check", () => {
    // Stub fetch to a promise that never resolves
    vi.stubGlobal(
      "fetch",
      vi.fn(() => new Promise(() => { /* intentionally never resolves */ }))
    );

    const { result } = renderHook(() =>
      useAgentHealth("http://localhost:5080", 999999)
    );
    expect(result.current.healthy).toBeNull();
    expect(result.current.error).toBeNull();
    expect(result.current.lastChecked).toBeNull();
  });

  it("healthy becomes true when fetch returns 200", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() =>
        Promise.resolve({
          ok: true,
          status: 200,
        } as Response)
      )
    );

    const { result } = renderHook(() =>
      useAgentHealth("http://localhost:5080", 999999)
    );

    await waitFor(() => {
      expect(result.current.healthy).toBe(true);
    });
    expect(result.current.error).toBeNull();
    expect(result.current.lastChecked).toBeInstanceOf(Date);
  });

  it("healthy becomes false when fetch returns 500", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() =>
        Promise.resolve({
          ok: false,
          status: 500,
        } as Response)
      )
    );

    const { result } = renderHook(() =>
      useAgentHealth("http://localhost:5080", 999999)
    );

    await waitFor(() => {
      expect(result.current.healthy).toBe(false);
    });
    expect(result.current.error).toBe("HTTP 500");
    expect(result.current.lastChecked).toBeInstanceOf(Date);
  });

  it("error is set when fetch throws (network error)", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() => Promise.reject(new Error("Network failure")))
    );

    const { result } = renderHook(() =>
      useAgentHealth("http://localhost:5080", 999999)
    );

    await waitFor(() => {
      expect(result.current.healthy).toBe(false);
    });
    expect(result.current.error).toBe("Network failure");
    expect(result.current.lastChecked).toBeInstanceOf(Date);
  });

  it("AbortSignal.timeout(3000) is used in fetch call", async () => {
    const abortSignal = {} as AbortSignal;
    const timeoutSpy = vi
      .spyOn(AbortSignal, "timeout")
      .mockReturnValue(abortSignal);

    vi.stubGlobal(
      "fetch",
      vi.fn(() =>
        Promise.resolve({
          ok: true,
          status: 200,
        } as Response)
      )
    );

    const { result } = renderHook(() =>
      useAgentHealth("http://localhost:5080", 999999)
    );

    await waitFor(() => {
      expect(result.current.healthy).toBe(true);
    });

    expect(timeoutSpy).toHaveBeenCalledWith(3000);
    const fetchMock = vi.mocked(fetch);
    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5080/health",
      expect.objectContaining({ signal: abortSignal })
    );
  });
});
