import { act, renderHook } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { ProposalAcknowledgement, ScientificWorkflowProposal } from '../lib/scientificWorkflowProposalStore';
import { useProposalEditability } from './useProposalEditability';

describe('useProposalEditability', () => {
  it('requires explicit local intent and does not depend on Agent health or session state', () => {
    const { result } = renderHook(() => useProposalEditability());

    expect(result.current.isEditing).toBe(false);
    expect(result.current.proposal).toBeNull();
    act(() => result.current.beginCreate());
    expect(result.current.isEditing).toBe(true);
    expect(result.current.proposal?.contentRevision).toBe(1);
    expect(result.current.proposal?.content.title).toBe('Untitled proposal');
  });

  it('increments one content revision per distinct proposal change and keeps no-op edits clean', () => {
    const { result } = renderHook(() => useProposalEditability());

    act(() => result.current.beginCreate());
    const initial = result.current.proposal!;
    act(() => result.current.updateContent((content) => ({ ...content, title: 'Changed proposal' })));
    expect(result.current.proposal?.contentRevision).toBe(initial.contentRevision + 1);
    act(() => result.current.updateContent((content) => ({ ...content, title: 'Changed proposal' })));
    expect(result.current.proposal?.contentRevision).toBe(initial.contentRevision + 1);
  });

  it('keeps a later edit dirty when an older save acknowledgement resolves', async () => {
    let finishSave!: (acknowledgement: ProposalAcknowledgement) => void;
    const store = {
      saveProposal: vi.fn(() => new Promise<ProposalAcknowledgement>((resolve) => { finishSave = resolve; })),
      loadProposal: vi.fn(),
      discardProposal: vi.fn(),
      listProposals: vi.fn(async () => ({ proposals: [], issues: [] })),
    };
    const { result } = renderHook(() => useProposalEditability(store));
    act(() => result.current.beginCreate());
    act(() => result.current.updateContent((content) => ({ ...content, title: 'Revision two' })));
    const submitted = result.current.proposal!;
    let pending!: Promise<unknown>;
    act(() => { pending = result.current.saveProposal(true); });
    act(() => result.current.updateContent((content) => ({ ...content, title: 'Revision three' })));
    finishSave({
      proposalId: submitted.proposalId,
      schemaVersion: submitted.schemaVersion,
      contentRevision: submitted.contentRevision,
      content: submitted.content,
      document: submitted,
      idempotent: false,
    });
    await act(async () => { expect(await pending).toBeNull(); });

    expect(result.current.proposal?.contentRevision).toBe(submitted.contentRevision + 1);
    expect(result.current.isDirty).toBe(true);
  });

  it('rejects an acknowledgement whose document differs from the acknowledged snapshot', async () => {
    const saveProposal = vi.fn(async (submitted: ScientificWorkflowProposal) => ({
      proposalId: submitted.proposalId,
      schemaVersion: submitted.schemaVersion,
      contentRevision: submitted.contentRevision,
      content: submitted.content,
      document: { ...submitted, content: { ...submitted.content, title: 'Different persisted content' } },
      idempotent: false,
    }));
    const store = {
      saveProposal,
      loadProposal: vi.fn(),
      discardProposal: vi.fn(),
      listProposals: vi.fn(async () => ({ proposals: [], issues: [] })),
    };
    const { result } = renderHook(() => useProposalEditability(store));
    act(() => result.current.beginCreate());
    act(() => result.current.updateContent((content) => ({ ...content, title: 'Submitted content' })));

    let acknowledgement!: Promise<unknown>;
    act(() => { acknowledgement = result.current.saveProposal(); });
    await act(async () => { await acknowledgement; });

    expect(result.current.isDirty).toBe(true);
    expect(result.current.error).toContain('acknowledgement did not match');
    expect(result.current.isEditing).toBe(true);
  });
});
