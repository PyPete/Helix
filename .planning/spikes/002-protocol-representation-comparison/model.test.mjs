import test from 'node:test';
import assert from 'node:assert/strict';
import { createState, reduce, fixtures, loadFixture, validate, reorderLinear, insertStep, project, relations, bufferOf, hasUnsaved, exportSession } from './model.mjs';
const t = second => `2026-10-30T14:00:${String(second).padStart(2, '0')}.000Z`;
const initial = () => createState(t(0), 'model-test');

test('all fixtures are structurally valid with explicit quarantine/authority warnings', () => {
  for (const type of Object.keys(fixtures)) {
    const check = validate(loadFixture(type)); assert.equal(check.valid, true, check.errors.join(' '));
    assert.match(check.warnings.join(' '), /quarantined/); assert.match(check.warnings.join(' '), /Agent/);
  }
  assert.throws(() => fixtures.linear.nodes[0].name = 'mutated', TypeError);
});
test('step IDs, buffers, reviewed draft and selection survive representation switching', () => {
  let state = reduce(initial(), { type: 'buffer', fields: { name: 'Gentle heating', value: '55' } });
  state = reduce(state, { type: 'variant', value: 'graph' });
  assert.equal(bufferOf(state).value, '55'); assert.equal(state.selected, 'heat');
  state = reduce(state, { type: 'apply' }); state = reduce(state, { type: 'review' });
  const draft = structuredClone(state.draft); state = reduce(state, { type: 'variant', value: 'cards' });
  assert.deepEqual(state.draft, draft); assert.equal(state.draft.protocol.nodes[1].id, 'heat');
  assert.equal(state.draft.protocol.nodes[1].name, 'Gentle heating'); assert.equal(state.draft.reviewedRevision, state.draft.revision);
});
test('linear reorder regenerates correct dependencies without changing identities or source fixtures', () => {
  const source = loadFixture('linear'); const result = reorderLinear(source, 'heat', -1);
  assert.deepEqual(result.nodes.map(item => item.id), ['heat', 'feed', 'collect']);
  assert.deepEqual(result.edges.map(item => [item.from, item.to]), [['heat', 'feed'], ['feed', 'collect']]);
  assert.equal(validate(result).valid, true); assert.equal(source.nodes[0].id, 'feed');
  assert.throws(() => reorderLinear(source, 'feed', -1), /adjacent/);
  assert.throws(() => reorderLinear(loadFixture('branching'), 'heat', 1), /disabled/);
});
test('branch projections retain exclusive YES/NO paths and ANY merge rather than a flat sequence', () => {
  const projection = project(loadFixture('branching'));
  assert.deepEqual(projection.lanes.map(lane => lane.id), ['main', 'yes', 'no', 'merge']);
  assert.equal(projection.lanes[1].nodes[0].upstream[0].label, 'YES · hypothetical');
  assert.match(projection.lanes[2].nodes[0].upstream[0].label, /^NO/);
  assert.equal(projection.nodes.find(item => item.id === 'merge').kind, 'merge-any');
  assert.equal(projection.edges.filter(item => item.to === 'merge').every(item => item.label.startsWith('ANY')), true);
});
test('parallel projections preserve two independent lanes and ALL join dependencies', () => {
  const protocol = loadFixture('parallel'), projection = project(protocol);
  assert.equal(projection.nodes.find(item => item.id === 'join').kind, 'join-all');
  assert.equal(relations(protocol, 'join').upstream.length, 2);
  assert.equal(relations(protocol, 'heat').downstream[0].to, 'join');
  assert.equal(protocol.edges.some(item => item.from === 'heat' && item.to === 'feed'), false);
});
test('add on an explicit branch edge keeps condition and merge labels and unique stable IDs', () => {
  const original = loadFixture('branching');
  const inserted = insertStep(original, 'gate', 'heat', 'proposed-1', 'wait');
  assert.equal(inserted.nodes.find(item => item.id === 'proposed-1').lane, 'yes');
  assert.equal(inserted.edges.find(item => item.to === 'proposed-1').label, 'YES · hypothetical');
  const beforeMerge = insertStep(inserted, 'heat', 'merge', 'proposed-2', 'wait');
  assert.equal(beforeMerge.edges.find(item => item.from === 'proposed-2').label, 'ANY · yes path');
  assert.equal(validate(beforeMerge).valid, true);
  assert.throws(() => insertStep(original, 'gate', '', 'new', 'wait'), /explicit outgoing/);
});
test('add to parallel lane preserves ALL join, shared views and run isolation', () => {
  let state = reduce(initial(), { type: 'fixture', value: 'parallel' });
  state = reduce(state, { type: 'select', id: 'heat' }); state = reduce(state, { type: 'add', kind: 'wait', to: 'join' });
  assert.equal(state.selected, 'proposed-1'); assert.equal(validate(state.draft.protocol).valid, true);
  const edges = structuredClone(state.draft.protocol.edges); state = reduce(state, { type: 'variant', value: 'graph' });
  assert.deepEqual(state.draft.protocol.edges, edges); assert.equal(state.run.protocol.type, 'linear');
});
test('invalid numeric/units/name proposals cannot be reviewed or affect frozen run', () => {
  for (const value of ['', ' ', 'NaN', 'Infinity', 'abc', '19.9', '80.1']) {
    let state = reduce(initial(), { type: 'buffer', fields: { value } }); state = reduce(state, { type: 'apply' });
    assert.equal(validate(state.draft.protocol).valid, false, value);
    state = reduce(state, { type: 'review' }); assert.equal(state.draft.reviewedRevision, null); assert.equal(state.events.at(-1).category, 'blocked'); assert.equal(state.run.protocol.nodes[1].value, '50');
  }
  for (const fields of [{ unit: 'K' }, { name: ' ' }]) {
    const state = reduce(reduce(initial(), { type: 'buffer', fields }), { type: 'apply' }); assert.equal(validate(state.draft.protocol).valid, false);
  }
});
test('numeric policy covers all action kinds and boundary values without unit conversion', () => {
  for (const [kind, min, max, unit] of [['heat',20,80,'°C'],['feed',0.1,5,'mL/min'],['collect',0.1,5,'mL'],['wait',0,60,'min']]) {
    const protocol = loadFixture('linear'); protocol.nodes[1] = { ...protocol.nodes[1], kind, unit, value: String(min) };
    assert.equal(validate(protocol).valid, true); protocol.nodes[1].value = String(max); assert.equal(validate(protocol).valid, true);
    protocol.nodes[1].unit = 'wrong'; assert.equal(validate(protocol).valid, false);
  }
});
test('dangling dependencies, self-links, cycles and duplicate IDs are rejected', () => {
  const dangling = loadFixture('linear'); dangling.edges.push({ from: 'missing', to: 'heat', label: 'after' }); assert.match(validate(dangling).errors.join(' '), /Dangling/);
  const cyclic = loadFixture('linear'); cyclic.edges.push({ from: 'collect', to: 'feed', label: 'after' }); assert.match(validate(cyclic).errors.join(' '), /cycle/);
  const self = loadFixture('linear'); self.edges.push({ from: 'heat', to: 'heat', label: 'after' }); assert.equal(validate(self).valid, false);
  const duplicate = loadFixture('linear'); duplicate.nodes[1].id = 'feed'; assert.equal(validate(duplicate).valid, false);
});
test('ambiguous branch labels and missing ALL join cannot be silently accepted', () => {
  const branch = loadFixture('branching'); branch.edges[1].label = 'after'; assert.match(validate(branch).errors.join(' '), /YES and NO/);
  const parallel = loadFixture('parallel'); parallel.edges[2].label = 'ANY'; assert.match(validate(parallel).errors.join(' '), /ALL-dependency/);
});
test('dirty and unsaved fixture confirmation cancellation preserves buffers, edits and selection', () => {
  let state = reduce(initial(), { type: 'buffer', fields: { value: '55' } }); assert.equal(hasUnsaved(state), true);
  state = reduce(state, { type: 'fixture', value: 'branching' }); assert.equal(state.pendingFixture, 'branching');
  state = reduce(state, { type: 'cancelFixture' }); assert.equal(bufferOf(state).value, '55'); assert.equal(state.fixture, 'linear');
  state = reduce(state, { type: 'apply' }); const draft = structuredClone(state.draft);
  state = reduce(state, { type: 'fixture', value: 'parallel' }); state = reduce(state, { type: 'cancelFixture' }); assert.deepEqual(state.draft, draft);
  state = reduce(state, { type: 'fixture', value: 'parallel' }); state = reduce(state, { type: 'confirmFixture' });
  assert.equal(state.fixture, 'parallel'); assert.equal(state.draft.dirty, false); assert.deepEqual(state.buffers, {}); assert.equal(state.run.protocol.type, 'linear');
});
test('review is proposal revision bound, quarantined evidence never becomes accepted', () => {
  let state = reduce(initial(), { type: 'review' }); const reviewed = structuredClone(state.draft.lastReviewed);
  state = reduce(state, { type: 'buffer', fields: { value: '55' } }); state = reduce(state, { type: 'apply' });
  assert.equal(state.draft.reviewedRevision, null); assert.deepEqual(state.draft.lastReviewed, reviewed);
  assert.equal(state.draft.protocol.evidence.quality, 'quarantined'); assert.deepEqual(state.run, initial().run);
});
test('diagnostic probes reject malformed structure without contaminating the shared draft', () => {
  const source = initial(); const cycle = reduce(source, { type: 'probe', kind: 'cycle' });
  assert.equal(cycle.probe.check.valid, false); assert.deepEqual(cycle.draft, source.draft);
  assert.match(reduce(source, { type: 'probe', kind: 'dangling' }).probe.check.errors.join(' '), /Dangling/);
});
test('uncertainty remains explicit across reconnect, representation and fixture switches', () => {
  let state = reduce(initial(), { type: 'uncertainty', value: 'Disconnected · stale' });
  state = reduce(state, { type: 'uncertainty', value: 'Simulation snapshot only' });
  state = reduce(state, { type: 'variant', value: 'graph' }); state = reduce(state, { type: 'fixture', value: 'branching' });
  assert.equal(state.uncertainty, 'Reconciliation required'); assert.deepEqual(state.run, initial().run);
});
test('JSON export includes ISO provenance, semantic snapshot, task outcomes/counts and split durations', () => {
  let state = reduce(initial(), { type: 'taskStart', index: 0 }, t(1));
  state = reduce(state, { type: 'variant', value: 'graph' }, t(3));
  state = reduce(state, { type: 'fixture', value: 'branching' }, t(4));
  state = reduce(state, { type: 'note', text: '<script>text only</script>' }, t(5));
  state = reduce(state, { type: 'taskFinish', blocked: true }, t(6));
  const report = exportSession(state, t(8)); assert.equal(report.summary.taskCount, 1); assert.equal(report.summary.fixtureCount, 2);
  assert.deepEqual(report.summary.variantDurationMs, { cards: 3000, graph: 5000 });
  assert.deepEqual(report.summary.fixtureDurationMs, { linear: 4000, branching: 4000, parallel: 0 });
  assert.equal(report.completedTasks[0].durationMs, 5000); assert.equal(report.completedTasks[0].outcome, 'blocked');
  assert.ok(report.events.every(item => Number.isFinite(Date.parse(item.time)) && item.category && item.variant && item.fixture));
  assert.equal(report.semanticSnapshot.draft.protocol.type, 'branching'); assert.equal(report.verdict, 'PENDING');
  report.semanticSnapshot.run.protocol.nodes[0].name = 'changed'; assert.notEqual(state.run.protocol.nodes[0].name, 'changed');
});
