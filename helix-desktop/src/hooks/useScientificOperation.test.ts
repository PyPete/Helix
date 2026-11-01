import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useScientificOperation } from "./useScientificOperation";

vi.mock("../lib/scientificClient", () => ({ readScientificOperation: vi.fn() }));

describe("useScientificOperation", () => {
  it("retains an uncertain original operation for reconciliation without reporting a commit", () => {
    const { result } = renderHook(() => useScientificOperation("scientist-a"));
    act(() => {
      result.current.begin({ kind: "procedure", operationId: "original-operation", request: "draft" });
      result.current.markUncertain();
    });
    expect(result.current.state).toBe("uncertain");
    expect(result.current.context).toEqual({ kind: "procedure", operationId: "original-operation", request: "draft" });
  });
});
