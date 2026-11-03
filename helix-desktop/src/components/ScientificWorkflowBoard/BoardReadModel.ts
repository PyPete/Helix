import type { CommandEvidenceView } from './BoardEvidenceAdapter';
import type { CommandEvidenceHistoryRecord, ExperimentRecord } from '../../types/agent';
import type {
  AssemblyDefaults,
  PolicyRevision,
  SampleEvent,
  SampleLineageEntry,
  SampleProjection,
  ScientificAssembly,
  ScientificRevision,
  ScientificSummary,
} from '../../types/scientific';

type SetupRecord = AssemblyDefaults | PolicyRevision | ScientificAssembly;
type PlanRecord = ScientificRevision | ScientificSummary;
type SampleAnalysisRecord = SampleEvent | SampleLineageEntry | SampleProjection;

export interface BoardReadModelSources {
  setup?: readonly SetupRecord[];
  experiment?: readonly ExperimentRecord[];
  plan?: readonly PlanRecord[];
  run?: CommandEvidenceView;
  samplesAnalysis?: readonly SampleAnalysisRecord[];
}

export type BoardReadModel =
  | { view: 'setup'; status: 'available'; source: 'agent-native'; records: readonly SetupRecord[] }
  | { view: 'setup'; status: 'unavailable'; reason: 'no-agent-record-supplied' }
  | { view: 'experiment'; status: 'available'; source: 'agent-native'; records: readonly ExperimentRecord[] }
  | { view: 'experiment'; status: 'unavailable'; reason: 'no-agent-record-supplied' }
  | { view: 'plan'; status: 'available'; source: 'agent-native'; records: readonly PlanRecord[] }
  | { view: 'plan'; status: 'unavailable'; reason: 'no-agent-record-supplied' }
  | { view: 'run'; status: 'available' | 'loading' | 'empty' | 'error'; source: 'agent-command-evidence'; records: readonly CommandEvidenceHistoryRecord[]; error: string | null }
  | { view: 'run'; status: 'unavailable'; source: 'agent-command-evidence'; reason: 'execution-id-unavailable' | 'history-unavailable' | 'no-agent-record-supplied'; records: readonly CommandEvidenceHistoryRecord[]; error: string | null }
  | { view: 'results'; status: 'unavailable'; reason: 'reviewed-result-record-not-supported' }
  | { view: 'samples-analysis'; status: 'available'; source: 'agent-native'; records: readonly SampleAnalysisRecord[] }
  | { view: 'samples-analysis'; status: 'unavailable'; reason: 'no-agent-record-supplied' };

function setupView(records: readonly SetupRecord[] | undefined): Extract<BoardReadModel, { view: 'setup' }> {
  return records?.length ? { view: 'setup', status: 'available', source: 'agent-native', records } : { view: 'setup', status: 'unavailable', reason: 'no-agent-record-supplied' };
}

function experimentView(records: readonly ExperimentRecord[] | undefined): Extract<BoardReadModel, { view: 'experiment' }> {
  return records?.length ? { view: 'experiment', status: 'available', source: 'agent-native', records } : { view: 'experiment', status: 'unavailable', reason: 'no-agent-record-supplied' };
}

function planView(records: readonly PlanRecord[] | undefined): Extract<BoardReadModel, { view: 'plan' }> {
  return records?.length ? { view: 'plan', status: 'available', source: 'agent-native', records } : { view: 'plan', status: 'unavailable', reason: 'no-agent-record-supplied' };
}

function runView(run: CommandEvidenceView | undefined): Extract<BoardReadModel, { view: 'run' }> {
  if (!run) {
    return {
      view: 'run',
      source: 'agent-command-evidence',
      status: 'unavailable',
      reason: 'no-agent-record-supplied',
      records: [],
      error: null,
    };
  }
  if (run.status === 'unavailable') return { view: 'run', ...run };
  return { view: 'run', ...run };
}

function samplesAnalysisView(records: readonly SampleAnalysisRecord[] | undefined): Extract<BoardReadModel, { view: 'samples-analysis' }> {
  return records?.length
    ? { view: 'samples-analysis', status: 'available', source: 'agent-native', records }
    : { view: 'samples-analysis', status: 'unavailable', reason: 'no-agent-record-supplied' };
}

/** Builds six view-specific projections without deriving facts across their trust boundaries. */
export function createBoardReadModels(sources: BoardReadModelSources = {}): BoardReadModel[] {
  return [
    setupView(sources.setup),
    experimentView(sources.experiment),
    planView(sources.plan),
    runView(sources.run),
    { view: 'results', status: 'unavailable', reason: 'reviewed-result-record-not-supported' },
    samplesAnalysisView(sources.samplesAnalysis),
  ];
}
