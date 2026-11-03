import { BaseDirectory, exists, mkdir, readDir, readTextFile, remove, writeTextFile } from '@tauri-apps/plugin-fs';

export const PROPOSAL_SCHEMA_VERSION = 1;
const PROPOSAL_DIRECTORY = 'scientific-workflow-proposals';

export interface ProposalNode {
  id: string;
  label: string;
  kind: 'step' | 'condition' | 'parallel';
}

export interface ProposalEdge {
  id: string;
  source: string;
  target: string;
}

export interface ProposalContent {
  title: string;
  nodes: ProposalNode[];
  edges: ProposalEdge[];
}

export interface ScientificWorkflowProposal {
  proposalId: string;
  schemaVersion: number;
  contentRevision: number;
  content: ProposalContent;
  sourceReferences?: Array<{ kind: 'revision' | 'route' | 'procedure'; value: string }>;
  updatedAt: string;
}

export interface ProposalAcknowledgement {
  proposalId: string;
  schemaVersion: number;
  contentRevision: number;
  content: ProposalContent;
  document: ScientificWorkflowProposal;
  idempotent: boolean;
}

export interface ProposalInventoryIssue {
  fileName: string;
  message: string;
}

export interface ProposalInventory {
  proposals: ScientificWorkflowProposal[];
  issues: ProposalInventoryIssue[];
}

export interface ProposalStorePort {
  listProposals(): Promise<ProposalInventory>;
  loadProposal(proposalId: string): Promise<ScientificWorkflowProposal>;
  saveProposal(proposal: ScientificWorkflowProposal): Promise<ProposalAcknowledgement>;
  discardProposal(proposalId: string): Promise<void>;
}

export type ProposalStoreErrorCode = 'invalid-id' | 'read-failed' | 'malformed' | 'unsupported-schema' | 'conflict' | 'write-failed' | 'acknowledgement-mismatch' | 'delete-failed';

export class ProposalStoreError extends Error {
  constructor(readonly code: ProposalStoreErrorCode, message: string, options?: ErrorOptions) {
    super(message, options);
    this.name = 'ProposalStoreError';
  }
}

/** Filesystem boundary kept injectable so independent store sessions can test durable read-back. */
export interface ProposalAppDataAdapter {
  read(path: string): Promise<string>;
  write(path: string, contents: string): Promise<void>;
  list(path: string): Promise<Array<{ name: string | null }>>;
  ensureDirectory(path: string): Promise<void>;
  remove(path: string): Promise<void>;
  exists(path: string): Promise<boolean>;
}

const tauriAppDataAdapter: ProposalAppDataAdapter = {
  read: (path) => readTextFile(path, { baseDir: BaseDirectory.AppData }),
  write: (path, contents) => writeTextFile(path, contents, { baseDir: BaseDirectory.AppData }),
  list: (path) => readDir(path, { baseDir: BaseDirectory.AppData }),
  ensureDirectory: (path) => mkdir(path, { baseDir: BaseDirectory.AppData, recursive: true }),
  remove: (path) => remove(path, { baseDir: BaseDirectory.AppData }),
  exists: (path) => exists(path, { baseDir: BaseDirectory.AppData }),
};

export function createEmptyProposal(): ScientificWorkflowProposal {
  return {
    proposalId: crypto.randomUUID(),
    schemaVersion: PROPOSAL_SCHEMA_VERSION,
    contentRevision: 1,
    content: { title: 'Untitled proposal', nodes: [], edges: [] },
    updatedAt: new Date().toISOString(),
  };
}

export function cloneProposal(proposal: ScientificWorkflowProposal): ScientificWorkflowProposal {
  return JSON.parse(JSON.stringify(proposal)) as ScientificWorkflowProposal;
}

export function canonicalProposalContent(content: ProposalContent): string {
  return JSON.stringify(sortJsonKeys(content));
}

function sortJsonKeys(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(sortJsonKeys);
  if (value !== null && typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value).sort(([left], [right]) => left.localeCompare(right)).map(([key, nested]) => [key, sortJsonKeys(nested)])
    );
  }
  return value;
}

