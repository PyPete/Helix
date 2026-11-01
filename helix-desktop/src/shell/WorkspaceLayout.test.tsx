import { describe, expect, it } from "vitest";
import { DEFAULT_LAYOUT } from "./WorkspaceLayout";

describe("scientific workspace registration", () => {
  it("restores separate procedure, plan, assembly, and sample tab identities", () => {
    const serialized = JSON.stringify(DEFAULT_LAYOUT);
    expect(serialized).toContain("scientific-procedures");
    expect(serialized).toContain("scientific-plans");
    expect(serialized).toContain("scientific-assembly");
    expect(serialized).toContain("scientific-samples");
  });
});
