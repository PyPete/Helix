import { useRef, useState, type ReactNode } from "react";
import "./scientific.css";

export interface ScientificRecordUiProps {
  entityLabel: string;
  entityId?: string;
  state: "Draft" | "Published" | "Frozen" | "Confirmed" | "Pending acknowledgement" | "Uncertain";
  children?: ReactNode;
}

/** Shared visual vocabulary for Agent-backed records. It makes no authority claim. */
export function ScientificRecordUi({ entityLabel, entityId, state, children }: ScientificRecordUiProps) {
  const [fullIdOpen, setFullIdOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  function closeFullId() {
    setFullIdOpen(false);
    triggerRef.current?.focus();
  }

  return <section className="scientific-record" aria-label={`${entityLabel} evidence`}>
    <h2>{entityLabel}</h2>
    <p role="status">{state}</p>
    {entityId && <div className="scientific-evidence-id"><code title={entityId}>{entityId}</code><button ref={triggerRef} type="button" onClick={() => setFullIdOpen(true)}>View full ID</button></div>}
    {children}
    {fullIdOpen && <div role="dialog" aria-modal="true" aria-label={`${entityLabel} full ID`} className="scientific-dialog"><p><code>{entityId}</code></p><button type="button" onClick={() => void navigator.clipboard?.writeText(entityId ?? "")}>Copy ID</button><button type="button" onClick={closeFullId}>Close</button></div>}
  </section>;
}
