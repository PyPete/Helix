import { useCallback, useRef, useState } from 'react';
import {
  canonicalProposalContent,
  cloneProposal,
  createEmptyProposal,
  scientificWorkflowProposalStore,
  type ProposalAcknowledgement,
  type ProposalContent,
  type ScientificWorkflowProposal,
  type ProposalStorePort,
} from '../lib/scientificWorkflowProposalStore';

function sameBaseline(current: ScientificWorkflowProposal | null, baseline: ScientificWorkflowProposal | null): boolean {
  return current !== null && baseline !== null && current.proposalId === baseline.proposalId &&
    current.schemaVersion === baseline.schemaVersion && current.contentRevision === baseline.contentRevision &&
    canonicalProposalContent(current.content) === canonicalProposalContent(baseline.content);
}

export function useProposalEditability(store: ProposalStorePort = scientificWorkflowProposalStore) {
  const [isEditing, setIsEditing] = useState(false);
  const [proposal, setProposal] = useState<ScientificWorkflowProposal | null>(null);
  const [acknowledged, setAcknowledged] = useState<ScientificWorkflowProposal | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState('');
  const proposalRef = useRef(proposal);
  proposalRef.current = proposal;

  const isDirty = proposal !== null && !sameBaseline(proposal, acknowledged);

  const beginCreate = useCallback(() => {
    setProposal(createEmptyProposal());
    setAcknowledged(null);
    setError(null);
    setStatus('New local proposal is being edited in memory.');
    setIsEditing(true);
  }, []);

  const beginEdit = useCallback(() => {
    if (!proposal) return;
    setError(null);
    setStatus('Local proposal editing enabled.');
    setIsEditing(true);
  }, [proposal]);

  const resumeProposal = useCallback(async (proposalId: string) => {
    setError(null);
    setStatus('Loading the selected local proposal.');
    try {
      const loaded = await store.loadProposal(proposalId);
      setProposal(cloneProposal(loaded));
      setAcknowledged(cloneProposal(loaded));
      setIsEditing(false);
      setStatus('Selected local proposal loaded. Choose Edit Proposal to change it.');
      return loaded;
    } catch (cause) {
      const message = cause instanceof Error ? cause.message : 'Selected local proposal could not be loaded.';
      setError(message);
      setStatus('Local proposal could not be loaded.');
      return null;
    }
  }, [store]);

  const updateContent = useCallback((update: (content: ProposalContent) => ProposalContent) => {
    if (!isEditing || !proposal) return;
    const nextContent = update(cloneProposal(proposal).content);
    if (canonicalProposalContent(nextContent) === canonicalProposalContent(proposal.content)) return;
    setProposal({
      ...proposal,
      content: nextContent,
      contentRevision: proposal.contentRevision + 1,
      updatedAt: new Date().toISOString(),
    });
    setStatus('Proposal changes are held in memory.');
  }, [isEditing, proposal]);

  const saveProposal = useCallback(async (requireCurrentSnapshot = false) => {
    if (!proposal) return null;
    const submitted = cloneProposal(proposal);
    setError(null);
    setStatus('Saving local proposal and checking exact read-back.');
    try {
      const acknowledgement = await store.saveProposal(submitted);
      if (acknowledgement.proposalId !== submitted.proposalId || acknowledgement.schemaVersion !== submitted.schemaVersion ||
        acknowledgement.contentRevision !== submitted.contentRevision ||
        canonicalProposalContent(acknowledgement.content) !== canonicalProposalContent(submitted.content)) {
        throw new Error('Local proposal save acknowledgement did not match the submitted content.');
      }
      const saved = cloneProposal(acknowledgement.document);
      setAcknowledged(saved);
      const latest = proposalRef.current;
      const stillAtSubmittedSnapshot = sameBaseline(latest, submitted);
      if (stillAtSubmittedSnapshot) setIsEditing(false);
      setStatus(stillAtSubmittedSnapshot ? 'Local proposal saved and read back exactly.' : 'Earlier revision saved; newer in-memory edits remain unsaved.');
      if (requireCurrentSnapshot && !stillAtSubmittedSnapshot) return null;
      return acknowledgement;
    } catch (cause) {
      const message = cause instanceof Error ? cause.message : 'Local proposal could not be saved.';
      setError(message);
      setStatus('Local proposal save failed. Unsaved changes remain in memory.');
      return null;
    }
  }, [proposal, store]);

  const discardProposal = useCallback(async () => {
    if (!proposal) return false;
    setError(null);
    setStatus('Confirming removal of this local proposal.');
    try {
      await store.discardProposal(proposal.proposalId);
      setProposal(null);
      setAcknowledged(null);
      setIsEditing(false);
      setStatus('Local proposal deletion confirmed.');
      return true;
    } catch (cause) {
      const message = cause instanceof Error ? cause.message : 'Local proposal deletion could not be confirmed.';
      setError(message);
      setStatus('Local proposal deletion failed. Proposal data remains in memory.');
      return false;
    }
  }, [proposal, store]);

  const acceptAcknowledgedProposal = useCallback((loaded: ScientificWorkflowProposal) => {
    setProposal(cloneProposal(loaded));
    setAcknowledged(cloneProposal(loaded));
    setIsEditing(false);
    setError(null);
    setStatus('Selected local proposal loaded. Choose Edit Proposal to change it.');
  }, []);

  return {
    isEditing,
    proposal,
    isDirty,
    error,
    status,
    beginCreate,
    beginEdit,
    resumeProposal,
    acceptAcknowledgedProposal,
    updateContent,
    saveProposal,
    discardProposal,
  };
}
