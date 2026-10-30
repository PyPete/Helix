const freeze = value => { Object.values(value).forEach(item => { if (item && typeof item === 'object') freeze(item); }); return Object.freeze(value); };
export const entities = freeze({
  'R-201': { id: 'R-201', kind: 'device', name: 'Thermal reactor', source: 'apparatus-fixture/r3', unit: '°C', quality: 'simulated snapshot' },
  'S-401': { id: 'S-401', kind: 'device', name: 'Sample collector', source: 'apparatus-fixture/r3', unit: 'mL', quality: 'simulated snapshot' },
  heat: { id: 'heat', kind: 'step', name: 'Heat reactor', source: 'protocol-fixture/r7', unit: '°C', quality: 'draft proposal' },
  collect: { id: 'collect', kind: 'step', name: 'Collect aliquot', source: 'protocol-fixture/r7', unit: 'mL', quality: 'draft proposal' },
  'S-014': { id: 'S-014', kind: 'sample', name: 'Aliquot S-014', source: 'sample-fixture/r1', unit: 'mL', quality: 'synthetic correlation only' },
  'RESULT-014': { id: 'RESULT-014', kind: 'result', name: 'Concentration evidence', source: 'synthetic-results.csv', unit: 'mg/L', quality: 'fixture-dependent' },
});
export const links = freeze([
  { from: 'R-201', to: 'heat', label: 'capability target · proposal' },
  { from: 'heat', to: 'collect', label: 'after heat · draft dependency' },
  { from: 'S-401', to: 'collect', label: 'collection target · proposal' },
  { from: 'collect', to: 'S-014', label: 'synthetic collection provenance' },
  { from: 'S-014', to: 'RESULT-014', label: 'sample correlation · no acceptance implied' },
]);
export const evidenceFixtures = freeze({
  quarantine: { id: 'RESULT-014', record: 'RESULT-014/quarantine-r1', sample: 'S-014', run: 'SIM-RUN-014', protocol: 'approved-fixture-r7', artifact: 'synthetic-results.csv', sourceRevision: 'CSV-Q-r1', unit: 'mg/L', quality: 'quarantined', reviewed: false, reason: 'Ambiguous source-unit mapping; no accepted result', rows: [{ time: '09:14', value: 12.4, quality: 'quarantined · unit mapping unresolved' }, { time: '09:16', value: 12.6, quality: 'quarantined · unit mapping unresolved' }] },
  reviewed: { id: 'RESULT-014', record: 'RESULT-014/reviewed-fixture-r2', sample: 'S-014', run: 'SIM-RUN-014', protocol: 'approved-fixture-r7', artifact: 'synthetic-reviewed-results.csv', sourceRevision: 'CSV-VALID-r2', unit: 'mg/L', quality: 'reviewed synthetic fixture', reviewed: true, reason: 'Separate pre-reviewed fixture with explicit units; not promotion of CSV-Q-r1', rows: [{ time: '09:14', value: 12.4, quality: 'reviewed synthetic fixture' }, { time: '09:16', value: 12.6, quality: 'reviewed synthetic fixture' }] },
});
export const tasks = [
  'Trace R-201 → heat → collect → S-014 → RESULT-014; explain source, units and quarantine.',
  'Trace result → sample → collection → reactor; compare discovery in each layout.',
  'Type 55 °C without saving; select evidence and return. Verify the pending field survives.',
  'Keep unsaved heat and step-name edits, switch layouts/cards, then save one and discard the other.',
  'Try 95 °C, blank or K; explain blocked UI review and the independent frozen 50 °C run.',
  'Inspect separate reviewed evidence fixture, then quarantine; explain why neither inspection promotes CSV-Q-r1.',
  'Disconnect, select another entity and reconnect; locate stale/reconciliation status. Export notes.',
];
export function relations(id) { return { upstream: links.filter(link => link.to === id), downstream: links.filter(link => link.from === id) }; }
export function trace(id) {
  const seen = new Set([id]);
  const walk = (current, direction) => links.filter(link => link[direction === 'forward' ? 'from' : 'to'] === current).forEach(link => { const next = link[direction === 'forward' ? 'to' : 'from']; if (!seen.has(next)) { seen.add(next); walk(next, direction); } });
  walk(id, 'forward'); walk(id, 'reverse'); return [...seen];
}
export function editableFields(state, id) {
  if (id === 'R-201') return { value: state.draft.heat.value, unit: state.draft.heat.unit };
  if (entities[id]?.kind === 'step') return { name: state.draft.labels[id] };
  return null;
}
export function bufferOf(state, id) { return state.buffers[id]?.fields ?? editableFields(state, id); }
export function unsavedIds(state) { return Object.keys(state.buffers).filter(id => JSON.stringify(state.buffers[id].fields) !== JSON.stringify(editableFields(state, id))); }
export function validate(state) {
  const errors = [], heat = state.draft.heat;
  if (!String(heat.value).trim() || !Number.isFinite(Number(heat.value))) errors.push('R-201: finite, nonblank heat required.');
  else if (Number(heat.value) < 20 || Number(heat.value) > 80) errors.push('R-201: illustrative heat limit 20–80 °C.');
  if (heat.unit !== '°C') errors.push('R-201: unit must be °C; no implicit conversion.');
  Object.entries(state.draft.labels).forEach(([id, name]) => { if (!String(name).trim()) errors.push(`${id}: nonblank step label required.`); });
  return { valid: !errors.length, errors, warnings: ['UI-only proposal checks, not scientific sequencing or Agent admission.', 'No capability, safety, lease, resource, budget, termination or reconciliation validation.'] };
}
const initialDraft = () => ({ heat: { value: '50', unit: '°C' }, labels: { heat: 'Heat reactor', collect: 'Collect aliquot' }, revision: 1, reviewedRevision: null });
export function createState(now = new Date().toISOString(), id = 'session-' + now) {
  const state = { session: { id, startedAt: now, spike: '003', idea: 'ivoryos-scientific-workspace', schema: 1, simulation: true }, layout: 'contextual', representation: 'graph', selected: 'R-201', panelSelections: { device: 'R-201', step: 'heat', evidence: 'RESULT-014' }, draft: initialDraft(), buffers: {}, run: { id: 'SIM-RUN-014', revision: 'approved-fixture-r7', heat: { value: '50', unit: '°C' }, labels: { heat: 'Heat reactor', collect: 'Collect aliquot' } }, scenario: 'Running', uncertainty: 'Simulation snapshot only', evidenceFixture: 'quarantine', evidence: structuredClone(evidenceFixtures.quarantine), pendingReset: false, task: null, completedTasks: [], notes: [], events: [], message: 'Inspection only. No Agent connection or execution commands.' };
  state.events.push(eventOf(state, now, 'session', state.message)); return state;
}
function eventOf(state, time, category, detail, data = {}) { return { time, category, layout: state.layout, representation: state.representation, entity: state.selected, scenario: state.scenario, uncertainty: state.uncertainty, evidenceFixture: state.evidenceFixture, revision: state.draft.revision, detail, data }; }
export function reduce(state, action, now = new Date().toISOString()) {
  const next = structuredClone(state), id = action.id ?? next.selected;
  const log = (category, detail, data = {}) => { next.message = detail; next.events.push(eventOf(next, now, category, detail, data)); };
  switch (action.type) {
    case 'select': if (!entities[id]) return state; next.selected = id; next.panelSelections[['sample','result'].includes(entities[id].kind) ? 'evidence' : entities[id].kind] = id; log('selection', 'Context changed; no auto-save, discard or review.', { id }); break;
    case 'layout': if (!['contextual','permanent'].includes(action.value)) return state; next.layout = action.value; log('layout', 'Layout changed; all per-entity buffers preserved.'); break;
    case 'representation': if (!['graph','cards'].includes(action.value)) return state; next.representation = action.value; log('representation', 'Canonical identities and control-flow links unchanged.'); break;
    case 'buffer': {
      const fields = editableFields(next, id); if (!fields) return state;
      next.buffers[id] ??= { fields, baseRevision: next.draft.revision };
      Object.entries(action.fields).forEach(([key,value]) => { if (Object.hasOwn(fields, key)) next.buffers[id].fields[key] = String(value).slice(0,100); });
      log('editor', 'Pending fields retained for entity; not saved or reviewed.', { id, buffer: next.buffers[id] }); break;
    }
    case 'save': {
      const buffer = next.buffers[id]; if (!buffer || !unsavedIds(next).includes(id)) { log('blocked', 'No pending change for this entity.'); break; }
      // Distinct entities edit distinct canonical fields; unrelated revisions are safely rebased.
      if (id === 'R-201') next.draft.heat = { ...buffer.fields };
      else if (entities[id]?.kind === 'step') next.draft.labels[id] = buffer.fields.name;
      else return state;
      delete next.buffers[id]; next.draft.revision++; next.draft.reviewedRevision = null;
      log('draft', validate(next).valid ? 'Saved draft only; frozen run unchanged. UI review required.' : 'Invalid proposal saved visibly; UI review blocked. Frozen run unchanged.', { id }); break;
    }
    case 'discard': delete next.buffers[id]; log('editor', 'Discarded unsaved fields for this entity only; saved draft unchanged.', { id }); break;
    case 'review': {
      const check = validate(next);
      if (unsavedIds(next).length || !check.valid) log('blocked', unsavedIds(next).length ? 'Save or discard all pending entity fields before UI-only review.' : check.errors.join(' '));
      else { next.draft.reviewedRevision = next.draft.revision; log('review', 'UI-only proposal review recorded; NOT scientific approval or Agent authorization.'); } break;
    }
    case 'evidence': if (!evidenceFixtures[action.value]) return state; next.evidenceFixture = action.value; next.evidence = structuredClone(evidenceFixtures[action.value]); log('fixture', 'Explicit independent evidence fixture loaded; no promotion, edits lost or campaign decision.', { record: next.evidence.record }); break;
    case 'reset': if (next.draft.revision > 1 || unsavedIds(next).length) { next.pendingReset = true; log('confirmation', 'Replacing edited draft requires confirmation.'); } else log('fixture', 'Draft already matches initial fixture.'); break;
    case 'cancelReset': next.pendingReset = false; log('confirmation', 'Replacement cancelled; all drafts and buffers retained.'); break;
    case 'confirmReset': if (!next.pendingReset) return state; next.draft = initialDraft(); next.buffers = {}; next.pendingReset = false; log('fixture', 'Draft fixture explicitly replaced; run, evidence and observations retained.'); break;
    case 'scenario': if (!['Running','Paused','Held','Disconnected','Reconciliation'].includes(action.value)) return state; next.scenario = action.value; if (action.value === 'Disconnected') next.uncertainty = 'Disconnected · stale'; else if (action.value === 'Reconciliation' || next.uncertainty !== 'Simulation snapshot only') next.uncertainty = 'Reconciliation required · snapshots stale'; log('uncertainty', 'Simulated status snapshot only. Reconnect never reconciles or permits replay.'); break;
    case 'taskStart': if (next.task) { log('blocked', 'Finish current task first.'); break; } if (!tasks[action.index]) return state; next.task = { index: action.index, startedAt: now, layout: next.layout, representation: next.representation, scenario: next.scenario }; log('task', 'Observer timed attempt started.'); break;
    case 'taskFinish': if (!next.task) return state; next.completedTasks.push({ ...next.task, finishedAt: now, durationMs: Date.parse(now)-Date.parse(next.task.startedAt), finishedLayout: next.layout, finishedRepresentation: next.representation, outcome: action.blocked ? 'blocked' : 'observer-marked-complete' }); next.task = null; log('task', 'Observer outcome recorded, not validated success.'); break;
    case 'note': if (!String(action.text).trim()) return state; next.notes.push({ time: now, layout: next.layout, representation: next.representation, entity: next.selected, scenario: next.scenario, text: String(action.text).trim().slice(0,4000) }); log('note', 'Observer note saved locally.'); break;
    default: return state;
  }
  return next;
}
export function exportSession(state, now = new Date().toISOString()) {
  const layoutDurationMs = { contextual: 0, permanent: 0 };
  state.events.forEach((event,index) => { layoutDurationMs[event.layout] += Math.max(0,Date.parse(state.events[index+1]?.time ?? now)-Date.parse(event.time)); });
  return structuredClone({ ...state.session, exportedAt: now, verdict: 'PENDING', summary: { eventCount: state.events.length, noteCount: state.notes.length, taskCount: state.completedTasks.length, entityCount: new Set(state.events.map(event => event.entity)).size, scenarioCount: new Set(state.events.map(event => event.scenario)).size, blockedCount: state.events.filter(event => event.category === 'blocked').length, durationMs: Date.parse(now)-Date.parse(state.session.startedAt), layoutDurationMs }, events: state.events, notes: state.notes, completedTasks: state.completedTasks, activeTask: state.task, semanticSnapshot: { entities, links, selected: state.selected, panelSelections: state.panelSelections, draft: state.draft, buffers: state.buffers, unsavedIds: unsavedIds(state), validation: validate(state), run: state.run, scenario: state.scenario, uncertainty: state.uncertainty, evidence: state.evidence, evidenceFixture: state.evidenceFixture, pendingReset: state.pendingReset, layout: state.layout, representation: state.representation } });
}
