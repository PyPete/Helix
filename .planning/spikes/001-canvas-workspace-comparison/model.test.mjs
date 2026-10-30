import test from 'node:test';
import assert from 'node:assert/strict';
import { createState, reduce, validateHeat, actionBlock, exportSession, devices, scenarios } from './model.mjs';
const time = second => `2026-10-30T12:00:${String(second).padStart(2, '0')}.000Z`;
const initial = () => createState(time(0), 'test-session');

test('shared fixture identities, units and semantics survive comparison toggle', () => {
  const state = reduce(reduce(initial(), { type: 'select', value: 'sample' }), { type: 'draft', heat: '55', unit: '°C' });
  const changed = reduce(state, { type: 'variant', value: 'canvas' });
  assert.equal(changed.selected, 'sample'); assert.deepEqual(changed.run, state.run); assert.deepEqual(changed.draft, state.draft);
  assert.equal(devices.length, 4); assert.equal(devices[1].unit, '°C'); assert.equal(initial().variant, 'baseline');
});
test('deterministic heat policy rejects invalid units, empty, nonfinite and out-of-limit values', () => {
  for (const heat of ['', ' ', 'NaN', 'Infinity', 'abc', '19.9', '80.1']) assert.equal(validateHeat({ heat, unit: '°C' }).valid, false, heat);
  for (const heat of ['20', '55', '80']) assert.equal(validateHeat({ heat, unit: '°C' }).valid, true);
  assert.equal(validateHeat({ heat: '55', unit: 'K' }).valid, false);
});
test('review is revision bound; draft edits never mutate admitted run', () => {
  let state = reduce(initial(), { type: 'review' });
  assert.equal(state.draft.reviewedRevision, 1);
  state = reduce(state, { type: 'draft', heat: '95', unit: '°C' });
  assert.equal(state.draft.reviewedRevision, null); assert.equal(state.run.heat, 50);
  state = reduce(state, { type: 'review' }); assert.equal(state.events.at(-1).category, 'blocked');
});
test('requested pause is not an observed Paused outcome', () => {
  const state = reduce(initial(), { type: 'request', action: 'pause' }, time(1));
  assert.equal(state.run.status, 'Running'); assert.equal(state.pending.action, 'pause');
  assert.ok(actionBlock(state, 'resume'));
  const observed = reduce(state, { type: 'observe' }, time(2));
  assert.equal(observed.run.status, 'Paused'); assert.equal(observed.pending, null);
  assert.equal(observed.events.at(-1).category, 'observation');
});
test('all uncertain states block start/resume/retry and observation', () => {
  for (const scenario of ['Disconnected', 'Reconciliation required']) {
    let state = reduce(initial(), { type: 'request', action: 'pause' });
    state = reduce(state, { type: 'scenario', value: scenario });
    assert.equal(state.pending, null);
    for (const action of ['start', 'resume', 'retry', 'abort']) assert.ok(actionBlock(state, action));
    state = reduce(state, { type: 'observe' }); assert.equal(state.events.at(-1).category, 'blocked');
    state = reduce(state, { type: 'scenario', value: 'Running' });
    assert.equal(state.reconcile, true); assert.equal(state.run.status, 'Reconciliation required'); assert.ok(actionBlock(state, 'resume'));
  }
});
test('Held and Pausing do not admit resume; retry never silently replays', () => {
  for (const scenario of ['Held', 'Pausing']) {
    const state = reduce(initial(), { type: 'scenario', value: scenario });
    assert.ok(actionBlock(state, 'resume')); assert.ok(actionBlock(state, 'retry'));
  }
  assert.equal(scenarios.length, 7);
});
test('start snapshots reviewed draft at request; later edits cannot alter candidate', () => {
  let state = reduce(initial(), { type: 'reset' });
  assert.ok(actionBlock(state, 'start'));
  state = reduce(state, { type: 'review' }); state = reduce(state, { type: 'request', action: 'start' });
  assert.equal(state.run.status, 'Ready');
  state = reduce(state, { type: 'draft', heat: '95', unit: '°C' }); state = reduce(state, { type: 'observe' });
  assert.equal(state.run.status, 'Running'); assert.equal(state.run.heat, 50); assert.equal(state.draft.heat, '95');
});
test('abort requires explicit confirmation then separate observation', () => {
  let state = reduce(initial(), { type: 'request', action: 'abort', confirmed: true });
  assert.equal(state.pending, null);
  state = reduce(state, { type: 'request', action: 'abort' }); assert.equal(state.abortDialog, true);
  state = reduce(state, { type: 'cancelAbort' }); assert.equal(state.pending, null); assert.equal(state.run.status, 'Running');
  state = reduce(state, { type: 'request', action: 'abort' });
  state = reduce(state, { type: 'request', action: 'abort', confirmed: true });
  assert.equal(state.run.status, 'Running');
  state = reduce(state, { type: 'observe' }); assert.equal(state.run.status, 'Aborted');
});
test('request cancellation preserves observation; fixture reset is explicitly not recovery', () => {
  let state = reduce(initial(), { type: 'request', action: 'pause' });
  state = reduce(state, { type: 'cancelRequest' }); assert.equal(state.run.status, 'Running');
  state = reduce(state, { type: 'scenario', value: 'Disconnected' }); state = reduce(state, { type: 'reset' });
  assert.equal(state.run.status, 'Ready'); assert.match(state.message, /Not recovery/); assert.equal(state.events.at(-1).category, 'fixture');
});
test('export preserves ISO provenance, task/state counts, split durations and notes without mutating state', () => {
  let state = reduce(initial(), { type: 'taskStart', index: 0 }, time(1));
  state = reduce(state, { type: 'variant', value: 'canvas' }, time(3));
  state = reduce(state, { type: 'scenario', value: 'Held' }, time(4));
  state = reduce(state, { type: 'note', text: '<script>observer text only</script>' }, time(5));
  state = reduce(state, { type: 'taskFinish' }, time(6));
  const report = exportSession(state, time(8));
  assert.equal(report.summary.taskCount, 1); assert.equal(report.summary.stateCount, 2);
  assert.deepEqual(report.summary.variantDurationMs, { baseline: 3000, canvas: 5000 });
  assert.equal(report.completedTasks[0].durationMs, 5000); assert.equal(report.notes[0].variant, 'canvas');
  assert.ok(report.events.every(item => Number.isFinite(Date.parse(item.time)) && item.category && item.variant && item.scenario));
  report.snapshot.run.heat = 0; assert.equal(state.run.heat, 50); assert.equal(report.verdict, 'PENDING');
});
