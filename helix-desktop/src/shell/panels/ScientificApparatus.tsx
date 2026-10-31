import { FormEvent, useRef, useState } from "react";
import { createApparatus, ScientificOperationReceipt } from "../../lib/scientificApparatusClient";

function newOperationId(): string {
  return crypto.randomUUID();
}

/** A durable scientific-apparatus submission panel. Retries retain the original operation ID. */
export function ScientificApparatus() {
  const [name, setName] = useState("");
  const [receipt, setReceipt] = useState<ScientificOperationReceipt>();
  const [error, setError] = useState<string>();
  const [pending, setPending] = useState(false);
  const operationId = useRef<string | undefined>(undefined);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending || !name.trim()) return;

    operationId.current ??= newOperationId();
    setPending(true);
    setError(undefined);
    try {
      const result = await createApparatus({ name: name.trim(), operationId: operationId.current });
      setReceipt(result);
      operationId.current = undefined;
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Unable to create apparatus.");
    } finally {
      setPending(false);
    }
  }

  return (
    <section aria-labelledby="scientific-apparatus-title" className="panel scientific-apparatus-panel">
      <h2 id="scientific-apparatus-title">Scientific apparatus</h2>
      <form onSubmit={submit}>
        <label htmlFor="apparatus-name">Name</label>
        <input id="apparatus-name" value={name} onChange={(event) => setName(event.target.value)} disabled={pending} required />
        <button type="submit" disabled={pending || !name.trim()}>{pending ? "Creating…" : "Create apparatus"}</button>
      </form>
      {error && <p role="alert">{error} Retry uses operation {operationId.current}.</p>}
      {receipt && <p role="status">Created {receipt.apparatusId}; operation {receipt.operationId}{receipt.revisionId ? `; revision ${receipt.revisionId}` : ""}.</p>}
    </section>
  );
}
