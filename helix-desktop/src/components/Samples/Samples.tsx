import { FormEvent, useEffect, useRef, useState } from "react";
import { appendSampleEvent, confirmSample, listSamples, readSample, sampleEvents } from "../../lib/scientificClient";
import type { SampleEvent, SampleEventAppendRequest, SampleProjection } from "../../types/scientific";

type MutationState = "idle" | "pending" | "uncertain" | "conflict" | "error";

function newOperationId(): string {
  return crypto.randomUUID();
}

/** Sample facts are Agent-backed evidence; this view never infers collection from a plan or procedure. */
export function Samples({ selectedId }: { selectedId?: string }) {
  const [samples, setSamples] = useState<SampleProjection[]>([]);
  const [selected, setSelected] = useState<SampleProjection>();
  const [history, setHistory] = useState<SampleEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [readError, setReadError] = useState<string>();
  const [mutationState, setMutationState] = useState<MutationState>("idle");
  const [mutationError, setMutationError] = useState<string>();
  const [confirmationOpen, setConfirmationOpen] = useState(false);
  const [eventKind, setEventKind] = useState<"Relabeled" | "CustodyMoved" | "Correction">();
  const operationId = useRef<string>();

  async function reload(preferredId = selectedId ?? selected?.identity.sampleId) {
    setLoading(true); setReadError(undefined);
    try {
      const loaded = await listSamples();
      setSamples(loaded);
      const next = preferredId ? loaded.find((sample) => sample.identity.sampleId === preferredId) : loaded[0];
      setSelected(next);
      setHistory(next ? await sampleEvents(next.identity.sampleId) : []);
    } catch (reason) {
      setReadError(reason instanceof Error ? reason.message : "Scientific records could not be loaded from the local Helix Agent.");
    } finally { setLoading(false); }
  }

  useEffect(() => { void reload(selectedId); }, [selectedId]);

  async function choose(sample: SampleProjection) {
    setSelected(sample);
    try { setHistory(await sampleEvents(sample.identity.sampleId)); }
    catch (reason) { setReadError(reason instanceof Error ? reason.message : "Scientific records could not be loaded from the local Helix Agent."); }
  }

  async function confirm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (mutationState !== "idle") return;
    const data = new FormData(event.currentTarget);
    operationId.current ??= newOperationId(); setMutationState("pending"); setMutationError(undefined);
    try {
      const result = await confirmSample({
        operationId: operationId.current,
        location: String(data.get("location") ?? ""),
        occurredAtUtc: String(data.get("occurredAtUtc") ?? ""),
        label: String(data.get("label") ?? "") || undefined,
        custodian: String(data.get("custodian") ?? "") || undefined,
      });
      operationId.current = undefined; setConfirmationOpen(false); setMutationState("idle");
      await reload(result.sample.identity.sampleId);
    } catch (reason) {
      setMutationState("uncertain"); setMutationError(reason instanceof Error ? reason.message : "Collection outcome uncertain.");
    }
  }

  async function append(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected || !eventKind || mutationState !== "idle") return;
    const data = new FormData(event.currentTarget);
    operationId.current ??= newOperationId(); setMutationState("pending"); setMutationError(undefined);
    const request: SampleEventAppendRequest = {
      operationId: operationId.current, kind: eventKind,
      location: String(data.get("location") ?? ""), occurredAtUtc: String(data.get("occurredAtUtc") ?? ""),
      label: String(data.get("label") ?? "") || undefined, custodian: String(data.get("custodian") ?? "") || undefined,
      expectedSequence: selected.serverSequence,
      supersedesEventId: String(data.get("supersedesEventId") ?? "") || undefined,
      correctionReason: String(data.get("correctionReason") ?? "") || undefined,
    };
    try {
      const result = await appendSampleEvent(selected.identity.sampleId, request);
      operationId.current = undefined; setEventKind(undefined); setMutationState("idle");
      await reload(result.sample.identity.sampleId);
    } catch (reason) {
      setMutationState("uncertain"); setMutationError(reason instanceof Error ? reason.message : "Outcome uncertain.");
    }
  }

  async function checkOutcome() {
    if (!operationId.current) return;
    try {
      // Re-read only known evidence. A missing receipt is deliberately not treated as absence or permission to retry.
      if (selected) await readSample(selected.identity.sampleId);
      setMutationError("No committed result has been recorded yet. Do not repeat this action.");
    } catch { setMutationError("No committed result has been recorded yet. Do not repeat this action."); }
  }

  return <section aria-labelledby="samples-title">
    <h2 id="samples-title">Samples</h2>
    <p>Planned reference only — collection not confirmed.</p>
    <button type="button" onClick={() => setConfirmationOpen(true)}>Confirm sample collection</button>
    {loading && <p>Loading Agent-backed scientific records…</p>}
    {readError && <p role="alert">Scientific records could not be loaded from the local Helix Agent. Check the local Agent connection, then <button type="button" onClick={() => void reload()}>Retry</button>. {readError}</p>}
    {!loading && !readError && samples.length === 0 && <p>No confirmed samples yet. A planned step is not a sample. Confirm actual collection to create an immutable sample record.</p>}
    <div aria-label="Confirmed samples">
      {samples.map((sample) => <button type="button" key={sample.identity.sampleId} onClick={() => void choose(sample)} aria-pressed={selected?.identity.sampleId === sample.identity.sampleId}>
        {sample.label || "Unlabelled sample"} · {sample.identity.sampleId}
      </button>)}
    </div>
    {selected && <article aria-label="Sample evidence">
      <h3>{selected.label || "Unlabelled sample"}</h3>
      <p>Helix sample ID: <code>{selected.identity.sampleId}</code></p>
      <p>Origin: {selected.identity.origin}</p><p>Current custody: {selected.location} ({selected.custodian || "no custodian recorded"})</p>
      <p>Derived from append-only history · server sequence {selected.serverSequence}</p>
      <button type="button" onClick={() => setEventKind("Relabeled")}>Relabel sample</button>
      <button type="button" onClick={() => setEventKind("CustodyMoved")}>Record custody change</button>
      <button type="button" onClick={() => setEventKind("Correction")}>Record correction</button>
      <h4>Immutable custody history</h4>
      <table><thead><tr><th>Sequence</th><th>Event</th><th>Occurrence</th><th>Recorded</th><th>Actor</th><th>Location</th><th>Reason</th><th>Supersedes</th></tr></thead><tbody>
        {history.map((entry) => <tr key={entry.eventId}><td>{entry.serverSequence}</td><td>{entry.kind}</td><td>{entry.occurredAtUtc}</td><td>{entry.recordedAtUtc}</td><td>{entry.actorSid}</td><td>{entry.location}</td><td>{entry.correctionReason || "—"}</td><td>{entry.supersedesEventId || "—"}</td></tr>)}
      </tbody></table>
    </article>}
    {mutationState === "pending" && <p role="status">Awaiting Agent acknowledgement…</p>}
    {mutationState === "uncertain" && <p role="alert">Outcome uncertain. Do not repeat this action. Check outcome using operation ID {operationId.current}. <button type="button" onClick={() => void checkOutcome()}>Check outcome</button> {mutationError}</p>}
    {confirmationOpen && <div role="dialog" aria-modal="true" aria-labelledby="confirm-sample-title"><h3 id="confirm-sample-title">Confirm sample collection</h3><p>Confirm only after actual collection. This records a new immutable sample fact; it does not infer collection from a procedure step.</p><p>Authenticated actor source: Agent-determined.</p><form onSubmit={(event) => void confirm(event)}><label>Occurrence time<input name="occurredAtUtc" type="datetime-local" required /></label><label>Location<input name="location" required /></label><label>Label<input name="label" /></label><label>Custodian<input name="custodian" /></label><button type="submit">Confirm actual collection</button><button type="button" onClick={() => setConfirmationOpen(false)}>Cancel</button></form></div>}
    {eventKind && selected && <div role="dialog" aria-modal="true" aria-labelledby="sample-event-title"><h3 id="sample-event-title">{eventKind === "Relabeled" ? "Relabel sample" : eventKind === "CustodyMoved" ? "Record custody change" : "Record correction"}</h3><p>Sample ID remains immutable: <code>{selected.identity.sampleId}</code></p><form onSubmit={(event) => void append(event)}><label>Occurrence time<input name="occurredAtUtc" type="datetime-local" required /></label><label>Location<input name="location" defaultValue={selected.location} required /></label><label>Custodian<input name="custodian" defaultValue={selected.custodian} /></label><label>Label<input name="label" defaultValue={selected.label} /></label>{eventKind === "Correction" && <><label>Superseded event ID<input name="supersedesEventId" required /></label><label>Correction reason<input name="correctionReason" required /></label></>}<button type="submit">Record {eventKind}</button><button type="button" onClick={() => setEventKind(undefined)}>Cancel</button></form></div>}
  </section>;
}
