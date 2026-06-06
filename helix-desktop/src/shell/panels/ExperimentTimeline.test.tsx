import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { ExperimentTimeline } from './ExperimentTimeline';
import type { RunspaceEvent, ExperimentStartedEvent, ExperimentEvent } from '../../types/agent';

describe('ExperimentTimeline', () => {
  it('renders empty state when no activeExperimentId', () => {
    render(
      <ExperimentTimeline
        runspaceEvents={[]}
        experimentEvents={[]}
        activeExperimentId={null}
      />
    );
    expect(screen.getByTestId('timeline-empty')).toBeInTheDocument();
  });

  it('renders experiment-started event', () => {
    const startedEvent: ExperimentStartedEvent = {
      type: 'experiment-started',
      experimentId: 'exp-1',
      name: 'My Experiment',
      experimentPhase: 'init',
      timestamp: '2026-06-01T00:00:00Z',
    };
    render(
      <ExperimentTimeline
        runspaceEvents={[]}
        experimentEvents={[startedEvent]}
        activeExperimentId='exp-1'
      />
    );
    expect(screen.getByTestId('timeline-experiment-started')).toBeInTheDocument();
  });

  it('renders step events under phase', () => {
    const startedEvent: ExperimentStartedEvent = {
      type: 'experiment-started',
      experimentId: 'exp-2',
      name: 'Phase Test',
      experimentPhase: 'main',
      timestamp: '2026-06-01T00:00:00Z',
    };
    const stepEvent: RunspaceEvent = {
      type: 'step-started',
      runspaceId: 'rs-1',
      timestamp: '2026-06-01T00:00:01Z',
      experimentId: 'exp-2',
      experimentPhase: 'main',
    };
    render(
      <ExperimentTimeline
        runspaceEvents={[stepEvent]}
        experimentEvents={[startedEvent]}
        activeExperimentId='exp-2'
      />
    );
    expect(screen.getByTestId('timeline-phase-main')).toBeInTheDocument();
    expect(screen.getByTestId('timeline-step-step-started')).toBeInTheDocument();
  });

  it('renders manual-action events with [M] marker', () => {
    const manualEvent: RunspaceEvent = {
      type: 'manual-action',
      runspaceId: 'rs-manual',
      timestamp: '2026-06-01T00:00:02Z',
      experimentId: 'exp-3',
    };
    render(
      <ExperimentTimeline
        runspaceEvents={[manualEvent]}
        experimentEvents={[]}
        activeExperimentId='exp-3'
      />
    );
    const manualItem = screen.getByTestId('timeline-manual-action');
    expect(manualItem).toBeInTheDocument();
    expect(manualItem.textContent).toContain('[M]');
  });

  it('filters events to activeExperimentId only', () => {
    const startedA: ExperimentStartedEvent = {
      type: 'experiment-started',
      experimentId: 'exp-a',
      name: 'Experiment A',
      experimentPhase: 'init',
      timestamp: '2026-06-01T00:00:00Z',
    };
    const startedB: ExperimentStartedEvent = {
      type: 'experiment-started',
      experimentId: 'exp-b',
      name: 'Experiment B',
      experimentPhase: 'init',
      timestamp: '2026-06-01T00:00:00Z',
    };
    const allExperimentEvents: ExperimentEvent[] = [startedA, startedB];
    render(
      <ExperimentTimeline
        runspaceEvents={[]}
        experimentEvents={allExperimentEvents}
        activeExperimentId='exp-a'
      />
    );
    // Only one experiment-started should appear (for exp-a)
    const startedItems = screen.getAllByTestId('timeline-experiment-started');
    expect(startedItems).toHaveLength(1);
    expect(startedItems[0].textContent).toContain('Experiment A');
  });
});
