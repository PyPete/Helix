import test from 'node:test';
import assert from 'node:assert/strict';
import { entities, links, evidenceFixtures, createState, reduce, relations, trace, bufferOf, unsavedIds, validate, exportSession } from './model.mjs';
const time = seconds => `2026-10-30T15:00:${String(seconds).padStart(2,'0')}.000Z`;
const initial = () => createState(time(0),'test-003');
test('canonical identities and labeled provenance connect stable entities', () => {
  assert.equal(Object.keys(entities).length,6);
  assert.ok(links.every(link => entities[link.from] && entities[link.to] && link.label));
  assert.deepEqual(relations('S-014').upstream.map(link=>link.from),['collect']);
  assert.equal(relations('S-014').downstream[0].to,'RESULT-014');
});
test('provenance can be traced in both directions without inventing execution', () => {
  assert.ok(trace('R-201').includes('RESULT-014'));
  for (const id of ['R-201','heat','collect','S-014','S-401']) assert.ok(trace('RESULT-014').includes(id));
  assert.match(links.at(-1).label,/no acceptance/);
});
test('per-entity buffers survive selection, layout and graph/cards switching', () => {
  let state=reduce(initial(),{type:'buffer',id:'R-201',fields:{value:'55'}});
  state=reduce(state,{type:'buffer',id:'heat',fields:{name:'Gentle heat'}});
  state=reduce(state,{type:'select',id:'RESULT-014'});
  state=reduce(state,{type:'layout',value:'permanent'}); state=reduce(state,{type:'representation',value:'cards'});
  state=reduce(state,{type:'select',id:'R-201'});
  assert.equal(bufferOf(state,'R-201').value,'55'); assert.equal(bufferOf(state,'heat').name,'Gentle heat');
  assert.equal(state.draft.heat.value,'50'); assert.deepEqual(unsavedIds(state),['R-201','heat']);
});
test('permanent panel context is retained per category independently of global selection', () => {
  let state=reduce(initial(),{type:'select',id:'collect'});state=reduce(state,{type:'select',id:'S-014'});
  assert.deepEqual(state.panelSelections,{device:'R-201',step:'collect',evidence:'S-014'});
  state=reduce(state,{type:'layout',value:'permanent'});assert.equal(state.selected,'S-014');
});
test('save is explicit, discard only removes chosen pending fields, and unrelated edits survive', () => {
  let state=reduce(initial(),{type:'buffer',id:'R-201',fields:{value:'55'}});
  state=reduce(state,{type:'buffer',id:'heat',fields:{name:'Pending title'}});
  state=reduce(state,{type:'save',id:'R-201'});assert.equal(state.draft.heat.value,'55');
  assert.equal(bufferOf(state,'heat').name,'Pending title');
  state=reduce(state,{type:'discard',id:'heat'});assert.equal(state.draft.labels.heat,'Heat reactor');
  assert.equal(state.draft.heat.value,'55');assert.equal(unsavedIds(state).length,0);
});
test('UI review binds saved revision and cannot silently review pending fields', () => {
  let state=reduce(initial(),{type:'review'});assert.equal(state.draft.reviewedRevision,1);
  state=reduce(state,{type:'buffer',fields:{value:'55'}});state=reduce(state,{type:'review'});assert.equal(state.events.at(-1).category,'blocked');
  state=reduce(state,{type:'save'});assert.equal(state.draft.reviewedRevision,null);
  state=reduce(state,{type:'review'});assert.equal(state.draft.reviewedRevision,2);
});
test('invalid numbers, limits, units and labels remain visible but cannot review', () => {
  for (const fields of [{value:''},{value:' '},{value:'NaN'},{value:'Infinity'},{value:'95'},{value:'19'},{unit:'K'}]) {
    let state=reduce(initial(),{type:'buffer',fields});state=reduce(state,{type:'save'});state=reduce(state,{type:'review'});
    assert.equal(validate(state).valid,false);assert.equal(state.draft.reviewedRevision,null);assert.equal(state.events.at(-1).category,'blocked');
  }
  let state=reduce(initial(),{type:'buffer',id:'heat',fields:{name:' '}});state=reduce(state,{type:'save',id:'heat'});assert.equal(validate(state).valid,false);
});
test('run and evidence source revisions stay immutable under draft edits and inspection', () => {
  const original=initial();let state=reduce(original,{type:'buffer',fields:{value:'55'}});state=reduce(state,{type:'save'});
  state=reduce(state,{type:'select',id:'RESULT-014'});assert.deepEqual(state.run,original.run);assert.deepEqual(state.evidence,original.evidence);
  assert.throws(()=>evidenceFixtures.quarantine.rows[0].value=999,TypeError);
});
test('inspection cannot promote quarantine; reviewed data is a separate explicit fixture', () => {
  let state=reduce(initial(),{type:'select',id:'RESULT-014'});state=reduce(state,{type:'review'});
  assert.equal(state.evidence.reviewed,false);assert.equal(state.evidence.sourceRevision,'CSV-Q-r1');
  state=reduce(state,{type:'evidence',value:'reviewed'});assert.equal(state.evidence.reviewed,true);assert.equal(state.evidence.sourceRevision,'CSV-VALID-r2');
  state=reduce(state,{type:'evidence',value:'quarantine'});assert.deepEqual(state.evidence,evidenceFixtures.quarantine);
});
test('dirty fixture reset needs confirmation and cancel preserves saved/unsaved state', () => {
  let state=reduce(initial(),{type:'buffer',fields:{value:'55'}});const draft=structuredClone(state.draft),buffers=structuredClone(state.buffers);
  state=reduce(state,{type:'reset'});assert.equal(state.pendingReset,true);state=reduce(state,{type:'cancelReset'});
  assert.deepEqual(state.draft,draft);assert.deepEqual(state.buffers,buffers);
  state=reduce(state,{type:'reset'});state=reduce(state,{type:'confirmReset'});assert.equal(unsavedIds(state).length,0);assert.deepEqual(state.run,initial().run);
});
test('disconnection stays uncertain after reconnect and view/selection changes', () => {
  let state=reduce(initial(),{type:'scenario',value:'Disconnected'});state=reduce(state,{type:'scenario',value:'Running'});
  state=reduce(state,{type:'layout',value:'permanent'});state=reduce(state,{type:'representation',value:'cards'});state=reduce(state,{type:'select',id:'S-014'});
  assert.match(state.uncertainty,/Reconciliation.*stale/);assert.deepEqual(state.run,initial().run);
});
test('export has detached provenance, ISO context, notes, task/counts and split timing', () => {
  let state=reduce(initial(),{type:'taskStart',index:0},time(1));state=reduce(state,{type:'layout',value:'permanent'},time(3));
  state=reduce(state,{type:'select',id:'RESULT-014'},time(4));state=reduce(state,{type:'note',text:'<b>quarantine</b>'},time(5));state=reduce(state,{type:'taskFinish',blocked:true},time(6));
  const report=exportSession(state,time(8));assert.deepEqual(report.summary.layoutDurationMs,{contextual:3000,permanent:5000});
  assert.equal(report.completedTasks[0].durationMs,5000);assert.equal(report.summary.noteCount,1);assert.equal(report.summary.entityCount,2);
  assert.equal(report.verdict,'PENDING');assert.ok(report.events.every(event=>event.time&&event.layout&&event.representation&&event.entity&&event.scenario&&event.category));
  report.semanticSnapshot.evidence.rows[0].value=999;assert.equal(state.evidence.rows[0].value,12.4);
});
