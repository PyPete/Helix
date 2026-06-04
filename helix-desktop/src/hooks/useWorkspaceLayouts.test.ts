import { renderHook } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { useWorkspaceLayouts } from "./useWorkspaceLayouts";
import type { LayoutBase } from "rc-dock";

const { mockSet, mockSave, mockGet } = vi.hoisted(() => ({
  mockSet: vi.fn().mockResolvedValue(undefined),
  mockSave: vi.fn().mockResolvedValue(undefined),
  mockGet: vi.fn(),
}));

vi.mock("@tauri-apps/plugin-store", () => {
  const LazyStoreMock = vi.fn(function (this: Record<string, unknown>, _name: string) {
    this.set = mockSet;
    this.save = mockSave;
    this.get = mockGet;
  });
  return { LazyStore: LazyStoreMock };
});

const makeLayout = (): LayoutBase => ({
  dockbox: {
    mode: "horizontal",
    children: [],
  },
});

describe("useWorkspaceLayouts", () => {
  beforeEach(() => {
    mockSet.mockResolvedValue(undefined);
    mockSave.mockResolvedValue(undefined);
    mockGet.mockResolvedValue(null);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  // Test 1: saveLayout("default", layout) calls store.set("workspace-layouts", {default: layout})
  // then store.save().
  it("saveLayout calls store.set with merged object then store.save", async () => {
    mockGet.mockResolvedValue(null); // no existing layouts
    const { result } = renderHook(() => useWorkspaceLayouts());
    const layout = makeLayout();

    await result.current.saveLayout("default", layout);

    expect(mockSet).toHaveBeenCalledWith("workspace-layouts", { default: layout });
    expect(mockSave).toHaveBeenCalled();
  });

  // Test 2: saveLayout called twice with different names merges both into stored object.
  it("saveLayout merges second save into existing stored object", async () => {
    const layoutA = makeLayout();
    const layoutB = { ...makeLayout(), dockbox: { mode: "vertical" as const, children: [] } };

    // First call: no existing data
    mockGet.mockResolvedValueOnce(null);
    // Second call: returns what was set in the first call
    mockGet.mockResolvedValueOnce({ alpha: layoutA });

    const { result } = renderHook(() => useWorkspaceLayouts());

    await result.current.saveLayout("alpha", layoutA);
    await result.current.saveLayout("beta", layoutB);

    // Second save should merge into existing {alpha: layoutA}
    expect(mockSet).toHaveBeenNthCalledWith(2, "workspace-layouts", {
      alpha: layoutA,
      beta: layoutB,
    });
  });

  // Test 3: loadLayouts() returns the stored Record<string, LayoutBase> from store.get.
  it("loadLayouts returns stored layouts record", async () => {
    const stored = { default: makeLayout() };
    mockGet.mockResolvedValue(stored);

    const { result } = renderHook(() => useWorkspaceLayouts());
    const layouts = await result.current.loadLayouts();

    expect(mockGet).toHaveBeenCalledWith("workspace-layouts");
    expect(layouts).toEqual(stored);
  });

  // Test 4: loadLayouts() returns {} when store.get returns null.
  it("loadLayouts returns empty object when store returns null", async () => {
    mockGet.mockResolvedValue(null);

    const { result } = renderHook(() => useWorkspaceLayouts());
    const layouts = await result.current.loadLayouts();

    expect(layouts).toEqual({});
  });

  // Test 5: listLayoutNames() returns sorted string array of keys from stored layouts.
  it("listLayoutNames returns sorted keys from stored layouts", async () => {
    const stored = {
      gamma: makeLayout(),
      alpha: makeLayout(),
      beta: makeLayout(),
    };
    mockGet.mockResolvedValue(stored);

    const { result } = renderHook(() => useWorkspaceLayouts());
    const names = await result.current.listLayoutNames();

    expect(names).toEqual(["alpha", "beta", "gamma"]);
  });
});
