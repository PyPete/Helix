import { describe, expect, it, vi } from 'vitest';
import type { CommandEvidenceHistoryRecord, ExperimentRecord } from '../../types/agent';
import type { AssemblyDefaults, SampleEvent, ScientificRevision } from '../../types/scientific';
import { adaptCommandEvidence } from './BoardEvidenceAdapter';
import { createBoardReadModels } from './BoardReadModel';
import type { BoardReadModel } from './BoardReadModel';

function recordsFromView(models: BoardReadModel[], view: BoardReadModel['view']) {
  const model = models.find((candidate) => candidate.view === view);
  return model && 'records' in model ? model.records : undefined;
}

const evidenceRecord: CommandEvidenceHistoryRecord = {
  evidenceId: 'evidence-1',
  commandId: 'command-1',
  executionId: 'execution-1',
  correlationId: 'correlation-1',
  sequence: 4,
  outcome: 'Completed',
  verifiedActor: 'Scientist',
  deviceId: 'simulator',
  capability: 'SetFlow',
  timestamp: '2026-11-03T12:00:00Z',
  parameters: '{"flow":2}',
  checks: [{ check: 'lease', status: 'Passed', code: 'LEASE_OK' }],
};

describe('createBoardReadModels', () => {
  it('returns all six named views as unavailable when no native source is supplied', () => {
    const models = createBoardReadModels();

    expect(models.map((model) => model.view)).toEqual([
      'setup', 'experiment', 'plan', 'run', 'results', 'samples-analysis',
    ]);
    expect(models.filter((model) => model.status === 'unavailable')).toHaveLength(6);
    expect(models.find((model) => model.view === 'results')).toEqual({
      view: 'results',
      status: 'unavailable',
      reason: 'reviewed-result-record-not-supported',
    });
  });

  it('passes supplied native facts and command evidence through without changing identity or order', () => {
    const setup: AssemblyDefaults[] = [{
      apparatusRevisionId: 'apparatus-r3',
      procedureRevisionId: 'procedure-r7',
      planRevisionId: 'plan-r2',
      policyRevisionId: 'policy-r5',
      previewToken: 'preview-token',
    }];
    const experiment: ExperimentRecord[] = [{
      experimentId: 'experiment-1',
      name: 'Heat transfer',
      lifecycle: 'Held',
      activePhase: 'heating',
      activeRunspaceId: 'runspace-1',
    }];
    const plan: ScientificRevision[] = [{
      revisionId: 'plan-revision-9',
      name: 'Plan revision 9',
      actor: 'Scientist',
      recordedAt: '2026-11-03T12:00:00Z',
      contentHash: 'sha256:exact',
    }];
    const samplesAnalysis: SampleEvent[] = [{
      eventId: 'sample-event-6',
      sampleId: 'sample-1',
      kind: 'CustodyMoved',
      location: 'cold storage',
      occurredAtUtc: '2026-11-03T11:00:00Z',
      recordedAtUtc: '2026-11-03T12:00:00Z',
      actorSid: 'scientist-sid',
      serverSequence: 6,
    }];
    const run = adaptCommandEvidence('execution-1', {
      records: [evidenceRecord],
      loading: false,
      unavailable: false,
      error: null,
      refresh: vi.fn(),
    });

    const models = createBoardReadModels({ setup, experiment, plan, run, samplesAnalysis });
    expect(models.find((model) => model.view === 'setup')).toMatchObject({ status: 'available', source: 'agent-native' });
    expect(recordsFromView(models, 'setup')).toBe(setup);
    expect(recordsFromView(models, 'experiment')).toBe(experiment);
    expect(recordsFromView(models, 'plan')).toBe(plan);
    expect(recordsFromView(models, 'run')?.[0]).toBe(evidenceRecord);
    expect(recordsFromView(models, 'samples-analysis')).toBe(samplesAnalysis);
    expect(models.find((model) => model.view === 'results')?.status).toBe('unavailable');
  });

  it('does not promote a planned revision into run, results, or sample evidence', () => {
    const plan: ScientificRevision[] = [{
      revisionId: 'planned-procedure-r1',
      name: 'Planned collection step',
      actor: 'Scientist',
      recordedAt: '2026-11-03T12:00:00Z',
      contentHash: 'sha256:plan',
    }];

    const models = createBoardReadModels({ plan });
    expect(models.find((model) => model.view === 'plan')?.status).toBe('available');
    expect(models.find((model) => model.view === 'run')?.status).toBe('unavailable');
    expect(models.find((model) => model.view === 'results')?.status).toBe('unavailable');
    expect(models.find((model) => model.view === 'samples-analysis')?.status).toBe('unavailable');
  });
});
