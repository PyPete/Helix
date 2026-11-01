import { useEffect, useState } from "react";
import { assemblyDefaults, assembleScientific, listScientificPolicies, readScientificAssembly } from "../../lib/scientificClient";
import type { AssemblyDefaults, PolicyRevision, ScientificAssembly } from "../../types/scientific";
import { ScientificRecordUi } from "./ScientificRecordUi";

interface FrozenAssemblyProps {
  sessionId?: string;
}

/**
 * Read-only evidence for a future execution reference assembly. This view never
 * grants authority, starts a campaign, or issues a physical command.
 */
export function FrozenAssembly(_props: FrozenAssemblyProps) {
  const [defaults, setDefaults] = useState<AssemblyDefaults | null>(null);
  const [policies, setPolicies] = useState<PolicyRevision[]>([]);
  const [assembly, setAssembly] = useState<ScientificAssembly | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [confirmationOpen, setConfirmationOpen] = useState(false);

  useEffect(() => {
    void Promise.all([assemblyDefaults(), listScientificPolicies()])
      .then(([nextDefaults, nextPolicies]) => {
        setDefaults(nextDefaults);
        setPolicies(nextPolicies);
      })
      .catch(() => setError("Scientific records could not be loaded from the local Helix Agent. Check the local Agent connection, then Retry."))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <p>Loading Agent-backed scientific records…</p>;
  if (error) return <p role="alert">{error}</p>;
  if (!defaults) return <p role="alert">No published scientific references are available for assembly.</p>;

  const policy = policies.find((candidate) => candidate.revisionId === defaults.policyRevisionId);
  const isApproved = policy?.approval?.status === "Approved";
  const request = defaults.policyRevisionId ? {
    operationId: crypto.randomUUID(),
    apparatusRevisionId: defaults.apparatusRevisionId,
    procedureRevisionId: defaults.procedureRevisionId,
    planRevisionId: defaults.planRevisionId,
    policyRevisionId: defaults.policyRevisionId,
    previewToken: defaults.previewToken,
  } : null;

  async function confirmAssembly() {
    if (!request) return;
    try {
      const result = await assembleScientific(request);
      if (result.status === "Committed") {
        setAssembly(await readScientificAssembly(result.assemblyId));
        setConfirmationOpen(false);
        return;
      }
      setError(result.status === "Conflict"
        ? "Scientific references changed. Reload and explicitly confirm the current exact references."
        : "A valid authoritative policy revision is required to freeze execution references. No assembly was created.");
    } catch {
      setError("Assembly outcome uncertain. Do not repeat this action. Check outcome using the original operation ID.");
    }
  }

  const references = [
    ["Apparatus revision", defaults.apparatusRevisionId],
    ["Plan revision", defaults.planRevisionId],
    ["Plan-pinned procedure revision", defaults.procedureRevisionId],
    ["Policy revision", defaults.policyRevisionId ?? "Unavailable"],
    ["Preview token", defaults.previewToken],
  ];

  return <section aria-labelledby="assembly-title" data-testid="frozen-assembly">
    <h2 id="assembly-title">Execution reference assembly</h2>
    <ScientificRecordUi entityLabel="Assembly plan reference" entityId={defaults.planRevisionId} state={assembly ? "Frozen" : "Draft"} />
    <p>Latest published at assembly start</p>
    {assembly ? <><p><strong>Frozen</strong></p><p>Immutable manifest: <code>{assembly.assemblyId}</code></p></> : null}
    <dl>{references.map(([label, value]) => <div key={label}><dt>{label}</dt><dd><code title={value}>{value}</code></dd></div>)}</dl>
    {policy ? <p>Policy schema: <code>{policy.schema}</code>; hash: <code>{policy.contentHash}</code>; authority: {policy.approval?.status ?? "Unavailable"}</p> : null}
    {!isApproved ? <p role="alert">A valid authoritative policy revision is required to freeze execution references. No assembly was created.</p> : null}
    {!assembly && isApproved && request ? <button type="button" onClick={() => setConfirmationOpen(true)}>Assemble frozen references</button> : null}
    <p>Later publications will not update this assembly. This action creates no campaign and sends no physical command.</p>
    {confirmationOpen && request ? <div role="dialog" aria-modal="true" aria-label="Confirm frozen reference assembly">
      <h3>Confirm frozen reference assembly</h3>
      <p>Freeze these exact references for a future execution? Later publications will not update this assembly, and no campaign will start.</p>
      <ul>{references.slice(0, 4).map(([label, value]) => <li key={label}>{label}: <code>{value}</code></li>)}</ul>
      <button type="button" onClick={() => void confirmAssembly()}>Confirm exact references</button>
      <button type="button" onClick={() => setConfirmationOpen(false)}>Cancel</button>
    </div> : null}
  </section>;
}
