import { renderHook, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

const { pollCommittedEvidenceMock } = vi.hoisted(() => ({ pollCommittedEvidenceMock: vi.fn() }));

vi.mock("../lib/agentClient", () => ({ pollCommittedEvidence: pollCommittedEvidenceMock }));

import { useAgentWebSocket } from "./useAgentWebSocket";

const commandId = "11111111-1111-4111-8111-111111111111";

function cursorPages() {
  return (async function* () {
    yield {
      cursor: null,
      nextCursor: "committed-1",
      records: [{
        intent: { commandId, correlationId: "correlation", verifiedActorDisplayName: "Scientist", capability: "SetFlow", deviceId: "simulator" },
        evidence: { commandId, state: "Completed", sequence: 1, checks: [] },
      }],
    };
  })();
}

describe("useAgentWebSocket", () => {
  afterEach(() => vi.restoreAllMocks());

  it("reads committed native cursor facts and exposes the compatible event display API", async () => {
    pollCommittedEvidenceMock.mockReturnValue(cursorPages());
    const { result } = renderHook(() => useAgentWebSocket("https://untrusted.invalid"));

    await waitFor(() => expect(result.current.connected).toBe(true));
    expect(result.current.runspaceEvents).toHaveLength(1);
    expect(result.current.runspaceEvents[0]).toMatchObject({ type: "command-evidence", runspaceId: commandId });
    expect(pollCommittedEvidenceMock).toHaveBeenCalledWith(expect.objectContaining({ cursor: null }));
  });

  it("shows session read failures instead of a false connected state", async () => {
    pollCommittedEvidenceMock.mockImplementation(() => {
      throw new Error("No desktop Agent session is available.");
    });
    const { result } = renderHook(() => useAgentWebSocket("https://untrusted.invalid"));

    await waitFor(() => expect(result.current.error).toBe("No desktop Agent session is available."));
    expect(result.current.connected).toBe(false);
  });
});
