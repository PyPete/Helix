import { FormEvent, useRef, useState } from "react";
import { adoptProcedure, createScientific, deriveProcedure, publishScientific, readScientificOperation } from "../../lib/scientificClient";
import type { PlanDraft, ProcedureDraft, ScientificKind, ScientificReceipt } from "../../types/scientific";

export interface ProcedurePlansProps { kind: ScientificKind; selectedId?: string; }

/** Procedures and plans remain separate Agent-backed scientific records. */
export function ProcedurePlans({ kind, selectedId }: ProcedurePlansProps) {
  const label = kind === "procedure" ? "Procedures" : "Plans";
  const [name, setName] = useState("");
  const [procedureRevisionId, setProcedureRevisionId] = useState("");
  const [value, setValue] = useState("");
  const [unit, setUnit] = useState("mL");
  const [revisionName, setRevisionName] = useState("");
  const [sourceRevisionId, setSourceRevisionId] = useState("");
  const [confirmAdoption, setConfirmAdoption] = useState(false);
  const [receipt, setReceipt] = useState<ScientificReceipt>();
  const [state, setState] = useState<"idle" | "pending" | "uncertain" | "conflict" | "error">("idle");
  const [error, setError] = useState<string>();
  const operationId = useRef<string>();
  const resourceId = useRef(selectedId);

  function draft(): ProcedureDraft | PlanDraft {
    const id = operationId.current ??= crypto.randomUUID();
    return kind === "procedure"
      ? { name, operationId: id, wsl: { nodes: [], edges: [] }, declarations: [], plannedCollections: [] }
      : { name, operationId: id, procedureRevisionId, values: [{ name: "parameter", value, unit }] };
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!name.trim() || state === "pending" || state === "uncertain") return;
    setState("pending"); setError(undefined);
    try {
      const result = await createScientific(kind, draft());
      resourceId.current = result.resourceId;
      setReceipt(result); operationId.current = undefined; setState("idle");
    } catch (reason) {
      setState("uncertain"); setError(reason instanceof Error ? reason.message : "Outcome uncertain.");
    }
  }

  async function publish() {
    if (!resourceId.current || !revisionName.trim()) return;
    operationId.current ??= crypto.randomUUID(); setState("pending");
    try {
      const result = await publishScientific(kind, resourceId.current, { operationId: operationId.current, revisionName, expectedToken: 0 });
      setReceipt(result); operationId.current = undefined; setState("idle");
    } catch (reason) { setState("uncertain"); setError(reason instanceof Error ? reason.message : "Outcome uncertain."); }
  }

  async function checkOutcome() {
    if (!operationId.current) return;
    try { setReceipt(await readScientificOperation(kind, operationId.current)); operationId.current = undefined; setState("idle"); }
    catch { setError("No committed result has been recorded yet."); }
  }

  async function derive() {
    if (!sourceRevisionId.trim() || !name.trim()) return;
    operationId.current ??= crypto.randomUUID(); setState("pending"); setError(undefined);
    try {
      const result = await deriveProcedure({ operationId: operationId.current, sourceRevisionId, name });
      resourceId.current = result.resourceId; setReceipt(result); operationId.current = undefined; setState("idle");
    } catch (reason) { setState("uncertain"); setError(reason instanceof Error ? reason.message : "Outcome uncertain."); }
  }

  async function confirmProcedureAdoption() {
    if (!resourceId.current || !procedureRevisionId.trim()) return;
    operationId.current ??= crypto.randomUUID(); setState("pending"); setConfirmAdoption(false); setError(undefined);
    try {
      const result = await adoptProcedure(resourceId.current, { operationId: operationId.current, procedureRevisionId, expectedToken: 0 });
      setReceipt(result.receipt); operationId.current = undefined; setState("idle");
    } catch (reason) { setState("uncertain"); setError(reason instanceof Error ? reason.message : "Outcome uncertain."); }
  }

  return <section aria-labelledby={`${kind}-title`}>
    <h2 id={`${kind}-title`}>{label}</h2>
    <p>Loading Agent-backed scientific records…</p>
    <p>No published procedures or plans yet</p>
    <form onSubmit={(event) => void save(event)} aria-label={`${label} draft`}>
      <label htmlFor={`${kind}-name`}>Name</label><input id={`${kind}-name`} value={name} onChange={(event) => setName(event.target.value)} disabled={state === "pending" || state === "uncertain"} required />
      {kind === "procedure" ? <><label htmlFor="procedure-wsl">WSL document</label><textarea id="procedure-wsl" aria-label="WSL document" defaultValue={'{"nodes":[],"edges":[]}'} /><p>Declarations and planned collection descriptors preserve stable WSL node IDs and order.</p><label htmlFor="source-revision">Derived from procedure revision</label><input id="source-revision" value={sourceRevisionId} onChange={(event) => setSourceRevisionId(event.target.value)} /><button type="button" onClick={() => void derive()} disabled={!sourceRevisionId.trim() || !name.trim() || state !== "idle"}>Derive procedure revision</button>{sourceRevisionId && <p>Derived from: procedure revision {sourceRevisionId}. The source publication remains read-only.</p>}</> : <><label htmlFor="plan-procedure">Pins procedure revision</label><input id="plan-procedure" value={procedureRevisionId} onChange={(event) => setProcedureRevisionId(event.target.value)} required /><label htmlFor="plan-value">Parameter value</label><input id="plan-value" value={value} onChange={(event) => setValue(event.target.value)} /><label htmlFor="plan-unit">Unit</label><input id="plan-unit" value={unit} onChange={(event) => setUnit(event.target.value)} /><p>Pins procedure: {procedureRevisionId || "choose a published revision"}</p><p>Compatibility is checked by the Agent against the exact parameter declarations before adoption.</p><button type="button" onClick={() => setConfirmAdoption(true)} disabled={!resourceId.current || !procedureRevisionId.trim() || state !== "idle"}>Adopt newer procedure</button></>}
      <button type="submit" disabled={state === "pending" || state === "uncertain"}>{state === "pending" ? "Awaiting Agent acknowledgement…" : "Save draft"}</button>
    </form>
    <label htmlFor={`${kind}-revision-name`}>Revision name</label><input id={`${kind}-revision-name`} value={revisionName} onChange={(event) => setRevisionName(event.target.value)} />
    <button type="button" onClick={() => void publish()} disabled={!resourceId.current || !revisionName.trim() || state !== "idle"}>Publish named revision</button>
    {state === "uncertain" && <p role="alert">Outcome uncertain. Do not repeat this action. Check outcome using operation ID {operationId.current}. <button type="button" onClick={() => void checkOutcome()}>Check outcome</button></p>}
    {error && state !== "uncertain" && <p role="alert">{error}</p>}
    {receipt && <p role="status">Committed revision {receipt.revisionId ?? "draft"}; operation {receipt.operationId}</p>}
    {confirmAdoption && <div role="dialog" aria-modal="true" aria-label="Confirm procedure adoption"><p>Adopt procedure revision {procedureRevisionId} into this plan draft? This does not publish the plan.</p><button type="button" onClick={() => void confirmProcedureAdoption()}>Confirm adoption</button><button type="button" onClick={() => setConfirmAdoption(false)}>Cancel</button></div>}
    <h3>Revision history</h3><table><thead><tr><th>Name</th><th>Revision ID</th><th>Published by</th><th>Recorded time</th><th>Content hash</th></tr></thead><tbody><tr><td colSpan={5}>Immutable history loads from the local Helix Agent.</td></tr></tbody></table>
    <p>Published revisions are read-only. Opening a newer procedure never adopts it into a plan.</p>
  </section>;
}
