export const scenarios = ['Ready', 'Running', 'Pausing', 'Paused', 'Held', 'Disconnected', 'Reconciliation required'];
export const devices = Object.freeze([
  Object.freeze({ id: 'feed', name: 'Feed pump', tag: 'P-101', reading: 1.2, unit: 'mL/min', role: 'Solvent feed → reactor', capability: 'Metered liquid delivery' }),
  Object.freeze({ id: 'reactor', name: 'Jacketed reactor', tag: 'R-201', reading: 42.6, unit: '°C', role: 'Reaction vessel · 50 mL', capability: 'Heat + stir · synthetic sample S-014' }),
  Object.freeze({ id: 'cooler', name: 'Recirculator', tag: 'C-301', reading: 18.0, unit: '°C', role: 'Jacket return → coolant reservoir', capability: 'Thermal conditioning' }),
  Object.freeze({ id: 'sample', name: 'Sample collector', tag: 'S-401', reading: 3, unit: 'vials', role: 'Reactor outlet → S-014', capability: 'Collect traceable aliquots' }),
]);
export const tasks = [
  'Locate the reactor and trace its feed, cooling loop and sample destination.',
  'Propose 55 °C, review it, then try 95 °C. Explain draft versus run revision.',
  'Request pause; explain what is pending before confirming a simulated observation.',
  'Load Disconnected, then Running. Explain why resume/retry remains blocked.',
  'Find sample S-014 and explain why its analytical evidence is not accepted.',
];
export function validateHeat(draft) {
  const errors = [];
  if (draft.unit !== '°C') errors.push('Unit must be °C; no implicit conversion.');
  if (String(draft.heat).trim() === '' || !Number.isFinite(Number(draft.heat))) errors.push('Temperature must be a finite number.');
  else if (Number(draft.heat) < 20 || Number(draft.heat) > 80) errors.push('Illustrative temperature limit: 20–80 °C.');
  return { valid: errors.length === 0, errors };
}
export function createState(now = new Date().toISOString(), id = 'session-' + now) {
  const state = {
    session: { id, startedAt: now, idea: 'ivoryos-scientific-workspace', spike: '001', simulation: true, schema: 1 },
    variant: 'baseline', mode: 'Apparatus', selected: 'reactor', scenario: 'Running',
    draft: { heat: '50', unit: '°C', revision: 1, reviewedRevision: null },
    run: { id: 'SIM-RUN-014', heat: 50, unit: '°C', revision: 'approved-fixture-r7', policy: 'illustrative-policy-v1', status: 'Running', progress: 40 },
    connected: true, reconcile: false, pending: null, abortDialog: false,
    task: null, completedTasks: [], notes: [], events: [], message: 'Synthetic snapshot loaded. No device connection.',
  };
  state.events.push(event(state, now, 'session', 'Session started', {}));
  return state;
}
function event(state, now, category, detail, data) {
  return { time: now, category, detail, variant: state.variant, scenario: state.scenario, observedStatus: state.run.status, mode: state.mode, data };
}
export function actionBlock(state, action) {
  if (state.pending) return 'A request is pending; observe or cancel it first.';
  if (!state.connected || state.reconcile) return 'Physical state uncertain. Reconciliation required; no replay or execution request.';
  if (action === 'start') {
    if (state.run.status !== 'Ready') return 'Start requires the Ready fixture.';
    if (!validateHeat(state.draft).valid || state.draft.reviewedRevision !== state.draft.revision) return 'Current draft needs valid illustrative policy review.';
  } else if (action === 'resume') {
    if (state.run.status !== 'Paused') return 'Resume requires an observed Paused state. Held requires separate review.';
  } else if (action === 'pause') {
    if (state.run.status !== 'Running') return 'Pause requires an observed Running state.';
  } else if (action === 'abort') {
    if (!['Running', 'Pausing', 'Paused', 'Held'].includes(state.run.status)) return 'No active fixture to abort.';
  } else if (action === 'retry') return 'Retry intentionally unavailable: no failed-step evidence or replay authorization.';
  else return 'Unknown action.';
  return '';
}
export function reduce(state, input, now = new Date().toISOString()) {
  const next = structuredClone(state);
  const log = (category, detail, data = {}) => next.events.push(event(next, now, category, detail, data));
  const blocked = reason => { next.message = reason; log('blocked', reason, { requested: input.type, action: input.action }); };
  switch (input.type) {
    case 'variant':
      if (!['baseline', 'canvas'].includes(input.value)) return state;
      next.variant = input.value; log('presentation', 'Comparison variant changed'); break;
    case 'mode':
      if (!['Apparatus', 'Design', 'Run', 'Evidence'].includes(input.value)) return state;
      next.mode = input.value; log('navigation', 'Workspace mode changed'); break;
    case 'select':
      if (!devices.some(device => device.id === input.value)) return state;
      next.selected = input.value; log('selection', 'Device selected', { device: input.value }); break;
    case 'draft':
      next.draft.heat = String(input.heat); next.draft.unit = input.unit;
      next.draft.revision++; next.draft.reviewedRevision = null;
      next.message = 'Draft changed only. Active run revision is unchanged.';
      log('draft', next.message, { ...next.draft }); break;
    case 'review': {
      const validation = validateHeat(next.draft);
      if (!validation.valid) { blocked(validation.errors.join(' ')); break; }
      next.draft.reviewedRevision = next.draft.revision;
      next.message = 'Illustrative draft review passed. This is NOT real Agent admission or execution authorization.';
      log('review', next.message, { revision: next.draft.revision }); break;
    }
    case 'scenario':
      if (!scenarios.includes(input.value)) return state;
      if (next.pending) log('request', 'Pending request invalidated by fixture change', { ...next.pending });
      next.pending = null; next.abortDialog = false; next.scenario = input.value;
      next.connected = input.value !== 'Disconnected';
      next.reconcile ||= ['Disconnected', 'Reconciliation required'].includes(input.value);
      next.run.status = next.reconcile && next.connected ? 'Reconciliation required' : input.value;
      next.message = next.reconcile ? 'Fixture changed. Uncertainty is latched; reconnecting does not reconcile physical state.' : 'Explicit scenario fixture loaded; not an observed device transition.';
      log('scenario', next.message, { loaded: input.value }); break;
    case 'reset':
      next.connected = true; next.reconcile = false; next.pending = null; next.abortDialog = false;
      next.scenario = 'Ready'; next.run.status = 'Ready'; next.run.progress = 0;
      next.message = 'NEW Ready fixture. Not recovery, reconciliation or replay of an interrupted run.';
      log('fixture', next.message); break;
    case 'request': {
      const reason = actionBlock(next, input.action);
      if (reason) { blocked(reason); break; }
      if (input.action === 'abort' && !input.confirmed) {
        next.abortDialog = true; log('confirmation', 'Abort confirmation opened'); break;
      }
      // Confirmation is accepted only after the model actually opened the dialog.
      if (input.action === 'abort' && !next.abortDialog) { blocked('Open the abort confirmation first.'); break; }
      next.abortDialog = false;
      next.pending = { action: input.action, requestedAt: now, candidate: input.action === 'start' ? { heat: Number(next.draft.heat), unit: next.draft.unit, revision: `draft-r${next.draft.revision}`, policy: 'illustrative-policy-v1' } : null };
      next.message = `${input.action} requested in simulation. No physical outcome confirmed.`;
      log('request', next.message, { ...next.pending }); break;
    }
    case 'cancelAbort': next.abortDialog = false; log('confirmation', 'Abort cancelled'); break;
    case 'cancelRequest':
      if (next.pending) log('request', 'Pending simulated request cancelled', { ...next.pending });
      next.pending = null; next.message = 'Request cancelled; observed run state unchanged.'; break;
    case 'observe':
      if (!next.pending) { blocked('No request to observe.'); break; }
      if (!next.connected || next.reconcile) { blocked('Cannot confirm an outcome while physical state is uncertain.'); break; }
      if (next.pending.candidate) Object.assign(next.run, next.pending.candidate);
      next.run.status = { start: 'Running', pause: 'Paused', resume: 'Running', abort: 'Aborted' }[next.pending.action];
      next.message = `SIMULATED observation: ${next.run.status}. Not a real physical outcome.`;
      log('observation', next.message, { request: next.pending, run: { ...next.run } });
      next.pending = null; break;
    case 'taskStart':
      if (next.task) { blocked('Finish the current task before starting another.'); break; }
      if (!tasks[input.index]) return state;
      next.task = { index: input.index, startedAt: now, startedVariant: next.variant, startedScenario: next.scenario };
      log('task', 'Task started', { ...next.task }); break;
    case 'taskFinish':
      if (!next.task) return state;
      next.completedTasks.push({ ...next.task, finishedAt: now, finishedVariant: next.variant, durationMs: Date.parse(now) - Date.parse(next.task.startedAt), outcome: input.outcome === 'blocked' ? 'blocked' : 'observer-marked-complete' });
      log('task', 'Task finished (self-report, not usability validation)', next.completedTasks.at(-1)); next.task = null; break;
    case 'note':
      if (!String(input.text).trim()) return state;
      next.notes.push({ time: now, variant: next.variant, scenario: next.scenario, task: next.task?.index ?? null, text: String(input.text).trim().slice(0,4000) });
      log('note', 'Observer note saved', { index: next.notes.length - 1 }); break;
    default: return state;
  }
  return next;
}
export function exportSession(state, now = new Date().toISOString()) {
  const variantDurationMs = { baseline: 0, canvas: 0 };
  for (let i = 0; i < state.events.length; i++) {
    const current = state.events[i];
    variantDurationMs[current.variant] += Math.max(0, Date.parse(state.events[i + 1]?.time ?? now) - Date.parse(current.time));
  }
  return structuredClone({ ...state.session, exportedAt: now, verdict: 'PENDING',
    summary: { eventCount: state.events.length, taskCount: state.completedTasks.length, stateCount: new Set(state.events.map(item => `${item.scenario}:${item.observedStatus}`)).size, blockedCount: state.events.filter(item => item.category === 'blocked').length, noteCount: state.notes.length, durationMs: Date.parse(now) - Date.parse(state.session.startedAt), variantDurationMs },
    completedTasks: state.completedTasks, activeTask: state.task, notes: state.notes, events: state.events,
    snapshot: { variant: state.variant, mode: state.mode, selected: state.selected, draft: state.draft, run: state.run, pending: state.pending, connected: state.connected, reconcile: state.reconcile },
  });
}
