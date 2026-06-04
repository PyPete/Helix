import { LazyStore } from "@tauri-apps/plugin-store";
import type { LayoutBase } from "rc-dock";

// Separate store file from helix-config.json (per D-10)
const store = new LazyStore("workspace-layouts.json");

/**
 * Provides save/load/list operations for named workspace layouts,
 * persisted via Tauri plugin-store under key "workspace-layouts".
 */
export function useWorkspaceLayouts() {
  /**
   * Save a named layout. Merges into existing stored layouts (does not overwrite).
   */
  const saveLayout = async (name: string, layout: LayoutBase): Promise<void> => {
    const existing =
      (await store.get<Record<string, LayoutBase>>("workspace-layouts")) ?? {};
    existing[name] = layout;
    await store.set("workspace-layouts", existing);
    await store.save();
  };

  /**
   * Load all saved layouts. Returns {} if none stored yet.
   */
  const loadLayouts = async (): Promise<Record<string, LayoutBase>> => {
    return (await store.get<Record<string, LayoutBase>>("workspace-layouts")) ?? {};
  };

  /**
   * List sorted names of all saved layouts.
   */
  const listLayoutNames = async (): Promise<string[]> => {
    const layouts = await loadLayouts();
    return Object.keys(layouts).sort();
  };

  return { saveLayout, loadLayouts, listLayoutNames };
}
