import { invoke } from "@tauri-apps/api/core";
import type { AssemblyDefaults, PlanDraft, PolicyRevision, ProcedureDraft, SampleConfirmationRequest, SampleEvent, SampleEventAppendRequest, SampleLineage, SampleLineageDirection, SampleMutationResult, SampleProjection, SampleReceipt, SampleTransformationRequest, SampleTransformationResult, ScientificAssembly, ScientificAssemblyRequest, ScientificDraftState, ScientificKind, ScientificReceipt, ScientificRevision, ScientificSummary } from "../types/scientific";

function parse<T>(response: string): T { return JSON.parse(response) as T; }

function scientificReceipt(value: unknown, kind: ScientificKind, operationId?: string, resourceId?: string): ScientificReceipt {
  if (!value || typeof value !== "object") {
    throw new Error("Agent returned an invalid scientific receipt.");
  }
  const receipt = value as Partial<ScientificReceipt>;
  if (receipt.kind !== kind || typeof receipt.operationId !== "string" || receipt.operationId.length === 0 ||
    typeof receipt.resourceId !== "string" || receipt.resourceId.length === 0 || receipt.status !== "Committed" ||
    (operationId !== undefined && receipt.operationId !== operationId) ||
    (resourceId !== undefined && receipt.resourceId !== resourceId)) {
    throw new Error("Agent returned an invalid scientific receipt.");
  }
  return receipt as ScientificReceipt;
}

function receipt(response: string, kind: ScientificKind, operationId?: string, resourceId?: string): ScientificReceipt {
  return scientificReceipt(parse<unknown>(response), kind, operationId, resourceId);
}

export async function listScientific(kind: ScientificKind): Promise<ScientificSummary[]> {
  return parse(await invoke<string>("scientific_list_kind", { kind }));
}

export async function createScientific(kind: ScientificKind, draft: ProcedureDraft | PlanDraft): Promise<ScientificReceipt> {
  return receipt(await invoke<string>("scientific_create_kind", { kind, request: JSON.stringify(draft) }), kind, draft.operationId);
}

export async function readScientificDraft<T>(kind: ScientificKind, resourceId: string): Promise<ScientificDraftState<T>> {
  return parse(await invoke<string>("scientific_read_draft_kind", { kind, resourceId }));
}

export async function saveScientificDraft(kind: ScientificKind, resourceId: string, draft: ProcedureDraft | PlanDraft): Promise<{ receipt: ScientificReceipt; token: number }> {
  const value = parse<{ receipt?: unknown; token?: unknown }>(await invoke<string>("scientific_save_draft_kind", { kind, resourceId, request: JSON.stringify(draft) }));
  if (!Number.isInteger(value.token) || value.token < 0) throw new Error("Agent returned an invalid scientific draft save.");
  return { receipt: scientificReceipt(value.receipt, kind, draft.operationId, resourceId), token: value.token };
}

export async function publishScientific(kind: ScientificKind, resourceId: string, request: { operationId: string; revisionName: string; expectedToken: number }): Promise<ScientificReceipt> {
  return receipt(await invoke<string>("scientific_publish_kind", { kind, resourceId, request: JSON.stringify(request) }), kind, request.operationId, resourceId);
}

export async function revisions(kind: ScientificKind, resourceId: string): Promise<ScientificRevision[]> {
  return parse(await invoke<string>("scientific_revisions_kind", { kind, resourceId }));
}

export async function readScientificOperation(kind: ScientificKind, operationId: string): Promise<ScientificReceipt> {
  return receipt(await invoke<string>("scientific_operation_result", { kind, operationId }), kind);
}

export async function deriveProcedure(request: { operationId: string; sourceRevisionId: string; name: string }): Promise<ScientificReceipt> {
  const response = parse<{ receipt?: ScientificReceipt }>(await invoke<string>("scientific_derive_procedure", { request: JSON.stringify(request) }));
  return scientificReceipt(response.receipt, "procedure", request.operationId);
}

export async function adoptProcedure(planId: string, request: { operationId: string; procedureRevisionId: string; expectedToken: number }): Promise<{ receipt: ScientificReceipt; token: number }> {
  const response = parse<{ receipt?: ScientificReceipt; token?: number }>(await invoke<string>("scientific_adopt_procedure", { planId, request: JSON.stringify(request) }));
  if (!Number.isInteger(response.token) || response.token < 0) {
    throw new Error("Agent returned an invalid procedure adoption receipt.");
  }
  return { receipt: scientificReceipt(response.receipt, "plan", request.operationId, planId), token: response.token };
}

function exactString(value: unknown): value is string { return typeof value === "string" && value.length > 0; }

function validPolicy(value: unknown): value is PolicyRevision {
  if (!value || typeof value !== "object") return false;
  const policy = value as Partial<PolicyRevision>;
  if (!exactString(policy.revisionId) || !exactString(policy.policyId) || !exactString(policy.schema) || !exactString(policy.contentHash) || !exactString(policy.recordedAtUtc)) return false;
  if (!policy.approval) return true;
  return exactString(policy.approval.approvalId) && policy.approval.revisionId === policy.revisionId &&
    policy.approval.contentHash === policy.contentHash && policy.approval.schema === policy.schema &&
    ["Approved", "Unavailable", "Unapproved"].includes(policy.approval.status);
}

