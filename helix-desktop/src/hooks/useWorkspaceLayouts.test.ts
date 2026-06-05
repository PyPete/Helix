import { renderHook } from "@testing-library/react";
import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { useWorkspaceLayouts } from "./useWorkspaceLayouts";
import type { LayoutBase } from "rc-dock";

// In the test environment window.__TAURI_INTERNALS__ is not set, so the hook
// uses the localStorage fallback path. Tests exercise that path directly.

const STORAGE_KEY = "helix:workspace-layouts";

const makeLayout = (): LayoutBase => ({
  dockbox: {
    mode: "horizontal",
    children: [],
  },
});

describe("useWorkspaceLayouts", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it("saveLayout writes merged object to localStorage", async () => {
    const { result } = renderHook(() => useWorkspaceLayouts());
    const layout = makeLayout();

    await result.current.saveLayout("default", layout);

    const stored = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? "{}") as Record<string, LayoutBase>;
    expect(stored).toEqual({ default: layout });
  });

  it("saveLayout merges second save into existing stored object", async () => {
    const layoutA = makeLayout();
    const layoutB = { ...makeLayout(), dockbox: { mode: "vertical" as const, children: [] } };

    const { result } = renderHook(() => useWorkspaceLayouts());

    await result.current.saveLayout("alpha", layoutA);
    await result.current.saveLayout("beta", layoutB);

    const stored = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? "{}") as Record<string, LayoutBase>;
    expect(stored).toEqual({ alpha: layoutA, beta: layoutB });
  });

  it("loadLayouts returns stored layouts record", async () => {
    const stored = { default: makeLayout() };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));

    const { result } = renderHook(() => useWorkspaceLayouts());
    const layouts = await result.current.loadLayouts();

    expect(layouts).toEqual(stored);
  });

  it("loadLayouts returns empty object when nothing stored", async () => {
    const { result } = renderHook(() => useWorkspaceLayouts());
    const layouts = await result.current.loadLayouts();

    expect(layouts).toEqual({});
  });

  it("listLayoutNames returns sorted keys from stored layouts", async () => {
    const stored = {
      gamma: makeLayout(),
      alpha: makeLayout(),
      beta: makeLayout(),
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));

    const { result } = renderHook(() => useWorkspaceLayouts());
    const names = await result.current.listLayoutNames();

    expect(names).toEqual(["alpha", "beta", "gamma"]);
  });
});
