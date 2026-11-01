export type ScientificKind = "procedure" | "plan";

export interface ScientificReceipt {
  operationId: string;
  kind: ScientificKind;
  resourceId: string;
  revisionId?: string;
  actor?: string;
  recordedAt?: string;
  contentHash?: string;
  status: "Committed" | "Rejected" | "Conflict";
}

export interface ProcedureDraft {
  procedureId?: string;
  name: string;
  operationId: string;
  expectedToken?: number;
  wsl: { nodes: Array<{ id: string; parameters?: Record<string, unknown> }>; edges: unknown[] };
  declarations: Array<{ name: string; unit: string; targets: Array<{ nodeId: string; parameter: string }> }>;
  plannedCollections: Array<{ nodeId: string; label: string }>;
}

export interface PlanDraft {
  planId?: string;
  name: string;
  operationId: string;
  expectedToken?: number;
  procedureRevisionId: string;
  values: Array<{ name: string; value: string; unit: string }>;
}

export interface ScientificSummary {
  resourceId: string;
  name: string;
  revisionId?: string;
  status: "Draft" | "Published";
  revisionName?: string;
}

export interface ScientificDraftState<T> { content: T; token: number; }

export interface ScientificRevision {
  revisionId: string;
  name: string;
  sourceRevisionId?: string;
  actor: string;
  recordedAt: string;
  contentHash: string;
}

export type SampleOrigin = "Collection" | "Transformation";
export type SampleEventKind = "Confirmed" | "Relabeled" | "CustodyMoved" | "Correction";

export interface SampleIdentity {
  sampleId: string;
  origin: SampleOrigin;
  parentSampleIds: string[];
}

export interface SampleProjection {
  identity: SampleIdentity;
  label?: string;
  custodian?: string;
  location: string;
  serverSequence: number;
  lastEventId: string;
}

export interface SampleEvent {
  eventId: string;
  sampleId: string;
  kind: SampleEventKind;
  label?: string;
  custodian?: string;
  location: string;
  occurredAtUtc: string;
  recordedAtUtc: string;
  actorSid: string;
  serverSequence: number;
  supersedesEventId?: string;
  correctionReason?: string;
}

export interface SampleReceipt {
  operationId: string;
  kind: "sample.confirm" | "sample.append" | "sample.transform";
  sampleId: string;
  actorSid: string;
  recordedAtUtc: string;
  payload: string;
}

export interface SampleConfirmationRequest {
  operationId: string;
  location: string;
  occurredAtUtc: string;
  label?: string;
  custodian?: string;
}

export interface SampleEventAppendRequest {
  operationId: string;
  kind: Exclude<SampleEventKind, "Confirmed">;
  label?: string;
  custodian?: string;
  location: string;
  occurredAtUtc: string;
  expectedSequence: number;
  supersedesEventId?: string;
  correctionReason?: string;
}

export interface SampleMutationResult {
  sample: SampleProjection;
  receipt: SampleReceipt;
}

export type SampleTransformationKind = "Split" | "Pool" | "Processing";
export type SampleLineageDirection = "Ancestors" | "Descendants";

export interface SampleQuantity {
  value: string;
  unitCode: string;
  dimension: string;
}

/** New child IDs are supplied up front and remain immutable evidence after acknowledgement. */
export interface SampleTransformationRequest {
  operationId: string;
  transformationId: string;
  kind: SampleTransformationKind;
  operation: string;
  parentSampleIds: string[];
  childSampleIds: string[];
  quantity?: SampleQuantity;
  quantitySupplied: boolean;
}

export interface SampleTransformationResult {
  operationId: string;
  childIds: string[];
}

export interface SampleLineageEntry {
  sampleId: string;
  origin: SampleOrigin;
  transformationId?: string;
  depth: number;
}

export interface SampleLineage {
  entries: SampleLineageEntry[];
  continuation?: string;
  isTruncated: boolean;
}
