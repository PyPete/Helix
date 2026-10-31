import { invoke } from "@tauri-apps/api/core";
import type { PlanDraft, ProcedureDraft, ScientificDraftState, ScientificKind, ScientificReceipt, ScientificRevision, ScientificSummary } from "../types/scientific";

function parse<T>(response: string): T { return JSON.parse(response) as T; }

export async function listScientific(kind: ScientificKind): Promise<ScientificSummary[]> {
  return parse(await invoke<string>("scientific_list_kind", { kind }));
}

export async function createScientific(kind: ScientificKind, draft: ProcedureDraft | PlanDraft): Promise<ScientificReceipt> {
  return parse(await invoke<string>("scientific_create_kind", { kind, request: JSON.stringify(draft) }));
}

export async function readScientificDraft<T>(kind: ScientificKind, resourceId: string): Promise<ScientificDraftState<T>> {
  return parse(await invoke<string>("scientific_read_draft_kind", { kind, resourceId }));
}

export async function saveScientificDraft(kind: ScientificKind, resourceId: string, draft: ProcedureDraft | PlanDraft): Promise<{ receipt: ScientificReceipt; token: number }> {
  return parse(await invoke<string>("scientific_save_draft_kind", { kind, resourceId, request: JSON.stringify(draft) }));
}

export async function publishScientific(kind: ScientificKind, resourceId: string, request: { operationId: string; revisionName: string; expectedToken: number }): Promise<ScientificReceipt> {
  return parse(await invoke<string>("scientific_publish_kind", { kind, resourceId, request: JSON.stringify(request) }));
}

export async function revisions(kind: ScientificKind, resourceId: string): Promise<ScientificRevision[]> {
  return parse(await invoke<string>("scientific_revisions_kind", { kind, resourceId }));
}

export async function readScientificOperation(kind: ScientificKind, operationId: string): Promise<ScientificReceipt> {
  return parse(await invoke<string>("scientific_operation_result", { kind, operationId }));
}
