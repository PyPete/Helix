import { invoke } from "@tauri-apps/api/core";
import type { PlanDraft, ProcedureDraft, SampleConfirmationRequest, SampleEvent, SampleEventAppendRequest, SampleMutationResult, SampleProjection, SampleReceipt, ScientificDraftState, ScientificKind, ScientificReceipt, ScientificRevision, ScientificSummary } from "../types/scientific";

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

function validProjection(value: unknown): value is SampleProjection {
  if (!value || typeof value !== "object") return false;
  const sample = value as Partial<SampleProjection>;
  return !!sample.identity && typeof sample.identity.sampleId === "string" &&
    (sample.identity.origin === "Collection" || sample.identity.origin === "Transformation") &&
    Array.isArray(sample.identity.parentSampleIds) && typeof sample.location === "string" &&
    typeof sample.serverSequence === "number" && typeof sample.lastEventId === "string";
}

function sampleReceipt(value: unknown, expectedKind: SampleReceipt["kind"], operationId?: string): SampleReceipt {
  if (!value || typeof value !== "object") throw new Error("Agent returned an invalid sample receipt.");
  const receipt = value as Partial<SampleReceipt>;
  if (receipt.kind !== expectedKind || typeof receipt.operationId !== "string" ||
    (operationId && receipt.operationId !== operationId) || typeof receipt.sampleId !== "string" ||
    typeof receipt.actorSid !== "string" || typeof receipt.recordedAtUtc !== "string" || typeof receipt.payload !== "string") {
    throw new Error("Agent returned an invalid sample receipt.");
  }
  return receipt as SampleReceipt;
}

function sampleMutation(response: string, expectedKind: SampleReceipt["kind"], operationId: string): SampleMutationResult {
  const value = parse<{ sample?: unknown; receipt?: unknown }>(response);
  if (!validProjection(value.sample)) throw new Error("Agent returned an invalid sample mutation result.");
  const receipt = sampleReceipt(value.receipt, expectedKind, operationId);
  if (receipt.sampleId !== value.sample.identity.sampleId) throw new Error("Agent returned a sample receipt for a different identity.");
  return { sample: value.sample, receipt };
}

export async function listSamples(pageSize = 25): Promise<SampleProjection[]> {
  const result = parse<unknown>(await invoke<string>("sample_list", { pageSize }));
  if (!Array.isArray(result) || !result.every(validProjection)) throw new Error("Agent returned an invalid sample list.");
  return result;
}

export async function readSample(sampleId: string): Promise<SampleProjection> {
  const result = parse<unknown>(await invoke<string>("sample_read", { sampleId }));
  if (!validProjection(result) || result.identity.sampleId !== sampleId) throw new Error("Agent returned an invalid sample projection.");
  return result;
}

export async function confirmSample(request: SampleConfirmationRequest): Promise<SampleMutationResult> {
  return sampleMutation(await invoke<string>("sample_confirm", { request: JSON.stringify(request) }), "sample.confirm", request.operationId);
}

export async function appendSampleEvent(sampleId: string, request: SampleEventAppendRequest): Promise<SampleMutationResult> {
  return sampleMutation(await invoke<string>("sample_append_event", { sampleId, request: JSON.stringify(request) }), "sample.append", request.operationId);
}

export async function sampleEvents(sampleId: string, afterSequence?: number, pageSize = 25): Promise<SampleEvent[]> {
  const result = parse<unknown>(await invoke<string>("sample_events", { sampleId, afterSequence, pageSize }));
  if (!Array.isArray(result) || !result.every((event) => {
    const value = event as Partial<SampleEvent>;
    return !!value && value.sampleId === sampleId && typeof value.eventId === "string" &&
      typeof value.kind === "string" && typeof value.location === "string" &&
      typeof value.occurredAtUtc === "string" && typeof value.recordedAtUtc === "string" &&
      typeof value.actorSid === "string" && typeof value.serverSequence === "number";
  })) throw new Error("Agent returned invalid sample history.");
  return result as SampleEvent[];
}
