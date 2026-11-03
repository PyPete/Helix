import { describe, expect, it } from "vitest";
import { DEFAULT_LAYOUT } from "./WorkspaceLayout";

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
});
