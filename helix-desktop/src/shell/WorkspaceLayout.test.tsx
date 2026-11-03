import { describe, expect, it } from "vitest";
import * as WorkspaceLayoutModule from "./WorkspaceLayout";
import { DEFAULT_LAYOUT } from "./WorkspaceLayout";

type BoardSourceInputs = {
  health: { healthy: boolean | null; error: string | null; lastChecked: Date | null };
  runspaceEvents: Array<{ type: string; runspaceId: string; timestamp: string; experimentId?: string }>;
  streamState: { source: "renderer-stream-state"; connected: boolean; loading: boolean; error: string | null; partial: boolean };
  selectedExecution: { source: "local-selection"; executionId: string | null };
  durableLifecycle: { source: "local-event-derived"; value: string | null };
};

const createBoardEvidenceInputs = (WorkspaceLayoutModule as unknown as {
  createBoardEvidenceInputs?: (inputs: BoardSourceInputs) => BoardSourceInputs;
}).createBoardEvidenceInputs;

describe("scientific workspace registration", () => {
  it("registers the original workflow board without removing legacy comparator tabs", () => {
    const serialized = JSON.stringify(DEFAULT_LAYOUT);
    expect(serialized).toContain("scientific-workflow-board");
    expect(serialized).toContain("scientific-apparatus");
    expect(serialized).toContain("scientific-procedures");
    expect(serialized).toContain("scientific-plans");
    expect(serialized).toContain("scientific-assembly");
    expect(serialized).toContain("scientific-samples");
    expect(serialized).toContain("experiment-timeline");
  });

  it("restores all five scientific tab identities", () => {
    const serialized = JSON.stringify(DEFAULT_LAYOUT);
    expect(serialized).toContain("scientific-apparatus");
    expect(serialized).toContain("scientific-procedures");
    expect(serialized).toContain("scientific-plans");
    expect(serialized).toContain("scientific-assembly");
    expect(serialized).toContain("scientific-samples");
  });

  it("preserves source-labelled stream and local context at the board seam", () => {
    expect(typeof createBoardEvidenceInputs).toBe("function");
    const inputs: BoardSourceInputs = {
      health: { healthy: true, error: null, lastChecked: null },
      runspaceEvents: [{ type: "runspace-started", runspaceId: "run-1", timestamp: "2026-11-03", experimentId: "execution-1" }],
      streamState: { source: "renderer-stream-state", connected: false, loading: true, error: "interrupted", partial: true },
      selectedExecution: { source: "local-selection", executionId: "execution-1" },
      durableLifecycle: { source: "local-event-derived", value: "Running" },
    };

    expect(createBoardEvidenceInputs?.(inputs)).toEqual(inputs);
  });
});
