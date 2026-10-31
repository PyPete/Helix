import { invoke } from "@tauri-apps/api/core";

export interface ApparatusDraft {
  name: string;
  description?: string;
  operationId: string;
}

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
