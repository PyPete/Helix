import { useCallback, useEffect, useRef, useState } from 'react';

export type ContextChangeTarget =
  | { kind: 'revision'; value: string }
  | { kind: 'route'; value: string }
  | { kind: 'procedure'; value: string };

function describeTarget(target: ContextChangeTarget): string {
  return `${target.kind} ${target.value}`;
}

export function useDraftContextResolver() {
  const [context, setContext] = useState<ContextChangeTarget>({ kind: 'revision', value: 'current' });
  const [pendingTarget, setPendingTarget] = useState<ContextChangeTarget | null>(null);
  const [status, setStatus] = useState('');
  const [error, setError] = useState<string | null>(null);
  const opener = useRef<HTMLElement | null>(null);
  const hadPending = useRef(false);

  useEffect(() => {
    if (pendingTarget) {
      hadPending.current = true;
      return;
    }
    if (hadPending.current) {
      opener.current?.focus();
      opener.current = null;
      hadPending.current = false;
    }
  }, [pendingTarget]);

  const requestDraftContextChange = useCallback((target: ContextChangeTarget, isDirty: boolean, trigger: HTMLElement | null = null) => {
    if (pendingTarget) {
      setError('A context request is already pending. Resolve it before requesting another target.');
      setStatus('The existing context request was retained.');
      return false;
    }
    setError(null);
    if (isDirty) {
      opener.current = trigger;
      setPendingTarget(target);
      setStatus(`Unsaved proposal changes need a decision before switching to ${describeTarget(target)}.`);
    } else {
      setContext(target);
      setStatus(`Context changed to ${describeTarget(target)}.`);
    }
    return true;
  }, [pendingTarget]);

  const saveAndApply = useCallback(async (save: () => Promise<unknown | null>) => {
    if (!pendingTarget) return false;
    setError(null);
    setStatus('Saving the exact local proposal before changing context.');
    const acknowledgement = await save();
    if (!acknowledgement) {
      setError('The proposal was not acknowledged as saved. The original context and pending target were retained.');
      setStatus('Save failed; the proposal remains in memory.');
      return false;
    }
    const target = pendingTarget;
    setContext(target);
    setPendingTarget(null);
    setStatus(`Proposal saved. Context changed to ${describeTarget(target)}.`);
    return true;
  }, [pendingTarget]);

  const discardAndApply = useCallback(async (discard: () => Promise<boolean>) => {
    if (!pendingTarget) return false;
    setError(null);
    setStatus('Confirming deletion of this local proposal before changing context.');
    const confirmed = await discard();
    if (!confirmed) {
      setError('Proposal deletion was not confirmed. The original context and proposal data were retained.');
      setStatus('Discard failed; the proposal remains in memory.');
      return false;
    }
    const target = pendingTarget;
    setContext(target);
    setPendingTarget(null);
    setStatus(`Proposal deletion confirmed. Context changed to ${describeTarget(target)}.`);
    return true;
  }, [pendingTarget]);

  const keepEditing = useCallback(() => {
    if (!pendingTarget) return;
    setPendingTarget(null);
    setError(null);
    setStatus('Kept editing the proposal in its original context.');
  }, [pendingTarget]);

  return { context, pendingTarget, status, error, requestDraftContextChange, saveAndApply, discardAndApply, keepEditing };
}
