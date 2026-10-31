import { describe, expect, it, vi } from "vitest";

const { invoke } = vi.hoisted(() => ({ invoke: vi.fn() }));
vi.mock("@tauri-apps/api/core", () => ({ invoke }));

import { createApparatus, readScientificOperation } from "./scientificApparatusClient";

describe("scientificApparatusClient", () => {
  it("sends a typed create request through the native broker", async () => {
    invoke.mockResolvedValueOnce('{"operationId":"11111111-1111-1111-1111-111111111111","apparatusId":"22222222-2222-2222-2222-222222222222","status":"accepted"}');

    await expect(createApparatus({ name: "Reactor", operationId: "11111111-1111-1111-1111-111111111111" })).resolves.toMatchObject({ status: "accepted" });
    expect(invoke).toHaveBeenCalledWith("scientific_create", { request: '{"name":"Reactor","operationId":"11111111-1111-1111-1111-111111111111"}' });
  });

  it("forwards the original operation identifier to the fixed native route", async () => {
    invoke.mockResolvedValueOnce('{"operationId":"11111111-1111-1111-1111-111111111111","apparatusId":"22222222-2222-2222-2222-222222222222","status":"completed"}');

    await readScientificOperation("11111111-1111-1111-1111-111111111111");
    expect(invoke).toHaveBeenCalledWith("scientific_operation", { operationId: "11111111-1111-1111-1111-111111111111" });
  });
});
