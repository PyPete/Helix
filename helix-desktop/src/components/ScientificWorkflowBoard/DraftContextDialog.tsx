import { useEffect, useRef, type KeyboardEvent } from 'react';
import type { ContextChangeTarget } from '../../hooks/useDraftContextResolver';

interface DraftContextDialogProps {
  target: ContextChangeTarget;
  status: string;
  error: string | null;
  busy?: boolean;
  onSave: () => void;
  onDiscard: () => void;
  onKeepEditing: () => void;
}

export function DraftContextDialog({ target, status, error, busy = false, onSave, onDiscard, onKeepEditing }: DraftContextDialogProps) {
  const saveButton = useRef<HTMLButtonElement>(null);
  const keepEditingButton = useRef<HTMLButtonElement>(null);
  useEffect(() => { saveButton.current?.focus(); }, []);

  function keepTabInsideDialog(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape' && !busy) {
      event.preventDefault();
      onKeepEditing();
      return;
    }
    if (event.key !== 'Tab') return;
    if (event.shiftKey && document.activeElement === saveButton.current) {
      event.preventDefault();
      keepEditingButton.current?.focus();
    } else if (!event.shiftKey && document.activeElement === keepEditingButton.current) {
      event.preventDefault();
      saveButton.current?.focus();
    }
  }

  return (
    <div role="dialog" aria-modal="true" aria-labelledby="draft-context-dialog-title" className="draft-context-dialog" onKeyDown={keepTabInsideDialog}>
      <h2 id="draft-context-dialog-title">Unsaved proposal changes</h2>
      <p>Choose what to do before switching to {target.kind}: {target.value}.</p>
      <p>Unsaved changes are held in memory and may be lost if Helix closes.</p>
      <div role="group" aria-label="Resolve unsaved proposal changes">
        <button ref={saveButton} type="button" disabled={busy} onClick={onSave}>Save proposal and switch</button>
        <button type="button" disabled={busy} onClick={onDiscard}>Discard proposal and switch</button>
        <button ref={keepEditingButton} type="button" disabled={busy} onClick={onKeepEditing}>Keep editing</button>
      </div>
      <p role="status" aria-live="polite">{status}</p>
      {error && <p role="alert">{error}</p>}
    </div>
  );
}
