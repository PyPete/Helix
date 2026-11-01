import { describe, expect, it, vi } from "vitest";

const { invoke } = vi.hoisted(() => ({ invoke: vi.fn() }));
vi.mock("@tauri-apps/api/core", () => ({ invoke }));

import * as scientificClient from "./scientificClient";

describe("scientificClient", () => {
  it("rejects a malformed receipt instead of reporting a false scientific commit", async () => {
    invoke.mockResolvedValueOnce('{"status":"Committed"}');
    await expect(scientificClient.createScientific("procedure", { name: "P", operationId: "op", wsl: { nodes: [], edges: [] }, declarations: [], plannedCollections: [] })).rejects.toThrow("receipt");
  });

  it("exposes typed native operations for immutable sample evidence", () => {
    expect(typeof (scientificClient as Record<string, unknown>).listSamples).toBe("function");
    expect(typeof (scientificClient as Record<string, unknown>).confirmSample).toBe("function");
    expect(typeof (scientificClient as Record<string, unknown>).appendSampleEvent).toBe("function");
    expect(typeof (scientificClient as Record<string, unknown>).sampleEvents).toBe("function");
    expect(typeof (scientificClient as Record<string, unknown>).readSample).toBe("function");
  });
});
