import { useState, type ReactNode } from 'react';
import type { RunspaceEvent, ExperimentEvent, CommandEvidenceEvent } from '../../types/agent';
import { useCommandEvidence } from '../../hooks/useCommandEvidence';
import { appendCommandReviewNote } from '../../lib/agentClient';
import './panels.css';

export interface ExperimentTimelineProps {
  runspaceEvents: RunspaceEvent[];
  experimentEvents: ExperimentEvent[];
  activeExperimentId: string | null;
  commandEvidence?: CommandEvidenceEvent[];
}

type TimelineEvidence = CommandEvidenceEvent & {
  parameters?: string | null;
  correlationId?: string | null;
  source?: string | null;
};

const unavailable = (value: string | null | undefined) => value?.trim() || 'Not recorded';
const criticalStates = new Set(['Rejected', 'Held']);

function evidenceStatus(state: string): string {
  if (state === 'Rejected') return 'Rejected — not dispatched';
  if (state === 'Held') return 'Held pending reconciliation';
  if (state === 'Unavailable') return 'Outcome not confirmed';
  return state;
}

function EvidenceRow({ evidence, onNote }: { evidence: TimelineEvidence; onNote: (commandId: string, text: string) => Promise<void> }) {
  const [expanded, setExpanded] = useState(false);
  const [note, setNote] = useState('');
  const [noteError, setNoteError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const isCritical = criticalStates.has(evidence.state);
  const submit = async () => {
    if (!note.trim()) return;
    setSaving(true);
    setNoteError(null);
    try {
      await onNote(evidence.commandId, note);
      setNote('');
    } catch (error) {
      setNoteError(error instanceof Error ? error.message : 'Review note is unavailable.');
    } finally {
      setSaving(false);
    }
  };

  return <li className={`timeline-evidence ${isCritical ? 'timeline-evidence--critical' : ''}`} data-testid="timeline-command-evidence">
    <div className="timeline-evidence__summary">
      <span aria-hidden="true">{isCritical ? '?' : '•'}</span>
      <strong>{evidenceStatus(evidence.state)}</strong>
      <span>{unavailable(evidence.capability)} on {unavailable(evidence.deviceId)}</span>
      <span>{unavailable(evidence.timestamp)}</span>
      <span>Verified actor: {unavailable(evidence.verifiedActor)}</span>
      {isCritical && <span role="alert">{evidenceStatus(evidence.state)}</span>}
    </div>
    <button type="button" className="timeline-evidence__toggle" aria-expanded={expanded} onClick={() => setExpanded(!expanded)}>
      {expanded ? 'Hide original facts' : 'Show original facts'}
    </button>
    {expanded && <div className="timeline-evidence__details">
      <dl>
        <dt>Command ID</dt><dd className="timeline-evidence__identifier">{unavailable(evidence.commandId)}</dd>
        <dt>Execution ID</dt><dd className="timeline-evidence__identifier">{unavailable(evidence.executionId)}</dd>
        <dt>Correlation ID</dt><dd className="timeline-evidence__identifier">{unavailable(evidence.correlationId)}</dd>
        <dt>Parameters</dt><dd className="timeline-evidence__value">{unavailable(evidence.parameters)}</dd>
        <dt>Provenance</dt><dd>{unavailable(evidence.source)}</dd>
      </dl>
      <h4>Admission checks</h4>
      {evidence.checks.length === 0 ? <p>Not recorded</p> : <ul>{evidence.checks.map((check) => <li key={`${check.check}-${check.code}`}><strong>{check.status}</strong> {check.check}: {check.code || 'Unavailable'}</li>)}</ul>}
      <label htmlFor={`review-note-${evidence.evidenceId}`}>Add review note</label>
      <textarea id={`review-note-${evidence.evidenceId}`} value={note} maxLength={2000} onChange={(event) => setNote(event.target.value)} />
      <button type="button" disabled={saving || !note.trim()} onClick={() => void submit()}>Add review note</button>
      {noteError && <p role="alert">{noteError}</p>}
    </div>}
  </li>;
}

export function ExperimentTimeline({ runspaceEvents, experimentEvents, activeExperimentId, commandEvidence = [] }: ExperimentTimelineProps) {
  const history = useCommandEvidence(activeExperimentId);
  const committedEvidence: TimelineEvidence[] = history.records.map((record) => ({
    evidenceId: record.evidenceId, commandId: record.commandId, executionId: record.executionId, sequence: record.sequence,
    state: record.outcome === 'Unavailable' ? 'Held' : record.outcome as 'Allowed' | 'Rejected' | 'Held',
    verifiedActor: record.verifiedActor ?? 'Not recorded', capability: record.capability ?? 'Not recorded', deviceId: record.deviceId ?? 'Not recorded',
    timestamp: record.timestamp, parameters: record.parameters, correlationId: record.correlationId, source: record.source,
    checks: record.checks.map((check) => ({ check: check.check, status: check.status, code: check.code })),
  }));
  const evidence = [...commandEvidence, ...committedEvidence].filter((item, index, values) => values.findIndex((candidate) => candidate.evidenceId === item.evidenceId) === index)
    .sort((left, right) => left.sequence - right.sequence || left.evidenceId.localeCompare(right.evidenceId));
  const runspace = runspaceEvents.filter((event) => event.experimentId === activeExperimentId);
  const experiments = experimentEvents.filter((event) => event.experimentId === activeExperimentId);
  const otherEvents: ReactNode[] = [
    ...experiments.map((event) => <li key={`experiment-${event.type}-${event.timestamp}`} data-testid={`timeline-${event.type}`}>{event.timestamp} {event.type}{event.type === 'experiment-started' ? `: ${event.name}` : event.type === 'experiment-failed' && event.reason ? `: ${event.reason}` : ''}</li>),
    ...runspace.map((event) => <li key={`runspace-${event.runspaceId}-${event.type}-${event.timestamp}`} data-testid={event.type === 'manual-action' ? 'timeline-manual-action' : `timeline-step-${event.type}`}>{event.type === 'manual-action' ? '[M] ' : ''}{event.timestamp} {event.type}</li>),
  ];
  const addNote = async (commandId: string, text: string) => { await appendCommandReviewNote(commandId, text); };

  return <section aria-label="Experiment Timeline" data-testid="experiment-timeline">
    <h3>Command evidence</h3>
    {history.loading && <p role="status">Loading committed command evidence…</p>}
    {history.unavailable && <p role="alert" data-testid="timeline-history-unavailable">Committed evidence history is unavailable: {history.error ?? 'Unavailable'}. Available evidence remains visible.</p>}
    {evidence.length === 0 && otherEvents.length === 0 && !history.loading && <div data-testid="timeline-empty"><h4>No command evidence yet</h4><p>Proposed simulated actions, admission checks, and observed outcomes will appear here after the local Agent records them.</p></div>}
    {(evidence.length > 0 || otherEvents.length > 0) && <ul className="timeline-list" data-testid="timeline-list">{evidence.map((item) => <EvidenceRow key={item.evidenceId} evidence={item} onNote={addNote} />)}{otherEvents}</ul>}
  </section>;
}
