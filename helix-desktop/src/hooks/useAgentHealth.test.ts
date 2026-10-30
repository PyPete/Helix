import { renderHook, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

const { getAgentStatusMock } = vi.hoisted(() => ({ getAgentStatusMock: vi.fn() }));

vi.mock("../lib/agentClient", () => ({ getAgentStatus: getAgentStatusMock }));

import { useAgentHealth } from "./useAgentHealth";

describe("useAgentHealth", () => {
  afterEach(() => vi.restoreAllMocks());

  it("starts unknown while its authenticated native read is pending", () => {
    getAgentStatusMock.mockReturnValue(new Promise(() => undefined));
    const { result } = renderHook(() => useAgentHealth("https://untrusted.invalid", 999999));

    expect(result.current).toMatchObject({ healthy: null, error: null, lastChecked: null });
  });

  it("reports a healthy native Agent response without browser networking", async () => {
    getAgentStatusMock.mockResolvedValue("Healthy");
    const { result } = renderHook(() => useAgentHealth("https://untrusted.invalid", 999999));

    await waitFor(() => expect(result.current.healthy).toBe(true));
    expect(getAgentStatusMock).toHaveBeenCalled();
    expect(result.current.error).toBeNull();
  });

  it("keeps an unavailable session visibly unhealthy", async () => {
    getAgentStatusMock.mockRejectedValue(new Error("No desktop Agent session is available."));
    const { result } = renderHook(() => useAgentHealth("https://untrusted.invalid", 999999));

    await waitFor(() => expect(result.current.healthy).toBe(false));
    expect(result.current.error).toBe("No desktop Agent session is available.");
  });
});
