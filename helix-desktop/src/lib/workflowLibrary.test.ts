import { describe, it, expect, vi, afterEach } from "vitest";
import type { WslDocument } from "../types/agent";

// Use vi.hoisted so mock functions are available when vi.mock factory runs
const { mockWrite, mockRead, mockReadDir, mockMkdir } = vi.hoisted(() => ({
  mockWrite: vi.fn().mockResolvedValue(undefined),
  mockRead: vi.fn(),
  mockReadDir: vi.fn(),
  mockMkdir: vi.fn().mockResolvedValue(undefined),
}));

vi.mock("@tauri-apps/plugin-fs", () => ({
  writeTextFile: mockWrite,
  readTextFile: mockRead,
  readDir: mockReadDir,
  mkdir: mockMkdir,
  BaseDirectory: { AppData: 0 },
}));

import { saveWorkflow, loadWorkflow, listWorkflows, duplicateWorkflow } from "./workflowLibrary";

const doc: WslDocument = {
  documentId: "doc-1",
  name: "My Workflow",
  schemaVersion: "1.0",
  nodes: [],
  edges: [],
};

afterEach(() => {
  vi.clearAllMocks();
  vi.restoreAllMocks();
});

describe("workflowLibrary", () => {
  it("saveWorkflow writes sanitized name to workflows/{name}.json", async () => {
    await saveWorkflow("My Workflow!!", doc);
    expect(mockWrite).toHaveBeenCalledWith(
      "workflows/My_Workflow__.json",
      expect.any(String),
      expect.objectContaining({ baseDir: 0 })
    );
  });

  it("saveWorkflow throws for empty name after sanitization", async () => {
    await expect(saveWorkflow("!!!", doc)).rejects.toThrow("alphanumeric");
  });

  it("loadWorkflow returns parsed WslDocument", async () => {
    mockRead.mockResolvedValue(JSON.stringify(doc));
    const result = await loadWorkflow("test");
    expect(result.name).toBe(doc.name);
  });

  it("listWorkflows returns names without .json extension", async () => {
    mockReadDir.mockResolvedValue([{ name: "test.json" }, { name: "other.json" }]);
    const names = await listWorkflows();
    expect(names).toEqual(["test", "other"]);
  });

  it("duplicateWorkflow saves with new name and new documentId", async () => {
    mockRead.mockResolvedValue(JSON.stringify(doc));
    await duplicateWorkflow("test", "test-copy");
    expect(mockWrite).toHaveBeenCalledWith(
      "workflows/test-copy.json",
      expect.stringContaining('"name": "test-copy"'),
      expect.any(Object)
    );
  });
});
