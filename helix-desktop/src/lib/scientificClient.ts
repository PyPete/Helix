import { invoke } from "@tauri-apps/api/core";
import type { PlanDraft, ProcedureDraft, ScientificDraftState, ScientificKind, ScientificReceipt, ScientificRevision, ScientificSummary } from "../types/scientific";

function parse<T>(response: string): T { return JSON.parse(response) as T; }

function receipt(response: string, kind: ScientificKind): ScientificReceipt {
  const value = parse<ScientificReceipt>(response);
  if (!value || value.kind !== kind || !value.operationId || !value.resourceId || value.status !== "Committed") {
    throw new Error("Agent returned an invalid scientific receipt.");
  }
  return value;
}

export async function listScientific(kind: ScientificKind): Promise<ScientificSummary[]> {
  return parse(await invoke<string>("scientific_list_kind", { kind }));
}

export async function createScientific(kind: ScientificKind, draft: ProcedureDraft | PlanDraft): Promise<ScientificReceipt> {
  return receipt(await invoke<string>("scientific_create_kind", { kind, request: JSON.stringify(draft) }), kind);
}

export async function readScientificDraft<T>(kind: ScientificKind, resourceId: string): Promise<ScientificDraftState<T>> {
  return parse(await invoke<string>("scientific_read_draft_kind", { kind, resourceId }));
}

export async function saveScientificDraft(kind: ScientificKind, resourceId: string, draft: ProcedureDraft | PlanDraft): Promise<{ receipt: ScientificReceipt; token: number }> {
  return parse(await invoke<string>("scientific_save_draft_kind", { kind, resourceId, request: JSON.stringify(draft) }));
}

export async function publishScientific(kind: ScientificKind, resourceId: string, request: { operationId: string; revisionName: string; expectedToken: number }): Promise<ScientificReceipt> {
  return receipt(await invoke<string>("scientific_publish_kind", { kind, resourceId, request: JSON.stringify(request) }), kind);
}

export async function revisions(kind: ScientificKind, resourceId: string): Promise<ScientificRevision[]> {
  return parse(await invoke<string>("scientific_revisions_kind", { kind, resourceId }));
}

export async function readScientificOperation(kind: ScientificKind, operationId: string): Promise<ScientificReceipt> {
  return receipt(await invoke<string>("scientific_operation_result", { kind, operationId }), kind);
}

export async function deriveProcedure(request: { operationId: string; sourceRevisionId: string; name: string }): Promise<ScientificReceipt> {
  const response = parse<{ receipt?: ScientificReceipt }>(await invoke<string>("scientific_derive_procedure", { request: JSON.stringify(request) }));
  if (!response.receipt || response.receipt.operationId !== request.operationId || !response.receipt.resourceId) {
    throw new Error("Agent returned an invalid procedure derivation receipt.");
  }
  return response.receipt;
}

export async function adoptProcedure(planId: string, request: { operationId: string; procedureRevisionId: string; expectedToken: number }): Promise<{ receipt: ScientificReceipt; token: number }> {
  const response = parse<{ receipt?: ScientificReceipt; token?: number }>(await invoke<string>("scientific_adopt_procedure", { planId, request: JSON.stringify(request) }));
  if (!response.receipt || response.receipt.operationId !== request.operationId || response.receipt.resourceId !== planId || typeof response.token !== "number") {
    throw new Error("Agent returned an invalid procedure adoption receipt.");
  }
  return { receipt: response.receipt, token: response.token };
}
