import { invoke } from "@tauri-apps/api/core";

export interface ApparatusDraft {
  name: string;
  description?: string;
  operationId: string;
  components?: ApparatusComponent[];
  connections?: ApparatusConnection[];
  assertions?: ScientificAssertion[];
}

export interface ApparatusComponent { componentId: string; type: string; roles: string[]; discoveredDeviceId?: string; ports?: ApparatusPort[]; }
export interface ApparatusPort { portId: string; medium: "Fluid" | "Gas" | "Signal"; direction: "In" | "Out" | "Bidirectional"; cardinality: "One" | "Many"; }
export interface ApparatusConnection { connectionId: string; fromPortId: string; toPortId: string; }
export interface ScientificAssertion { assertionId: string; kind: "CapabilityConstraint" | "CalibrationRecord" | "InspectionRecord"; capability: string; parameter: string; lowerBound?: number; upperBound?: number; measuredValue?: number; unit?: string; observedAtUtc: string; validThroughUtc?: string; sourceReference: string; recordedBy: string; }
export interface ApparatusDraftState { apparatus: ApparatusDraft & { apparatusId: string }; token: number; }

export interface ApparatusPublicationDraft {
  operationId: string;
  expectedRevisionId?: string;
  note?: string;
}

export interface ScientificOperationReceipt {
  operationId: string;
  apparatusId: string;
  revisionId?: string;
  status: string;
}

export interface ScientificApparatusSummary {
  apparatusId: string;
  name: string;
  revisionId?: string;
}

function parseResponse<T>(response: string): T {
  return JSON.parse(response) as T;
}

export async function createApparatus(draft: ApparatusDraft): Promise<ScientificOperationReceipt> {
  const response = await invoke<string>("scientific_create", { request: JSON.stringify(draft) });
  return parseResponse<ScientificOperationReceipt>(response);
}

export async function readApparatusDraft(apparatusId: string): Promise<ApparatusDraftState> {
  return parseResponse<ApparatusDraftState>(await invoke<string>("scientific_read_draft", { apparatusId }));
}

export async function saveApparatusDraft(apparatusId: string, draft: ApparatusDraft & { expectedToken: number }): Promise<{ receipt: ScientificOperationReceipt; token: number }> {
  return parseResponse(await invoke<string>("scientific_save_draft", { apparatusId, request: JSON.stringify(draft) }));
}

export async function publishApparatus(
  apparatusId: string,
  draft: ApparatusPublicationDraft,
): Promise<ScientificOperationReceipt> {
  const response = await invoke<string>("scientific_publish", {
    apparatusId,
    request: JSON.stringify(draft),
  });
  return parseResponse<ScientificOperationReceipt>(response);
}

export async function listApparatus(): Promise<ScientificApparatusSummary[]> {
  return parseResponse<ScientificApparatusSummary[]>(await invoke<string>("scientific_list"));
}

export async function readApparatus(apparatusId: string): Promise<ScientificApparatusSummary> {
  return parseResponse<ScientificApparatusSummary>(await invoke<string>("scientific_read", { apparatusId }));
}

export async function readApparatusHistory(apparatusId: string): Promise<ScientificOperationReceipt[]> {
  return parseResponse<ScientificOperationReceipt[]>(await invoke<string>("scientific_history", { apparatusId }));
}

export async function readApparatusRevision(apparatusId: string, revision: string): Promise<ScientificApparatusSummary> {
  return parseResponse<ScientificApparatusSummary>(await invoke<string>("scientific_read_revision", { apparatusId, revision }));
}

export async function readScientificOperation(operationId: string): Promise<ScientificOperationReceipt> {
  return parseResponse<ScientificOperationReceipt>(await invoke<string>("scientific_operation", { operationId }));
}