function proposalPath(proposalId: string): string {
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(proposalId)) {
    throw new ProposalStoreError('invalid-id', 'The local proposal ID is invalid.');
  }
  return `${PROPOSAL_DIRECTORY}/${proposalId}.json`;
}

function isContent(value: unknown): value is ProposalContent {
  if (!isPlainObject(value) || !hasExactKeys(value, ['title', 'nodes', 'edges'])) return false;
  const content = value as Partial<ProposalContent>;
  return typeof content.title === 'string' && Array.isArray(content.nodes) && content.nodes.every((node) =>
    isPlainObject(node) && hasExactKeys(node, ['id', 'label', 'kind']) && typeof node.id === 'string' &&
    typeof node.label === 'string' && ['step', 'condition', 'parallel'].includes(node.kind)
  ) && Array.isArray(content.edges) && content.edges.every((edge) =>
    isPlainObject(edge) && hasExactKeys(edge, ['id', 'source', 'target']) && typeof edge.id === 'string' &&
    typeof edge.source === 'string' && typeof edge.target === 'string'
  );
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function hasExactKeys(value: Record<string, unknown>, allowed: readonly string[], required = allowed): boolean {
  const keys = Object.keys(value);
  return keys.every((key) => allowed.includes(key)) && required.every((key) => Object.hasOwn(value, key));
}

function hasValidSourceReferences(value: unknown): boolean {
  return value === undefined || (Array.isArray(value) && value.every((reference) =>
    isPlainObject(reference) && hasExactKeys(reference, ['kind', 'value']) &&
    ['revision', 'route', 'procedure'].includes(String(reference.kind)) && typeof reference.value === 'string'
  ));
}

function parseProposal(text: string, expectedId?: string): ScientificWorkflowProposal {
  let parsed: unknown;
  try {
    parsed = JSON.parse(text);
  } catch (error) {
    throw new ProposalStoreError('malformed', 'This proposal file is malformed and was preserved for manual recovery.', { cause: error });
  }

  if (!isPlainObject(parsed)) {
    throw new ProposalStoreError('malformed', 'This proposal file is malformed and was preserved for manual recovery.');
  }
  const candidate = parsed as Partial<ScientificWorkflowProposal>;
  if (candidate.schemaVersion !== PROPOSAL_SCHEMA_VERSION) {
    throw new ProposalStoreError('unsupported-schema', 'This proposal uses an unsupported schema version and was preserved for manual recovery.');
  }
  if (!hasExactKeys(parsed, ['proposalId', 'schemaVersion', 'contentRevision', 'content', 'sourceReferences', 'updatedAt'],
    ['proposalId', 'schemaVersion', 'contentRevision', 'content', 'updatedAt']) ||
    typeof candidate.proposalId !== 'string' || (expectedId && candidate.proposalId !== expectedId) ||
    !Number.isInteger(candidate.contentRevision) || candidate.contentRevision! < 1 || !isContent(candidate.content) ||
    typeof candidate.updatedAt !== 'string' || !hasValidSourceReferences(candidate.sourceReferences)) {
    throw new ProposalStoreError('malformed', 'This proposal file is malformed and was preserved for manual recovery.');
  }
  return candidate as ScientificWorkflowProposal;
}

function assertExact(actual: ScientificWorkflowProposal, expected: ScientificWorkflowProposal): void {
  if (actual.proposalId !== expected.proposalId || actual.schemaVersion !== expected.schemaVersion ||
    actual.contentRevision !== expected.contentRevision ||
    canonicalProposalContent(actual.content) !== canonicalProposalContent(expected.content)) {
    throw new ProposalStoreError('acknowledgement-mismatch', 'Saved proposal read-back did not exactly match the submitted proposal.');
  }
}

function createAcknowledgement(document: ScientificWorkflowProposal, idempotent: boolean): ProposalAcknowledgement {
  return {
    proposalId: document.proposalId,
    schemaVersion: document.schemaVersion,
    contentRevision: document.contentRevision,
    content: cloneProposal(document).content,
    document: cloneProposal(document),
    idempotent,
  };
}

export function createScientificWorkflowProposalStore(adapter: ProposalAppDataAdapter) {
  function serialize<T>(proposalId: string, operation: () => Promise<T>): Promise<T> {
    const prior = proposalWriteQueues.get(proposalId) ?? Promise.resolve();
    const current = prior.catch(() => undefined).then(operation);
    proposalWriteQueues.set(proposalId, current);
    return current.finally(() => {
      if (proposalWriteQueues.get(proposalId) === current) proposalWriteQueues.delete(proposalId);
    });
  }

  return {
    async listProposals(): Promise<ProposalInventory> {
      try {
        await adapter.ensureDirectory(PROPOSAL_DIRECTORY);
        const entries = await adapter.list(PROPOSAL_DIRECTORY);
        const proposals: ScientificWorkflowProposal[] = [];
        const issues: ProposalInventoryIssue[] = [];
        for (const entry of entries) {
          if (!entry.name?.endsWith('.json')) continue;
          try {
            const text = await adapter.read(`${PROPOSAL_DIRECTORY}/${entry.name}`);
            proposals.push(parseProposal(text));
          } catch (error) {
            const message = error instanceof ProposalStoreError ? error.message : 'Proposal could not be read and was preserved.';
            issues.push({ fileName: entry.name, message });
          }
        }
        return { proposals, issues };
      } catch (error) {
        throw new ProposalStoreError('read-failed', 'Saved proposal inventory could not be read.', { cause: error });
      }
    },

    async loadProposal(proposalId: string): Promise<ScientificWorkflowProposal> {
      const path = proposalPath(proposalId);
      let text: string;
      try {
        text = await adapter.read(path);
      } catch (error) {
        throw new ProposalStoreError('read-failed', 'The selected local proposal could not be read.', { cause: error });
      }
      return parseProposal(text, proposalId);
    },

    async saveProposal(submitted: ScientificWorkflowProposal): Promise<ProposalAcknowledgement> {
      const proposal = cloneProposal(submitted);
      const path = proposalPath(proposal.proposalId);
      parseProposal(JSON.stringify(proposal), proposal.proposalId);
      return serialize(proposal.proposalId, async () => {
        try {
          await adapter.ensureDirectory(PROPOSAL_DIRECTORY);
          let current: ScientificWorkflowProposal | null = null;
          if (await adapter.exists(path)) {
            const storedText = await adapter.read(path);
            current = parseProposal(storedText, proposal.proposalId);
            if (current.contentRevision > proposal.contentRevision) {
              throw new ProposalStoreError('conflict', 'A newer proposal revision is already stored; the stale write was not applied.');
            }
            if (current.contentRevision === proposal.contentRevision) {
              if (canonicalProposalContent(current.content) !== canonicalProposalContent(proposal.content)) {
                throw new ProposalStoreError('conflict', 'The stored proposal has different content at this revision; the write was not applied.');
              }
              return createAcknowledgement(current, true);
            }
          }

          await adapter.write(path, JSON.stringify(proposal, null, 2));
          const readBack = parseProposal(await adapter.read(path), proposal.proposalId);
          assertExact(readBack, proposal);
          return createAcknowledgement(readBack, false);
        } catch (error) {
          if (error instanceof ProposalStoreError) throw error;
          throw new ProposalStoreError('write-failed', 'Local proposal could not be saved and acknowledged.', { cause: error });
        }
      });
    },

    async discardProposal(proposalId: string): Promise<void> {
      const path = proposalPath(proposalId);
      return serialize(proposalId, async () => {
        try {
          if (await adapter.exists(path)) await adapter.remove(path);
          if (await adapter.exists(path)) {
            throw new ProposalStoreError('delete-failed', 'Proposal deletion could not be confirmed; the proposal was retained in memory.');
          }
        } catch (error) {
          if (error instanceof ProposalStoreError) throw error;
          throw new ProposalStoreError('delete-failed', 'Proposal deletion could not be confirmed; the proposal was retained in memory.', { cause: error });
        }
      });
    },
  };
}

// All store sessions in this renderer share one serialized queue per local proposal identity.
const proposalWriteQueues = new Map<string, Promise<unknown>>();

/** The renderer's single runtime writer/queue for current-user AppData proposals. */
export const scientificWorkflowProposalStore = createScientificWorkflowProposalStore(tauriAppDataAdapter);
