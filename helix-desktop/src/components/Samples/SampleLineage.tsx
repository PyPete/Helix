import { useEffect, useState } from "react";
import { sampleLineage } from "../../lib/scientificClient";
import type { SampleLineage as SampleLineagePage, SampleLineageDirection } from "../../types/scientific";

export function SampleLineage({ sampleId, onSelect }: { sampleId: string; onSelect: (sampleId: string) => void }) {
  const [direction, setDirection] = useState<SampleLineageDirection>("Ancestors");
  const [page, setPage] = useState<SampleLineagePage>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();

  async function load(continuation?: string) {
    setLoading(true); setError(undefined);
    try {
      const next = await sampleLineage(sampleId, direction, continuation);
      setPage((current) => continuation && current ? { ...next, entries: [...current.entries, ...next.entries] } : next);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Lineage evidence could not be loaded."); }
    finally { setLoading(false); }
  }

  useEffect(() => { void load(); }, [sampleId, direction]);

  return <section aria-labelledby="sample-lineage-title">
    <h4 id="sample-lineage-title">Bounded lineage evidence</h4>
    <label>Direction <select value={direction} onChange={(event) => setDirection(event.target.value as SampleLineageDirection)}><option value="Ancestors">Ancestors</option><option value="Descendants">Descendants</option></select></label>
    {loading && <p>Loading lineage evidence…</p>}
    {error && <p role="alert">{error} <button type="button" onClick={() => void load()}>Retry</button></p>}
    {page && !loading && <><table><thead><tr><th>Sample ID</th><th>Origin</th><th>Transformation ID</th><th>Operation</th><th>Actor</th><th>Occurred</th><th>Recorded</th><th>Quantity</th><th>Depth</th></tr></thead><tbody>{page.entries.length === 0 ? <tr><td colSpan={9}>No {direction.toLowerCase()} lineage evidence.</td></tr> : page.entries.map((entry) => <tr key={`${entry.sampleId}-${entry.depth}`}><td><button type="button" onClick={() => onSelect(entry.sampleId)}><code>{entry.sampleId}</code></button></td><td>{entry.origin === "Transformation" ? "Created by transformation" : "Collection"}</td><td><code>{entry.transformationId || "—"}</code></td><td>{entry.operation || "—"}</td><td>{entry.actorSid || "—"}</td><td>{entry.occurredAtUtc || "—"}</td><td>{entry.recordedAtUtc || "—"}</td><td>{entry.quantity ? `${entry.quantity.value} ${entry.quantity.unitCode}` : "Not supplied"}</td><td>{entry.depth}</td></tr>)}</tbody></table>{page.isTruncated && <p>Lineage results are truncated by the approved traversal bound.</p>}{page.continuation && <button type="button" onClick={() => void load(page.continuation)}>Load more</button>}</>}
  </section>;
}
