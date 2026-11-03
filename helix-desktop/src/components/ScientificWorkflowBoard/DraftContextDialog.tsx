import { useEffect, useRef } from 'react';
import type { ContextChangeTarget } from '../../hooks/useDraftContextResolver';

interface DraftContextDialogProps {
  target: ContextChangeTarget;
  status: string;
  error: string | null;
  onSave: () => void;
  onDiscard: () => void;
  onKeepEditing: () => void;
}

export function DraftContextDialog({ target, status, error, onSave, onDiscard, onKeepEditing }: DraftContextDialogProps) {
  const saveButton = useRef<HTMLButtonElement>(null);
  useEffect(() => { saveButton.current?.focus(); }, []);

  return (
    <div role="dialog" aria-modal="true" aria-labelledby="draft-context-dialog-title" className="draft-context-dialog">
      <h2 id="draft-context-dialog-title">Unsaved proposal changes</h2>
      <p>Choose what to do before switching to {target.kind}: {target.value}.</p>
      <p>Unsaved changes are held in memory and may be lost if Helix closes.</p>
      <div role="group" aria-label="Resolve unsaved proposal changes">
        <button ref={saveButton} type="button" onClick={onSave}>Save proposal and switch</button>
        <button type="button" onClick={onDiscard}>Discard proposal and switch</button>
        <button type="button" onClick={onKeepEditing}>Keep editing</button>
      </div>
      <p role="status" aria-live="polite">{status}</p>
      {error && <p role="alert">{error}</p>}
    </div>
  );
}
