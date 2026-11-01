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

  it("rejects a confirmation receipt with the wrong original operation identity", async () => {
    invoke.mockResolvedValueOnce(JSON.stringify({
      sample: { identity: { sampleId: "sample-1", origin: "Collection", parentSampleIds: [] }, location: "Hood", serverSequence: 1, lastEventId: "event-1" },
      receipt: { operationId: "different-operation", kind: "sample.confirm", sampleId: "sample-1", actorSid: "scientist", recordedAtUtc: "2026-11-01T12:00:00Z", payload: "{}" },
    }));
    await expect(scientificClient.confirmSample({ operationId: "original-operation", location: "Hood", occurredAtUtc: "2026-11-01T11:00:00Z" })).rejects.toThrow("receipt");
  });

  it("rejects malformed sample history rather than treating it as evidence", async () => {
    invoke.mockResolvedValueOnce(JSON.stringify([{ eventId: "event-1", sampleId: "other-sample", kind: "Confirmed" }]));
    await expect(scientificClient.sampleEvents("sample-1")).rejects.toThrow("history");
  });

  it("submits complete transformations with the original operation identity and rejects malformed acknowledgements", async () => {
    const client = scientificClient as typeof scientificClient & {
      transformSample: (request: unknown) => Promise<unknown>;
    };
    expect(typeof client.transformSample).toBe("function");
    invoke.mockResolvedValueOnce(JSON.stringify({ operationId: "different-operation", childIds: ["child-1"] }));
    await expect(client.transformSample({
      operationId: "original-operation", transformationId: "activity-1", kind: "Split", operation: "Aliquot",
      parentSampleIds: ["parent-1"], childSampleIds: ["child-1"], quantitySupplied: false,
    })).rejects.toThrow("transformation");
  });

  it("reads bounded lineage only when every entry and continuation match the requested sample", async () => {
    const client = scientificClient as typeof scientificClient & {
      sampleLineage: (sampleId: string, direction: string, continuation?: string, pageSize?: number) => Promise<unknown>;
    };
    expect(typeof client.sampleLineage).toBe("function");
    invoke.mockResolvedValueOnce(JSON.stringify({ entries: [{ sampleId: "other-sample", origin: "Transformation", depth: 1 }], continuation: null, isTruncated: false }));
    await expect(client.sampleLineage("sample-1", "Ancestors")).rejects.toThrow("lineage");
  });
});
