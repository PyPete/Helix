export const specs = Object.freeze({
  feed: Object.freeze({ label: 'Meter feed', value: '1.2', unit: 'mL/min', min: 0.1, max: 5, device: 'P-101' }),
  heat: Object.freeze({ label: 'Heat reactor', value: '50', unit: '°C', min: 20, max: 80, device: 'R-201' }),
  collect: Object.freeze({ label: 'Collect S-014', value: '1', unit: 'mL', min: 0.1, max: 5, device: 'S-401' }),
  wait: Object.freeze({ label: 'Wait for review', value: '5', unit: 'min', min: 0, max: 60, device: 'Agent wait (proposal)' }),
});
export const tasks = [
  'Linear: select heat, propose 55 °C and rename it; toggle views and verify the same identity.',
  'Linear: move a step earlier/later and explain how its dependencies changed; run remains frozen.',
  'Branching: locate the hypothetical reviewed-result gate, YES/NO paths and any-one-path merge.',
  'Parallel: identify both independent lanes and the ALL-dependencies join; add a wait on one path.',
  'Try 95 °C or K, then inspect diagnostics; a quarantined CSV cannot authorize a branch decision.',
  'Edit a draft, request a different fixture, cancel replacement and verify your edits survived.',
];
function freeze(value) { Object.values(value).forEach(item => { if (item && typeof item === 'object') freeze(item); }); return Object.freeze(value); }
const node = (id, kind, lane = 'main', name = specs[kind]?.label ?? kind) => ({ id, kind, lane, name, ...(specs[kind] ? { value: specs[kind].value, unit: specs[kind].unit } : {}) });
const edge = (from, to, label = 'after') => ({ from, to, label });
const evidence = { sample: 'S-014', sourceRun: 'SIM-RUN-014', sourceRevision: 'approved-fixture-r7', artifact: 'synthetic-results.csv (fixture only)', result: 'RESULT-014', unit: 'mg/L', quality: 'quarantined', reason: 'CSV unit mapping unreviewed; no accepted result' };
export const fixtures = freeze({
  linear: { type: 'linear', title: 'Thermal aliquot', lanes: [{ id: 'main', title: 'Linear proposal · ordered dependencies', meaning: 'One dependency chain, not observed execution' }], nodes: [node('feed', 'feed'), node('heat', 'heat'), node('collect', 'collect')], edges: [edge('feed', 'heat'), edge('heat', 'collect')], evidence },
  branching: {
    type: 'branching', title: 'Evidence-gated follow-up',
    lanes: [{ id: 'main', title: 'Before the gate', meaning: 'Synthetic sample and hypothetical condition' }, { id: 'yes', title: 'Hypothetical YES · reviewed result', meaning: 'Exclusive path; NOT supported by the quarantined fixture' }, { id: 'no', title: 'NO / unavailable · hold for review', meaning: 'Exclusive alternative, not the next sequential step' }, { id: 'merge', title: 'Merge · ANY one selected path', meaning: 'XOR merge; never requires both exclusive paths' }],
    nodes: [node('collect', 'collect'), node('gate', 'condition', 'main', 'Reviewed result available?'), node('heat', 'heat', 'yes', 'Propose follow-up heat'), node('hold', 'wait', 'no', 'Hold for evidence review'), node('merge', 'merge-any', 'merge', 'Record selected path')],
    edges: [edge('collect', 'gate', 'sample → hypothetical check'), edge('gate', 'heat', 'YES · hypothetical'), edge('gate', 'hold', 'NO · result unavailable'), edge('heat', 'merge', 'ANY · yes path'), edge('hold', 'merge', 'ANY · no path')], evidence,
  },
  parallel: {
    type: 'parallel', title: 'Independent preparation lanes',
    lanes: [{ id: 'main', title: 'Shared preparation', meaning: 'Fork admits both conceptual lanes, not hardware authorization' }, { id: 'thermal', title: 'Lane A · reactor R-201', meaning: 'Independent dependency path' }, { id: 'feedlane', title: 'Lane B · feed pump P-101', meaning: 'Independent dependency path; no implied total order' }, { id: 'join', title: 'Join · ALL dependencies', meaning: 'Both lanes must reach the join before collection' }],
    nodes: [node('fork', 'fork-all', 'main', 'Prepare in parallel'), node('heat', 'heat', 'thermal'), node('feed', 'feed', 'feedlane'), node('join', 'join-all', 'join', 'Wait for both lanes'), node('collect', 'collect', 'join')],
    edges: [edge('fork', 'heat', 'ALL · lane A'), edge('fork', 'feed', 'ALL · lane B'), edge('heat', 'join', 'ALL · thermal ready'), edge('feed', 'join', 'ALL · feed ready'), edge('join', 'collect')], evidence,
  },
});
export function loadFixture(type) { if (!Object.hasOwn(fixtures, type)) throw new Error('Unknown fixture.'); return structuredClone(fixtures[type]); }
export function validate(protocol) {
  const errors = [], warnings = ['S-014 / RESULT-014 is quarantined. No analytical evidence is accepted; this check is not scientific or Agent validation.'];
  const ids = new Set(protocol.nodes.map(item => item.id));
  if (!protocol.nodes.length || ids.size !== protocol.nodes.length) errors.push('Step identities must be nonempty and unique.');
  for (const item of protocol.nodes) {
    if (!item.id || !String(item.name).trim()) errors.push(`${item.id || 'Step'}: identity and name required.`);
    if (!protocol.lanes.some(lane => lane.id === item.lane)) errors.push(`${item.id}: unknown lane.`);
    const spec = specs[item.kind];
    if (spec) {
      if (item.unit !== spec.unit) errors.push(`${item.id}: unit must be ${spec.unit}; no implicit conversion.`);
      if (String(item.value).trim() === '' || !Number.isFinite(Number(item.value))) errors.push(`${item.id}: parameter must be finite and nonblank.`);
      else if (Number(item.value) < spec.min || Number(item.value) > spec.max) errors.push(`${item.id}: illustrative limit ${spec.min}–${spec.max} ${spec.unit}.`);
    } else if (!['condition', 'merge-any', 'fork-all', 'join-all'].includes(item.kind)) errors.push(`${item.id}: unsupported kind.`);
  }
  const seenEdges = new Set();
  for (const item of protocol.edges) {
    if (!ids.has(item.from) || !ids.has(item.to)) errors.push(`Dangling dependency: ${item.from} → ${item.to}.`);
    if (item.from === item.to) errors.push(`Self dependency: ${item.from}.`);
    if (!item.label?.trim()) errors.push('Every dependency needs an explicit label.');
    const key = `${item.from}:${item.to}`;
    if (seenEdges.has(key)) errors.push(`Duplicate dependency: ${key}.`); seenEdges.add(key);
  }
  const visiting = new Set(), visited = new Set();
  const visit = id => {
    if (visiting.has(id)) { errors.push('Dependency cycle detected; no execution order inferred.'); return; }
    if (visited.has(id)) return;
    visiting.add(id);
    protocol.edges.filter(item => item.from === id && ids.has(item.to)).forEach(item => visit(item.to));
    visiting.delete(id); visited.add(id);
  };
  protocol.nodes.forEach(item => visit(item.id));
  if (protocol.type === 'linear') {
    const expected = protocol.nodes.slice(1).map((item, index) => `${protocol.nodes[index].id}:${item.id}`);
    if (protocol.edges.length !== expected.length || expected.some(key => !seenEdges.has(key))) errors.push('Linear proposal must preserve one explicit ordered dependency chain. Use linear reorder, not a flattened DAG.');
  }
  for (const item of protocol.nodes) {
    const outgoing = protocol.edges.filter(link => link.from === item.id);
    const incoming = protocol.edges.filter(link => link.to === item.id);
    if (item.kind === 'condition' && (outgoing.length !== 2 || !outgoing.some(link => link.label.startsWith('YES')) || !outgoing.some(link => link.label.startsWith('NO')))) errors.push(`${item.id}: condition needs labeled YES and NO exits.`);
    if (item.kind === 'fork-all' && (outgoing.length !== 2 || outgoing.some(link => !link.label.startsWith('ALL')))) errors.push(`${item.id}: fork needs two ALL-lane exits.`);
    if (item.kind === 'merge-any' && (incoming.length !== 2 || incoming.some(link => !link.label.startsWith('ANY')))) errors.push(`${item.id}: merge needs two explicitly ANY-path inputs.`);
    if (item.kind === 'join-all' && (incoming.length !== 2 || incoming.some(link => !link.label.startsWith('ALL')))) errors.push(`${item.id}: join needs two explicitly ALL-dependency inputs.`);
  }
  if (protocol.type === 'branching') warnings.push('Reviewed result available? is hypothetical. No YES path is selected or authorized by this fixture.');
  if (protocol.type === 'parallel') warnings.push('Parallel dependency lanes are proposals only. Device/resource/lease conflicts require real Agent validation.');
  warnings.push('Dependencies are UI structure only; scientific sequencing, capabilities, safety, leases, budgets and termination are not validated here.');
  return { valid: errors.length === 0, errors: [...new Set(errors)], warnings };
}
export function relations(protocol, id) {
  return { upstream: protocol.edges.filter(item => item.to === id), downstream: protocol.edges.filter(item => item.from === id) };
}
export function project(protocol) {
  // Both views receive the very same labeled dependencies; lane order never implies a total order.
  return { lanes: protocol.lanes.map(lane => ({ ...lane, nodes: protocol.nodes.filter(item => item.lane === lane.id).map(item => ({ ...item, ...relations(protocol, item.id) })) })), nodes: structuredClone(protocol.nodes), edges: structuredClone(protocol.edges) };
}
export function reorderLinear(protocol, id, direction) {
  if (protocol.type !== 'linear') throw new Error('Linear reorder is disabled for branching/parallel; preserve explicit paths.');
  if (!validate(protocol).valid) throw new Error('Repair invalid structure before reordering.');
  if (![-1, 1].includes(direction)) throw new Error('Direction must be earlier or later.');
  const next = structuredClone(protocol), index = next.nodes.findIndex(item => item.id === id), target = index + direction;
  if (index < 0 || target < 0 || target >= next.nodes.length) throw new Error('No adjacent step in that direction.');
  [next.nodes[index], next.nodes[target]] = [next.nodes[target], next.nodes[index]];
  next.edges = next.nodes.slice(1).map((item, i) => edge(next.nodes[i].id, item.id));
  return next;
}
export function insertStep(protocol, after, to, id, kind) {
  if (!specs[kind] || protocol.nodes.some(item => item.id === id)) throw new Error('Invalid kind or duplicate step identity.');
  const next = structuredClone(protocol), source = next.nodes.find(item => item.id === after);
  if (!source) throw new Error('Select an existing insertion source.');
  const outgoing = next.edges.filter(item => item.from === after);
  const selected = outgoing.find(item => item.to === to);
  if (outgoing.length && !selected) throw new Error('Choose an explicit outgoing dependency path; branches cannot be flattened.');
  if (!outgoing.length && to) throw new Error('Terminal step has no chosen outgoing dependency.');
  const target = next.nodes.find(item => item.id === to);
  const lane = ['condition', 'fork-all'].includes(source.kind) ? target?.lane : source.lane;
  const added = node(id, kind, lane ?? source.lane);
  const index = next.nodes.findIndex(item => item.id === after);
  next.nodes.splice(index + 1, 0, added);
  if (selected) {
    next.edges = next.edges.filter(item => item !== selected);
    // Gate/fork labels stay at source; merge/join labels stay at target.
    next.edges.push(edge(after, id, ['condition', 'fork-all'].includes(source.kind) ? selected.label : 'after'), edge(id, to, ['merge-any', 'join-all'].includes(target.kind) ? selected.label : 'after'));
  } else next.edges.push(edge(after, id));
  const check = validate(next); if (!check.valid) throw new Error(check.errors.join(' '));
  return next;
}
export function bufferOf(state, id = state.selected) {
  const item = state.draft.protocol.nodes.find(item => item.id === id);
  return state.buffers[id] ?? { name: item.name, value: item.value ?? '', unit: item.unit ?? '' };
}
export function hasUnsaved(state) {
  return state.draft.protocol.nodes.some(item => {
    const buffer = state.buffers[item.id];
    return buffer && (buffer.name !== item.name || (specs[item.kind] && (buffer.value !== item.value || buffer.unit !== item.unit)));
  });
}
export function createState(now = new Date().toISOString(), id = 'session-' + now) {
  const state = { session: { id, startedAt: now, idea: 'ivoryos-scientific-workspace', spike: '002', schema: 1, simulation: true }, variant: 'cards', fixture: 'linear', selected: 'heat',
    draft: { protocol: loadFixture('linear'), revision: 1, reviewedRevision: null, lastReviewed: null, dirty: false },
    run: { id: 'SIM-RUN-014', status: 'Running (frozen fixture)', revision: 'approved-fixture-r7', protocol: loadFixture('linear') },
    uncertainty: 'Simulation snapshot only', buffers: {}, pendingFixture: null, nextId: 1, probe: null,
    task: null, completedTasks: [], notes: [], events: [], message: 'Draft proposals only. No Agent/API/hardware connection.' };
  state.events.push(logEvent(state, now, 'session', state.message)); return state;
}
function logEvent(state, time, category, detail, data = {}) { return { time, category, variant: state.variant, fixture: state.fixture, selected: state.selected, revision: state.draft.revision, detail, data }; }
export function reduce(state, input, now = new Date().toISOString()) {
  const next = structuredClone(state);
  const log = (category, detail, data = {}) => next.events.push(logEvent(next, now, category, detail, data));
  const block = detail => { next.message = detail; log('blocked', detail, { type: input.type }); };
  const mutate = protocol => { next.draft.protocol = protocol; next.draft.revision++; next.draft.dirty = true; next.draft.reviewedRevision = null; next.probe = null; };
  const replace = type => { next.fixture = type; next.draft = { protocol: loadFixture(type), revision: 1, reviewedRevision: null, lastReviewed: null, dirty: false }; next.buffers = {}; next.selected = next.draft.protocol.nodes[0].id; next.pendingFixture = null; next.probe = null; next.message = 'Explicit fixture replacement. Frozen run and log retained.'; log('fixture', next.message); };
  switch (input.type) {
    case 'variant': if (!['cards', 'graph'].includes(input.value)) return state; next.variant = input.value; log('presentation', 'Representation changed; semantic draft preserved'); break;
    case 'select': if (!next.draft.protocol.nodes.some(item => item.id === input.id)) return state; next.selected = input.id; log('selection', 'Step selected', { id: input.id }); break;
    case 'buffer': next.buffers[next.selected] = { ...bufferOf(next), ...input.fields }; log('editor', 'Unsaved proposal buffer changed', { id: next.selected, buffer: next.buffers[next.selected] }); break;
    case 'apply': {
      const protocol = structuredClone(next.draft.protocol), item = protocol.nodes.find(item => item.id === next.selected), buffer = bufferOf(next);
      item.name = buffer.name.trim().slice(0, 100);
      if (specs[item.kind]) { item.value = buffer.value; item.unit = buffer.unit; }
      mutate(protocol); delete next.buffers[next.selected];
      next.message = validate(protocol).valid ? 'Draft edit saved. Frozen run unchanged; UI review required.' : 'Invalid proposal retained visibly; UI review blocked. Frozen run unchanged.';
      log('draft', next.message, { id: item.id, node: item }); break;
    }
    case 'review': {
      const check = validate(next.draft.protocol);
      if (hasUnsaved(next)) { block('Save or restore editor buffers before UI-only review.'); break; }
      if (!check.valid) { block(check.errors.join(' ')); break; }
      next.draft.reviewedRevision = next.draft.revision; next.draft.lastReviewed = structuredClone(next.draft.protocol);
      next.message = 'UI structure/parameter review recorded, NOT scientific approval, Agent admission or execution authorization.';
      log('review', next.message, { revision: next.draft.revision, warnings: check.warnings }); break;
    }
    case 'restoreBuffer': delete next.buffers[next.selected]; log('editor', 'Unsaved buffer restored to current draft'); break;
    case 'reorder':
      try { mutate(reorderLinear(next.draft.protocol, next.selected, input.direction)); next.message = 'Draft chain reordered; dependencies regenerated, identities and run unchanged.'; log('draft', next.message, { order: next.draft.protocol.nodes.map(item => item.id) }); } catch (error) { block(error.message); } break;
    case 'add':
      try { const id = `proposed-${next.nextId}`; mutate(insertStep(next.draft.protocol, next.selected, input.to, id, input.kind)); next.nextId++; next.selected = id; next.message = 'New draft step inserted on the chosen dependency path only.'; log('draft', next.message, { id, to: input.to, kind: input.kind }); } catch (error) { block(error.message); } break;
    case 'fixture':
      if (!Object.hasOwn(fixtures, input.value) || input.value === next.fixture) return state;
      if (next.draft.dirty || hasUnsaved(next)) { next.pendingFixture = input.value; log('confirmation', 'Dirty fixture replacement requires confirmation', { target: input.value }); }
      else replace(input.value); break;
    case 'cancelFixture': next.pendingFixture = null; log('confirmation', 'Fixture replacement cancelled; all draft/buffer state retained'); break;
    case 'confirmFixture': if (next.pendingFixture) replace(next.pendingFixture); break;
    case 'uncertainty':
      if (!['Simulation snapshot only', 'Disconnected · stale', 'Reconciliation required'].includes(input.value)) return state;
      next.uncertainty = next.uncertainty === 'Simulation snapshot only' ? input.value : input.value === 'Disconnected · stale' ? input.value : 'Reconciliation required';
      log('uncertainty', 'Status fixture changed; reconnect never reconciles or authorizes execution', { state: next.uncertainty }); break;
    case 'probe': {
      const protocol = structuredClone(next.draft.protocol);
      if (input.kind === 'cycle') protocol.edges.push(edge(protocol.nodes.at(-1).id, protocol.nodes[0].id, 'diagnostic cycle'));
      else if (input.kind === 'dangling') protocol.edges.push(edge('MISSING-STEP', protocol.nodes[0].id, 'diagnostic missing dependency'));
      else { next.probe = null; log('diagnostic', 'Diagnostic overlay cleared'); break; }
      next.probe = { kind: input.kind, check: validate(protocol) }; log('diagnostic', 'Invalid structural probe rejected; canonical draft untouched', { ...next.probe }); break;
    }
    case 'taskStart': if (next.task) { block('Finish the current task first.'); break; } if (!tasks[input.index]) return state; next.task = { index: input.index, startedAt: now, variant: next.variant, fixture: next.fixture }; log('task', 'Timed attempt started', next.task); break;
    case 'taskFinish': if (!next.task) return state; next.completedTasks.push({ ...next.task, finishedAt: now, finishedVariant: next.variant, finishedFixture: next.fixture, durationMs: Date.parse(now) - Date.parse(next.task.startedAt), outcome: input.blocked ? 'blocked' : 'observer-marked-complete' }); log('task', 'Attempt finished; self-report, not usability validation', next.completedTasks.at(-1)); next.task = null; break;
    case 'note': if (!String(input.text).trim()) return state; next.notes.push({ time: now, variant: next.variant, fixture: next.fixture, task: next.task?.index ?? null, text: String(input.text).trim().slice(0, 4000) }); log('note', 'Observer note saved'); break;
    default: return state;
  }
  return next;
}
export function exportSession(state, now = new Date().toISOString()) {
  const variantDurationMs = { cards: 0, graph: 0 }, fixtureDurationMs = { linear: 0, branching: 0, parallel: 0 };
  state.events.forEach((event, i) => { const duration = Math.max(0, Date.parse(state.events[i + 1]?.time ?? now) - Date.parse(event.time)); variantDurationMs[event.variant] += duration; fixtureDurationMs[event.fixture] += duration; });
  return structuredClone({ ...state.session, exportedAt: now, verdict: 'PENDING', summary: { eventCount: state.events.length, taskCount: state.completedTasks.length, noteCount: state.notes.length, fixtureCount: new Set(state.events.map(item => item.fixture)).size, blockedCount: state.events.filter(item => item.category === 'blocked').length, durationMs: Date.parse(now) - Date.parse(state.session.startedAt), variantDurationMs, fixtureDurationMs },
    events: state.events, notes: state.notes, completedTasks: state.completedTasks, activeTask: state.task,
    semanticSnapshot: { variant: state.variant, fixture: state.fixture, selected: state.selected, draft: state.draft, buffers: state.buffers, validation: validate(state.draft.protocol), projection: project(state.draft.protocol), run: state.run, uncertainty: state.uncertainty, pendingFixture: state.pendingFixture, probe: state.probe } });
}
