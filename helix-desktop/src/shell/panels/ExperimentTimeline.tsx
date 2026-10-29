import type { ReactNode } from 'react';
import type { RunspaceEvent, ExperimentEvent, CommandEvidenceEvent } from '../../types/agent';
import './panels.css';

export interface ExperimentTimelineProps {
  runspaceEvents: RunspaceEvent[];
  experimentEvents: ExperimentEvent[];
  activeExperimentId: string | null;
  commandEvidence?: CommandEvidenceEvent[];
}

export function ExperimentTimeline({
  runspaceEvents,
  experimentEvents,
  activeExperimentId,
  commandEvidence = [],
}: ExperimentTimelineProps) {
  if (!activeExperimentId && commandEvidence.length === 0) {
    return (
      <section aria-label="Experiment Timeline" data-testid="experiment-timeline">
        <p data-testid="timeline-empty">No active experiment</p>
      </section>
    );
  }

  // Filter events to active experiment
  const filteredRunspaceEvents = runspaceEvents.filter(
    (e) => e.experimentId === activeExperimentId
  );
  const filteredExperimentEvents = experimentEvents.filter(
    (e) => e.experimentId === activeExperimentId
  );

  // Merge all events into a unified list with a discriminant
  type RunspaceItem = { kind: 'runspace'; event: RunspaceEvent };
  type ExperimentItem = { kind: 'experiment'; event: ExperimentEvent };
  type TimelineItem = RunspaceItem | ExperimentItem;

  const allItems: TimelineItem[] = [
    ...filteredRunspaceEvents.map((e): RunspaceItem => ({ kind: 'runspace', event: e })),
    ...filteredExperimentEvents.map((e): ExperimentItem => ({ kind: 'experiment', event: e })),
  ];

  // Sort by timestamp ascending
  allItems.sort((a, b) => {
    const ta = a.kind === 'runspace' ? a.event.timestamp : a.event.timestamp;
    const tb = b.kind === 'runspace' ? b.event.timestamp : b.event.timestamp;
    return ta < tb ? -1 : ta > tb ? 1 : 0;
  });

  if (allItems.length === 0 && commandEvidence.length === 0) {
    return (
      <section aria-label="Experiment Timeline" data-testid="experiment-timeline">
        <p data-testid="timeline-empty">No active experiment</p>
      </section>
    );
  }

  const evidenceItems = [...commandEvidence]
    .sort((left, right) => left.sequence - right.sequence)
    .map((evidence) => (
      <li key={evidence.evidenceId} data-testid="timeline-command-evidence">
        <strong>{evidence.state}</strong> {evidence.timestamp} {evidence.capability} on {evidence.deviceId}
        {' '}by {evidence.verifiedActor} (command {evidence.commandId})
        <details>
          <summary>Admission checks</summary>
          <ul>
            {evidence.checks.map((check) => <li key={check.check}>{check.check}: {check.status} ({check.code})</li>)}
          </ul>
        </details>
      </li>
    ));

  // Build rendered list items, grouping step events by experimentPhase
  const listItems: ReactNode[] = [];
  const renderedPhases = new Set<string>();

  for (let i = 0; i < allItems.length; i++) {
    const item = allItems[i];

    if (item.kind === 'experiment') {
      const evt = item.event;
      if (evt.type === 'experiment-started') {
        listItems.push(
          <li key={`exp-started-${i}`} data-testid="timeline-experiment-started">
            {evt.timestamp} experiment-started: {evt.name}
          </li>
        );
      } else if (evt.type === 'experiment-phase-changed') {
        const phase = evt.experimentPhase;
        if (!renderedPhases.has(phase)) {
          renderedPhases.add(phase);
          listItems.push(
            <li key={`phase-${phase}-${i}`} data-testid={`timeline-phase-${phase}`}>
              Phase: {phase}
            </li>
          );
        }
        listItems.push(
          <li key={`exp-phase-changed-${i}`} data-testid={`timeline-step-experiment-phase-changed`} style={{ paddingLeft: 16 }}>
            {evt.timestamp} experiment-phase-changed → {phase}
          </li>
        );
      } else if (evt.type === 'experiment-completed') {
        listItems.push(
          <li key={`exp-completed-${i}`} data-testid="timeline-experiment-completed">
            {evt.timestamp} experiment-completed
          </li>
        );
      } else if (evt.type === 'experiment-failed') {
        listItems.push(
          <li key={`exp-failed-${i}`} data-testid="timeline-experiment-failed">
            {evt.timestamp} experiment-failed{evt.reason ? `: ${evt.reason}` : ''}
          </li>
        );
      }
    } else {
      // RunspaceEvent
      const evt = item.event;
      const phase = evt.experimentPhase as string | undefined;

      if (evt.type === 'manual-action') {
        listItems.push(
          <li key={`manual-${i}`} data-testid="timeline-manual-action" style={{ paddingLeft: 16 }}>
            [M] {evt.timestamp} manual-action
          </li>
        );
      } else {
        // Step or other runspace event — group under phase header
        if (phase && !renderedPhases.has(phase)) {
          renderedPhases.add(phase);
          listItems.push(
            <li key={`phase-${phase}-${i}`} data-testid={`timeline-phase-${phase}`}>
              Phase: {phase}
            </li>
          );
        }
        listItems.push(
          <li key={`step-${i}`} data-testid={`timeline-step-${evt.type}`} style={{ paddingLeft: 16 }}>
            {evt.timestamp} {evt.type}
          </li>
        );
      }
    }
  }

  return (
    <section aria-label="Experiment Timeline" data-testid="experiment-timeline">
      <ul data-testid="timeline-list">
        {evidenceItems}
        {listItems}
      </ul>
    </section>
  );
}
