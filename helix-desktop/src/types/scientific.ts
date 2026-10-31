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