function validAssembly(value: unknown, request?: ScientificAssemblyRequest): value is ScientificAssembly {
  if (!value || typeof value !== "object") return false;
  const assembly = value as Partial<ScientificAssembly>;
  if (!exactString(assembly.assemblyId) || !exactString(assembly.operationId) || !exactString(assembly.apparatusRevisionId) ||
    !exactString(assembly.procedureRevisionId) || !exactString(assembly.planRevisionId) || !exactString(assembly.policyRevisionId) ||
    !["Committed", "Unavailable", "Conflict", "Rejected"].includes(assembly.status ?? "")) return false;
  return !request || (assembly.operationId === request.operationId && assembly.apparatusRevisionId === request.apparatusRevisionId &&
    assembly.procedureRevisionId === request.procedureRevisionId && assembly.planRevisionId === request.planRevisionId &&
    assembly.policyRevisionId === request.policyRevisionId);
}

/** Reads a single consistent preview; it does not infer or mint policy approval. */
export async function assemblyDefaults(): Promise<AssemblyDefaults> {
  const value = parse<unknown>(await invoke<string>("scientific_assembly_defaults"));
  const defaults = value as Partial<AssemblyDefaults>;
  if (!exactString(defaults.apparatusRevisionId) || !exactString(defaults.procedureRevisionId) || !exactString(defaults.planRevisionId) ||
    !exactString(defaults.previewToken) || (defaults.policyRevisionId !== undefined && !exactString(defaults.policyRevisionId))) {
    throw new Error("Agent returned invalid assembly defaults.");
  }
  return defaults as AssemblyDefaults;
}

/** Preserves the caller-owned operation identity if Agent authority is unavailable or the preview drifts. */
export async function assembleScientific(request: ScientificAssemblyRequest): Promise<ScientificAssembly> {
  const value = parse<unknown>(await invoke<string>("scientific_assemble", { request: JSON.stringify(request) }));
  if (!validAssembly(value, request)) throw new Error("Agent returned an invalid scientific assembly receipt.");
  return value;
}

export async function readScientificAssembly(assemblyId: string): Promise<ScientificAssembly> {
  const value = parse<unknown>(await invoke<string>("scientific_read_assembly", { assemblyId }));
  if (!validAssembly(value) || value.assemblyId !== assemblyId) throw new Error("Agent returned an invalid scientific assembly.");
  return value;
}

export async function listScientificPolicies(): Promise<PolicyRevision[]> {
  const value = parse<unknown>(await invoke<string>("scientific_policies"));
  if (!Array.isArray(value) || !value.every(validPolicy)) throw new Error("Agent returned invalid policy content.");
  return value;
}

export async function readScientificPolicy(revisionId: string): Promise<PolicyRevision> {
  const value = parse<unknown>(await invoke<string>("scientific_read_policy", { revisionId }));
  if (!validPolicy(value) || value.revisionId !== revisionId) throw new Error("Agent returned an invalid policy revision.");
  return value;
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

/** Looks up a sample mutation receipt without replaying the mutation or generating a new key. */
export async function readSampleOperation(kind: SampleReceipt["kind"], operationId: string): Promise<SampleReceipt> {
  return sampleReceipt(await invoke<string>("sample_operation", { operationId, kind }), kind, operationId);
}

function validTransformationResult(value: unknown, request: SampleTransformationRequest): value is SampleTransformationResult {
  if (!value || typeof value !== "object") return false;
  const result = value as Partial<SampleTransformationResult>;
  return result.operationId === request.operationId && Array.isArray(result.childIds) &&
    result.childIds.length === request.childSampleIds.length &&
    result.childIds.every((childId, index) => childId === request.childSampleIds[index]);
}

function validLineage(value: unknown): value is SampleLineage {
  if (!value || typeof value !== "object") return false;
  const lineage = value as Partial<SampleLineage>;
  return Array.isArray(lineage.entries) && typeof lineage.isTruncated === "boolean" &&
    (lineage.continuation === undefined || lineage.continuation === null || typeof lineage.continuation === "string") &&
    lineage.entries.every((entry) => !!entry && typeof entry.sampleId === "string" &&
      (entry.origin === "Collection" || entry.origin === "Transformation") &&
      (entry.transformationId === undefined || typeof entry.transformationId === "string") &&
      typeof entry.depth === "number" && Number.isInteger(entry.depth) && entry.depth >= 0 &&
      typeof entry.quantitySupplied === "boolean" &&
      (entry.actorSid === undefined || typeof entry.actorSid === "string") &&
      (entry.occurredAtUtc === undefined || typeof entry.occurredAtUtc === "string") &&
      (entry.recordedAtUtc === undefined || typeof entry.recordedAtUtc === "string"));
}

/** Submits exactly one new-child transformation with its caller-owned operation identity. */
export async function transformSample(request: SampleTransformationRequest): Promise<SampleTransformationResult> {
  const result = parse<unknown>(await invoke<string>("sample_transform", { request: JSON.stringify(request) }));
  if (!validTransformationResult(result, request)) throw new Error("Agent returned an invalid sample transformation acknowledgement.");
  return result;
}

/** Reads one bounded lineage page; continuation is returned only as Agent-provided evidence. */
export async function sampleLineage(sampleId: string, direction: SampleLineageDirection, continuation?: string, pageSize = 25): Promise<SampleLineage> {
  if (!Number.isInteger(pageSize) || pageSize < 1 || pageSize > 100) throw new Error("Sample lineage page size must be between 1 and 100.");
  const result = parse<unknown>(await invoke<string>("sample_lineage", { sampleId, direction, continuation, pageSize }));
  if (!validLineage(result)) throw new Error("Agent returned invalid sample lineage evidence.");
  return result;
}
