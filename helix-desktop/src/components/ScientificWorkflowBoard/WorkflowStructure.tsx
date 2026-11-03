import type { CommandEvidenceHistoryRecord } from '../../types/agent';
import type { ProposalContent, ProposalNode } from '../../lib/scientificWorkflowProposalStore';

interface WorkflowStructureProps {
  canEdit: boolean;
  content: ProposalContent | null;
  onContentChange: (update: (content: ProposalContent) => ProposalContent) => void;
  resourceLeaseDiagnostics: readonly CommandEvidenceHistoryRecord['checks'][number][];
}

export function WorkflowStructure({ canEdit, content, onContentChange, resourceLeaseDiagnostics }: WorkflowStructureProps) {
  const relevantDiagnostics = resourceLeaseDiagnostics.filter((diagnostic) => /resource|lease/i.test(diagnostic.check));

  function addNode(kind: ProposalNode['kind'], label: string) {
    onContentChange((current) => ({
      ...current,
      nodes: [...current.nodes, { id: crypto.randomUUID(), label, kind }],
    }));
  }

  function updateNodeLabel(nodeId: string, label: string) {
    onContentChange((current) => ({
      ...current,
      nodes: current.nodes.map((node) => node.id === nodeId ? { ...node, label } : node),
    }));
  }

  function addEdge() {
    if (!content || content.nodes.length < 2) return;
    const [first, ...rest] = content.nodes;
    const last = rest[rest.length - 1];
    onContentChange((current) => ({
      ...current,
      edges: [...current.edges, { id: crypto.randomUUID(), source: first.id, target: last.id }],
    }));
  }

  return (
    <section aria-label="Workflow proposal structure">
      <h2>Proposal control flow</h2>
      <section aria-label="If/Else proposal structure" data-editable={canEdit}>
        <h3>If/Else</h3>
        <p>Condition evidence: unavailable — no Agent-authoritative condition evidence was supplied.</p>
        <div role="group" aria-label="If/Else proposal ports">
          <section aria-label="True port">
            <h4>True port</h4>
            <p>Proposal path when the condition is true.</p>
          </section>
          <section aria-label="False port">
            <h4>False port</h4>
            <p>Proposal path when the condition is false.</p>
          </section>
        </div>
        <button type="button" disabled={!canEdit} onClick={() => addNode('condition', 'If/Else condition')}>Modify If/Else proposal structure</button>
      </section>

      <section aria-label="Fork/Join proposal structure" data-editable={canEdit}>
        <h3>Fork/Join</h3>
        <p>Fork creates parallel proposal branches; Join waits for each branch to complete.</p>
        <ol aria-label="Fork branches">
          <li><h4>Branch 1</h4><p>No proposal steps have been supplied for this branch.</p></li>
          <li><h4>Branch 2</h4><p>No proposal steps have been supplied for this branch.</p></li>
        </ol>
        <p>Join: all proposal branches rejoin here.</p>
        {relevantDiagnostics.length === 0 ? (
          <p>Agent resource and lease diagnostics: unavailable</p>
        ) : (
          <section aria-label="Agent resource and lease diagnostics">
            <h4>Agent resource and lease diagnostics</h4>
            <ul>
              {relevantDiagnostics.map((diagnostic, index) => (
                <li key={`${diagnostic.check}:${diagnostic.code}:${index}`}>
                  {diagnostic.check}: {diagnostic.status}; code {diagnostic.code}
                  {diagnostic.reason ? `; ${diagnostic.reason}` : ''}
                </li>
              ))}
            </ul>
          </section>
        )}
        <p>Diagnostic evidence is display-only; it does not perform Agent admission checks.</p>
        <button type="button" disabled={!canEdit} onClick={() => addNode('parallel', 'Fork/Join branches')}>Modify Fork/Join proposal structure</button>
      </section>
      <section aria-label="Local proposal nodes">
        <h3>Proposal nodes</h3>
        {!content || content.nodes.length === 0 ? <p>No proposal nodes.</p> : content.nodes.map((node) => (
          <label key={node.id}>
            {node.kind} node
            <input
              aria-label={`${node.kind} node label`}
              value={node.label}
              disabled={!canEdit}
              onChange={(event) => updateNodeLabel(node.id, event.currentTarget.value)}
            />
          </label>
        ))}
        <button type="button" disabled={!canEdit} onClick={() => addNode('step', 'New proposal step')}>Add proposal node</button>
        <button type="button" disabled={!canEdit || !content || content.nodes.length < 2} onClick={addEdge}>Add proposal edge</button>
        <ul aria-label="Proposal edges">
          {content?.edges.map((edge) => <li key={edge.id}>Local proposal edge {edge.source} to {edge.target}</li>)}
        </ul>
      </section>
    </section>
  );
}
