import { describe, expect, it, vi } from 'vitest';
import {
  createEmptyProposal,
  createScientificWorkflowProposalStore,
  type ProposalAppDataAdapter,
  type ScientificWorkflowProposal,
} from './scientificWorkflowProposalStore';

function createMemoryAdapter(files = new Map<string, string>()) {
  const adapter: ProposalAppDataAdapter = {
    read: async (path) => {
      const value = files.get(path);
      if (value === undefined) throw new Error('file not found');
      return value;
    },
    write: async (path, contents) => { files.set(path, contents); },
    list: async (path) => [...files.keys()].filter((key) => key.startsWith(`${path}/`)).map((key) => ({ name: key.slice(path.length + 1) })),
    ensureDirectory: async () => undefined,
    remove: async (path) => { files.delete(path); },
    exists: async (path) => files.has(path),
  };
  return { adapter, files };
}

function revise(proposal: ScientificWorkflowProposal, contentRevision: number, title: string): ScientificWorkflowProposal {
  return { ...proposal, contentRevision, content: { ...proposal.content, title } };
}

describe('scientificWorkflowProposalStore', () => {
  it('uses independent store sessions to read the exact serialized AppData proposal after restart', async () => {
    const files = new Map<string, string>();
    const firstSession = createScientificWorkflowProposalStore(createMemoryAdapter(files).adapter);
    const secondSession = createScientificWorkflowProposalStore(createMemoryAdapter(files).adapter);
    const submitted = createEmptyProposal();
    submitted.content = { ...submitted.content, title: 'Persisted local proposal' };

    await firstSession.saveProposal(submitted);
    const loaded = await secondSession.loadProposal(submitted.proposalId);

    expect(loaded).toMatchObject({
      proposalId: submitted.proposalId,
      schemaVersion: submitted.schemaVersion,
      contentRevision: submitted.contentRevision,
      content: submitted.content,
    });
  });

  it('rejects a delayed stale write and preserves the newer stored bytes', async () => {
    const { adapter, files } = createMemoryAdapter();
    const store = createScientificWorkflowProposalStore(adapter);
    const original = createEmptyProposal();
    const newer = revise(original, 2, 'Newer snapshot');
    await store.saveProposal(newer);
    const path = `scientific-workflow-proposals/${original.proposalId}.json`;
    const bytes = files.get(path);

    await expect(store.saveProposal(original)).rejects.toMatchObject({ code: 'conflict' });
    expect(files.get(path)).toBe(bytes);
  });

  it('rejects different content at the same revision without changing stored bytes', async () => {
    const { adapter, files } = createMemoryAdapter();
    const store = createScientificWorkflowProposalStore(adapter);
    const original = createEmptyProposal();
    await store.saveProposal(original);
    const path = `scientific-workflow-proposals/${original.proposalId}.json`;
    const bytes = files.get(path);

    await expect(store.saveProposal(revise(original, original.contentRevision, 'Conflicting content')))
      .rejects.toMatchObject({ code: 'conflict' });
    expect(files.get(path)).toBe(bytes);
  });

  it('treats an exact same-revision retry as idempotent without a second write', async () => {
    const { adapter } = createMemoryAdapter();
    const write = vi.spyOn(adapter, 'write');
    const store = createScientificWorkflowProposalStore(adapter);
    const proposal = createEmptyProposal();

    const first = await store.saveProposal(proposal);
    const retry = await store.saveProposal(proposal);

    expect(first.idempotent).toBe(false);
    expect(retry.idempotent).toBe(true);
    expect(write).toHaveBeenCalledOnce();
  });

  it('preserves malformed and unsupported proposal files and surfaces them for manual recovery', async () => {
    const { adapter, files } = createMemoryAdapter();
    const store = createScientificWorkflowProposalStore(adapter);
    const malformedId = createEmptyProposal().proposalId;
    const unsupportedId = createEmptyProposal().proposalId;
    const malformedPath = `scientific-workflow-proposals/${malformedId}.json`;
    const unsupportedPath = `scientific-workflow-proposals/${unsupportedId}.json`;
    files.set(malformedPath, '{not valid JSON');
    files.set(unsupportedPath, JSON.stringify({ ...createEmptyProposal(), proposalId: unsupportedId, schemaVersion: 99 }));

    const inventory = await store.listProposals();

    expect(inventory.issues).toHaveLength(2);
    expect(inventory.issues.map((issue) => issue.message).join(' ')).toMatch(/malformed|unsupported schema/i);
    expect(files.get(malformedPath)).toBe('{not valid JSON');
    expect(files.get(unsupportedPath)).toContain('"schemaVersion":99');
    await expect(store.loadProposal(malformedId)).rejects.toMatchObject({ code: 'malformed' });
    await expect(store.loadProposal(unsupportedId)).rejects.toMatchObject({ code: 'unsupported-schema' });
  });

  it('fails closed on unrecognized schema fields without modifying the source bytes', async () => {
    const { adapter, files } = createMemoryAdapter();
    const store = createScientificWorkflowProposalStore(adapter);
    const proposal = createEmptyProposal();
    const raw = JSON.stringify({ ...proposal, approvalStatus: 'approved' });
    const path = `scientific-workflow-proposals/${proposal.proposalId}.json`;
    files.set(path, raw);

    const inventory = await store.listProposals();
    expect(inventory.proposals.length).toBe(0);
    expect(inventory.issues).toHaveLength(1);
    expect(files.get(path)).toBe(raw);
  });

  it('deletes only the scoped proposal file and confirms its absence', async () => {
    const { adapter, files } = createMemoryAdapter();
    const store = createScientificWorkflowProposalStore(adapter);
    const proposal = createEmptyProposal();
    const other = createEmptyProposal();
    await store.saveProposal(proposal);
    await store.saveProposal(other);

    await store.discardProposal(proposal.proposalId);

    expect(files.has(`scientific-workflow-proposals/${proposal.proposalId}.json`)).toBe(false);
    expect(files.has(`scientific-workflow-proposals/${other.proposalId}.json`)).toBe(true);
  });
});
