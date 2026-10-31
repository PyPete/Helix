import { describe, expect, it, vi } from "vitest";

const { invoke } = vi.hoisted(() => ({ invoke: vi.fn() }));
vi.mock("@tauri-apps/api/core", () => ({ invoke }));

import { createScientific } from "./scientificClient";

describe("scientificClient", () => {
  it("rejects a malformed receipt instead of reporting a false scientific commit", async () => {
    invoke.mockResolvedValueOnce('{"status":"Committed"}');
    await expect(createScientific("procedure", { name: "P", operationId: "op", wsl: { nodes: [], edges: [] }, declarations: [], plannedCollections: [] })).rejects.toThrow("receipt");
  });
});
