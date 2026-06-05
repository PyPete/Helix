import type { LayoutBase } from "rc-dock";

const STORAGE_KEY = "helix:workspace-layouts";

// Use Tauri plugin-store when running inside the desktop app; fall back to
// localStorage so save/load works during browser-based `npm run dev` sessions.
const isTauri = typeof window !== "undefined" && "__TAURI_INTERNALS__" in window;

async function readStore(): Promise<Record<string, LayoutBase>> {
  if (isTauri) {
    const { LazyStore } = await import("@tauri-apps/plugin-store");
    const store = new LazyStore("workspace-layouts.json");
    return (await store.get<Record<string, LayoutBase>>("workspace-layouts")) ?? {};
  }
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY) ?? "{}") as Record<string, LayoutBase>;
  } catch {
    return {};
  }
}

async function writeStore(layouts: Record<string, LayoutBase>): Promise<void> {
  if (isTauri) {
    const { LazyStore } = await import("@tauri-apps/plugin-store");
    const store = new LazyStore("workspace-layouts.json");
    await store.set("workspace-layouts", layouts);
    await store.save();
    return;
  }
  localStorage.setItem(STORAGE_KEY, JSON.stringify(layouts));
}

export function useWorkspaceLayouts() {
  const saveLayout = async (name: string, layout: LayoutBase): Promise<void> => {
    const existing = await readStore();
    existing[name] = layout;
    await writeStore(existing);
  };

  const loadLayouts = async (): Promise<Record<string, LayoutBase>> => {
    return readStore();
  };

  const listLayoutNames = async (): Promise<string[]> => {
    const layouts = await readStore();
    return Object.keys(layouts).sort();
  };

  return { saveLayout, loadLayouts, listLayoutNames };
}
