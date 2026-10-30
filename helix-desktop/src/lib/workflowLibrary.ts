import { readTextFile, writeTextFile, BaseDirectory, readDir, mkdir } from "@tauri-apps/plugin-fs";
import type { WslDocument } from "../types/agent";

/** Sanitize workflow name to [a-zA-Z0-9_-] only. */
function sanitizeName(name: string): string {
  const safe = name.replace(/[^a-zA-Z0-9_-]/g, "_");
  if (!safe || !/[a-zA-Z0-9]/.test(safe)) {
    throw new Error("Workflow name must contain at least one alphanumeric character.");
  }
  return safe;
}

/** Ensure workflows directory exists. */
async function ensureDir(): Promise<void> {
  try {
    await mkdir("workflows", { baseDir: BaseDirectory.AppData, recursive: true });
  } catch {
    // Directory may already exist; ignore
  }
}

export async function saveWorkflow(name: string, doc: WslDocument): Promise<void> {
  const safe = sanitizeName(name);
  await ensureDir();
  await writeTextFile(`workflows/${safe}.json`, JSON.stringify(doc, null, 2), {
    baseDir: BaseDirectory.AppData,
  });
}

export async function loadWorkflow(name: string): Promise<WslDocument> {
  const safe = sanitizeName(name);
  const text = await readTextFile(`workflows/${safe}.json`, {
    baseDir: BaseDirectory.AppData,
  });
  return JSON.parse(text) as WslDocument;
}

export async function listWorkflows(): Promise<string[]> {
  await ensureDir();
  const entries = await readDir("workflows", { baseDir: BaseDirectory.AppData });
  return entries
    .filter(e => e.name?.endsWith(".json"))
    .map(e => e.name!.replace(/\.json$/, ""));
}

export async function duplicateWorkflow(name: string, newName: string): Promise<void> {
  const doc = await loadWorkflow(name);
  await saveWorkflow(newName, { ...doc, documentId: crypto.randomUUID(), name: newName });
}
