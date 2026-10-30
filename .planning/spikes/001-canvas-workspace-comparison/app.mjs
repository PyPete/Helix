import { createState, reduce, devices, scenarios, tasks, actionBlock, validateHeat, exportSession } from './model.mjs';
const $ = id => document.getElementById(id);
let state = createState(new Date().toISOString(), crypto.randomUUID());
let editor = { heat: state.draft.heat, unit: state.draft.unit };
const icons = { Apparatus: '⌖', Design: '✎', Run: '▣', Evidence: '▤' };
const descriptions = {
  Apparatus: 'Understand the physical story before proposing the next step.',
  Design: 'Draft scientific intent. An edit is not execution authorization.',
  Run: 'Separate requested actions from observed outcomes, in every workspace.',
  Evidence: 'Trace samples to reviewed evidence. Uncertainty is part of the record.',
};
const text = (tag, content, className) => { const node = document.createElement(tag); node.textContent = content; if (className) node.className = className; return node; };
function button(label, input, className = '') {
  const node = text('button', label, className); node.type = 'button'; node.addEventListener('click', () => dispatch(input)); return node;
}
function dispatch(input) {
  state = reduce(state, input);
  if (input.type === 'draft') editor = { heat: state.draft.heat, unit: state.draft.unit };
  render();
}
function deviceButton(device) {
  const node = button('', { type: 'select', value: device.id }, 'device');
  node.dataset.device = device.id;
  node.setAttribute('aria-pressed', String(state.selected === device.id));
  node.setAttribute('aria-label', `${device.name} ${device.tag}, ${device.reading} ${device.unit}, ${state.connected && !state.reconcile ? 'simulated snapshot' : 'stale; not readiness'}`);
  node.append(text('span', device.tag, 'tag'), text('strong', device.name));
  const reading = text('span', device.reading.toFixed(device.unit === 'vials' ? 0 : 1) + ' ', 'reading'); reading.append(text('small', device.unit));
  node.append(reading, text('span', state.connected && !state.reconcile ? '◇ Simulated snapshot' : '⚠ STALE · not readiness', 'quality'));
  return node;
}
function renderDetail() {
  const device = devices.find(item => item.id === state.selected);
  const area = $('detail'); area.replaceChildren(text('span', device.tag, 'chip'), text('h2', device.name));
  const reading = text('p', String(device.reading) + ' ', 'detail-reading'); reading.append(text('small', device.unit)); area.append(reading);
  area.append(text('p', state.connected && !state.reconcile ? '◇ SIMULATED snapshot · never live telemetry' : '⚠ STALE / physical state unknown · not readiness', 'small'));
  const dl = document.createElement('dl');
  for (const [key, value] of [['Role', device.role], ['Capability', device.capability], ['Identity', 'Synthetic fixture · ' + device.tag], ['Authority', 'Real Agent only · not connected']]) dl.append(text('dt', key), text('dd', value));
  area.append(dl);
  if (device.id === 'sample') area.append(text('p', 'S-014 · synthetic aliquot · source SIM-RUN-014/r7. CSV evidence quarantined: unit mapping unreviewed. Not accepted evidence.', 'notice'));
}
function renderMode() {
  $('mode-heading').textContent = `${state.mode} workspace`;
  $('mode-description').textContent = descriptions[state.mode];
  $('stage-label').textContent = state.mode.toUpperCase() + ' / SHARED SCIENTIFIC CONTEXT';
  const content = $('mode-content'); content.replaceChildren();
  if (state.mode === 'Apparatus') {
    content.append(text('span', 'THE APPARATUS STORY', 'eyebrow'), text('p', 'P-101 meters solvent into R-201. C-301 conditions the reactor jacket. S-401 collects the resulting aliquot as synthetic sample S-014. Select any labeled device to inspect its context.'));
  } else if (state.mode === 'Design') {
    content.append(text('span', 'PROTOCOL INTENT / REPRESENTATION COMPARISON IS SPIKE 002', 'eyebrow'));
    const flow = document.createElement('div'); flow.className = 'flow';
    ['01 · Feed 1.2 mL/min', '→', `02 · Heat ${state.draft.heat} ${state.draft.unit} (draft)`, '→', '03 · Collect S-014'].forEach(value => flow.append(text('span', value)));
    content.append(flow, text('p', `Draft r${state.draft.revision} is separate from admitted run ${state.run.revision}: ${state.run.heat} ${state.run.unit}. Use the same heat editor in either variant.`), text('p', 'Illustrative review is not Agent admission. Capability, lease, resource, budget and termination validation are not implemented in this demo.', 'notice'));
  } else if (state.mode === 'Run') {
    content.append(text('span', 'EXECUTION SNAPSHOT / SIMULATION', 'eyebrow'), text('h3', `${state.run.status} · ${state.run.id}`), text('p', `Frozen revision ${state.run.revision} · heat ${state.run.heat} ${state.run.unit}. Fixture progress: ${state.run.progress}% — static, not measured, not a completion prediction.`));
    content.append(text('p', state.pending ? `Request pending: ${state.pending.action}. The observed status is still ${state.run.status}. Confirm a separate SIMULATED observation below.` : 'No request pending. All execution controls remain in the persistent safety strip.', 'notice'));
  } else {
    content.append(text('span', 'SAMPLE → SOURCE → REVIEW', 'eyebrow'), text('h3', 'S-014 · synthetic sample'), text('p', 'Aliquot 03 / S-401 → SIM-RUN-014 → approved-fixture-r7. Source artifact: synthetic-results.csv (fixture label only; no file imported).'), text('p', '⚠ Quarantined · unit mapping unreviewed. No accepted analytical result. AI suggestions cannot promote this record to reviewed scientific evidence.', 'notice'), button('Inspect collector provenance', { type: 'select', value: 'sample' }));
  }
}
function renderSafety() {
  const uncertain = !state.connected || state.reconcile;
  document.querySelector('.safety-strip').classList.toggle('uncertain', uncertain);
  $('run-status').textContent = `${uncertain || state.run.status === 'Held' ? '⚠' : state.run.status === 'Running' ? '▶' : 'Ⅱ'} ${state.run.status} · observed SIMULATION fixture`;
  $('safety-facts').replaceChildren(text('span', state.connected ? '◇ Simulator snapshot available' : '⚠ Disconnected · readings stale'), text('span', state.reconcile ? '⚠ Reconciliation required · no replay' : state.run.status === 'Held' ? '⚠ Safety hold · needs separate review' : '▣ Frozen illustrative run policy'), text('span', `Run heat ${state.run.heat} ${state.run.unit} · ${state.run.revision}`));
  const actions = $('actions'); actions.replaceChildren();
  for (const [action, label] of [['start', '▷ Request start'], ['pause', 'Ⅱ Request pause'], ['resume', '▷ Request resume'], ['retry', '↻ Retry blocked'], ['abort', '× Request abort']]) {
    const node = button(label, { type: 'request', action }, action === 'abort' ? 'danger' : '');
    node.dataset.action = action;
    const reason = actionBlock(state, action); node.disabled = !!reason; node.title = reason || 'Queues a simulated request; does not confirm a physical outcome.'; actions.append(node);
  }
  $('pending-status').textContent = state.pending ? `◷ PENDING REQUEST: ${state.pending.action.toUpperCase()} · observed status remains ${state.run.status}` : '◇ No pending request · no physical outcome implied';
  $('observe').disabled = !state.pending || uncertain; $('cancel-request').disabled = !state.pending;
  $('message').textContent = state.message;
  $('reading-quality').textContent = uncertain ? '⚠ STALE · NOT READINESS' : '◇ FIXTURE VALUES';
  $('snapshot-label').textContent = `${state.run.id} / ${state.run.revision}`;
  const dialog = $('abort-dialog');
  if (state.abortDialog && !dialog.open) dialog.showModal();
  if (!state.abortDialog && dialog.open) dialog.close();
}
function renderObserver() {
  $('task-start').disabled = !!state.task; $('task-finish').disabled = !state.task; $('task-blocked').disabled = !state.task;
  $('task-status').textContent = state.task ? `◷ Task ${state.task.index + 1} started ${state.task.startedAt} in ${state.task.startedVariant}. Mark outcome when done.` : `${state.completedTasks.length} task attempts recorded. Timing alone does not establish a winner.`;
  $('notes').replaceChildren(...state.notes.map(note => text('li', `${note.variant} / ${note.scenario}: ${note.text}`)));
  $('log-summary').textContent = `${state.events.length} events · ${state.completedTasks.length} tasks`;
  $('events').replaceChildren(...state.events.slice(-20).reverse().map(item => {
    const li = document.createElement('li'); li.append(text('time', item.time), text('strong', `${item.category.toUpperCase()} / ${item.variant} / ${item.scenario}`), text('div', item.detail)); return li;
  }));
}
function render() {
  const focusedDevice = document.activeElement?.dataset.device;
  const focusedAction = document.activeElement?.dataset.action;
  const focusedList = document.activeElement?.parentElement.id;
  document.querySelectorAll('[data-variant]').forEach(node => node.setAttribute('aria-pressed', String(node.dataset.variant === state.variant)));
  $('modes').querySelectorAll('button').forEach(node => node.setAttribute('aria-pressed', String(node.dataset.mode === state.mode)));
  $('layout').className = 'workspace-layout ' + state.variant; $('ribbon').hidden = state.variant !== 'baseline';
  $('scenario').value = state.scenario;
  $('device-list').replaceChildren(...devices.map(deviceButton)); $('canvas-devices').replaceChildren(...devices.map(deviceButton));
  // Both unsaved editor text and the saved model draft survive presentation changes.
  if (document.activeElement !== $('heat')) $('heat').value = editor.heat;
  $('unit').value = editor.unit;
  const validation = validateHeat(state.draft);
  $('draft-status').textContent = !validation.valid ? '⚠ ' + validation.errors.join(' ') : state.draft.reviewedRevision === state.draft.revision ? `◇ Draft r${state.draft.revision}: illustrative review passed; not authorized.` : `◷ Draft r${state.draft.revision}: review required; run unchanged.`;
  renderDetail(); renderMode(); renderSafety(); renderObserver();
  if (focusedDevice && ['device-list', 'canvas-devices'].includes(focusedList)) {
    $(focusedList).querySelector(`[data-device="${focusedDevice}"]`)?.focus({ preventScroll: true });
  }
  if (focusedAction && !state.abortDialog) {
    const replacement = $('actions').querySelector(`[data-action="${focusedAction}"]`);
    if (replacement && !replacement.disabled) replacement.focus({ preventScroll: true });
    else if (!$('observe').disabled) $('observe').focus({ preventScroll: true });
  }
}
for (const mode of Object.keys(icons)) { const node = button(`${icons[mode]} ${mode}`, { type: 'mode', value: mode }); node.dataset.mode = mode; $('modes').append(node); }
document.querySelectorAll('[data-variant]').forEach(node => node.addEventListener('click', () => dispatch({ type: 'variant', value: node.dataset.variant })));
document.querySelectorAll('#ribbon [data-mode]').forEach(node => node.addEventListener('click', () => dispatch({ type: 'mode', value: node.dataset.mode })));
document.querySelectorAll('#ribbon [data-select]').forEach(node => node.addEventListener('click', () => dispatch({ type: 'select', value: node.dataset.select })));
scenarios.forEach(value => $('scenario').append(new Option(value, value)));
$('scenario').addEventListener('change', event => dispatch({ type: 'scenario', value: event.target.value }));
$('reset').addEventListener('click', () => dispatch({ type: 'reset' }));
$('heat').addEventListener('input', () => editor.heat = $('heat').value);
$('unit').addEventListener('change', () => editor.unit = $('unit').value);
$('draft-form').addEventListener('submit', event => { event.preventDefault(); dispatch({ type: 'draft', heat: $('heat').value, unit: $('unit').value }); });
$('review').addEventListener('click', () => { dispatch({ type: 'draft', heat: $('heat').value, unit: $('unit').value }); dispatch({ type: 'review' }); });
$('ai-draft').addEventListener('click', () => dispatch({ type: 'draft', heat: '55', unit: '°C' }));
$('observe').addEventListener('click', () => dispatch({ type: 'observe' }));
$('cancel-request').addEventListener('click', () => dispatch({ type: 'cancelRequest' }));
$('abort-confirm').addEventListener('click', event => { event.preventDefault(); dispatch({ type: 'request', action: 'abort', confirmed: true }); });
$('abort-cancel').addEventListener('click', event => { event.preventDefault(); dispatch({ type: 'cancelAbort' }); });
$('abort-dialog').addEventListener('cancel', event => { event.preventDefault(); dispatch({ type: 'cancelAbort' }); });
tasks.forEach((value, index) => $('task-choice').append(new Option(`${index + 1}. ${value}`, String(index))));
$('task-start').addEventListener('click', () => dispatch({ type: 'taskStart', index: Number($('task-choice').value) }));
$('task-finish').addEventListener('click', () => dispatch({ type: 'taskFinish' }));
$('task-blocked').addEventListener('click', () => dispatch({ type: 'taskFinish', outcome: 'blocked' }));
$('save-note').addEventListener('click', () => { dispatch({ type: 'note', text: $('note').value }); $('note').value = ''; });
$('export').addEventListener('click', () => {
  const report = exportSession(state); const url = URL.createObjectURL(new Blob([JSON.stringify(report, null, 2)], { type: 'application/json' }));
  const anchor = document.createElement('a'); anchor.href = url; anchor.download = `helix-spike-001-${state.session.id}.json`; document.body.append(anchor); anchor.click(); anchor.remove(); URL.revokeObjectURL(url);
});
new ResizeObserver(entries => document.documentElement.style.setProperty('--safe-height', `${entries[0].target.getBoundingClientRect().height}px`)).observe(document.querySelector('.safety-strip'));
render();
