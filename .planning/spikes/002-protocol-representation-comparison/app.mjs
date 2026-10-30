import { createState, reduce, specs, tasks, project, relations, bufferOf, hasUnsaved, validate, exportSession } from './model.mjs';
const $ = id => document.getElementById(id);
let state = createState(new Date().toISOString(), crypto.randomUUID());
let fitted = false;
const text = (tag, value, className = '') => { const element = document.createElement(tag); element.textContent = value; if (className) element.className = className; return element; };
const svgElement = (tag, attributes = {}) => { const element = document.createElementNS('http://www.w3.org/2000/svg', tag); Object.entries(attributes).forEach(([key,value]) => element.setAttribute(key, String(value))); return element; };
function dispatch(input) { state = reduce(state, input); render(); }
function actionButton(label, input) { const element = text('button', label); element.type = 'button'; element.addEventListener('click', () => dispatch(input)); return element; }
function stepButton(item, graph = false) {
  const button = actionButton('', { type: 'select', id: item.id }); button.className = 'step-button'; button.dataset.step = item.id;
  button.setAttribute('aria-pressed', String(state.selected === item.id));
  const links = relations(state.draft.protocol, item.id);
  const parameter = specs[item.kind] ? `${item.value} ${item.unit} · ${specs[item.kind].device}` : { condition: 'Hypothetical YES / NO · no actual decision', 'merge-any': 'ANY one selected branch (XOR)', 'fork-all': 'ALL independent dependency lanes', 'join-all': 'ALL incoming lanes required' }[item.kind];
  button.setAttribute('aria-label', `${item.name}, stable ID ${item.id}, ${parameter}. Upstream: ${links.upstream.map(link => `${link.from}, ${link.label}`).join('; ') || 'none'}. Downstream: ${links.downstream.map(link => `${link.to}, ${link.label}`).join('; ') || 'terminal'}. Draft proposal only.`);
  const meta = text('span', '', 'step-meta'); meta.append(text('span', item.id), text('span', item.kind.replaceAll('-', ' ')));
  button.append(meta, text('strong', item.name || '(blank name · invalid proposal)'), text('span', parameter, 'parameter'));
  const dependencies = links.upstream.length ? links.upstream.map(link => `${link.from} → [${link.label}]`).join(' · ') : 'No upstream dependency · root';
  button.append(text('span', graph ? `${links.upstream.length} incoming / ${links.downstream.length} outgoing · inspect to trace` : dependencies, 'step-deps'));
  button.append(text('span', state.draft.reviewedRevision === state.draft.revision ? '◇ UI-only reviewed proposal · NOT authorized' : '◷ Proposed draft · UI review required', 'step-state'));
  return button;
}
function renderCards(projection) {
  const container = $('cards'); container.replaceChildren(); container.className = 'lane-list' + (state.fixture === 'linear' ? '' : ' multi');
  for (const lane of projection.lanes) {
    const section = text('section', '', `lane lane-${lane.id}`); section.setAttribute('aria-label', lane.title);
    section.append(text('h3', lane.title), text('p', lane.meaning));
    const list = text('div', '', 'lane-content'); lane.nodes.forEach(item => list.append(stepButton(item))); section.append(list); container.append(section);
  }
}
function renderGraph(projection) {
  const container = $('graph'); container.replaceChildren(); container.classList.toggle('fit', fitted);
  const depth = new Map();
  const rank = id => {
    if (depth.has(id)) return depth.get(id);
    // The canonical structural transforms prohibit cycles; invalid probes are never projected.
    const upstream = projection.edges.filter(link => link.to === id);
    const value = upstream.length ? Math.max(...upstream.map(link => rank(link.from))) + 1 : 0;
    depth.set(id, value); return value;
  };
  projection.nodes.forEach(item => rank(item.id));
  const positions = new Map(projection.nodes.map(item => [item.id, {
    x: ['yes','thermal'].includes(item.lane) ? 25 : ['no','feedlane'].includes(item.lane) ? 485 : 255,
    y: 55 + depth.get(item.id) * 244,
  }]));
  const height = Math.max(...[...positions.values()].map(position => position.y)) + 180;
  const svg = svgElement('svg', { viewBox: `0 0 800 ${height}`, role: 'group', 'aria-label': 'Proposed dependency graph; labeled selectable steps, not execution telemetry' });
  const defs = svgElement('defs'), marker = svgElement('marker', { id: 'dependency-arrow', viewBox: '0 0 10 10', refX: 9, refY: 5, markerWidth: 6, markerHeight: 6, orient: 'auto-start-reverse' });
  marker.append(svgElement('path', { d: 'M0 0 L10 5 L0 10z', fill: '#789080' })); defs.append(marker); svg.append(defs);
  for (const link of projection.edges) {
    const source = positions.get(link.from), target = positions.get(link.to);
    const x1 = source.x + 135, y1 = source.y + 146, x2 = target.x + 135, y2 = target.y;
    const midY = (y1+y2)/2, midX = (x1+x2)/2;
    svg.append(svgElement('path', { d: `M${x1} ${y1} C${x1} ${midY},${x2} ${midY},${x2} ${y2-5}`, class: 'graph-edge', 'marker-end': 'url(#dependency-arrow)', 'aria-hidden': 'true' }));
    const labelWidth = Math.min(270, link.label.length * 6.3 + 20);
    svg.append(svgElement('rect', { x: midX-labelWidth/2, y: midY-13, width: labelWidth, height: 25, rx: 5, fill: '#f6f9f3', stroke: '#d6e0d3', 'aria-hidden': 'true' }));
    const label = svgElement('text', { x: midX, y: midY+4, class: 'graph-label', 'aria-hidden': 'true' }); label.textContent = link.label; svg.append(label);
  }
  for (const item of projection.nodes) {
    const position = positions.get(item.id), lane = projection.lanes.find(lane => lane.id === item.lane);
    const laneLabel = svgElement('text', { x: position.x+135, y: position.y-12, class: 'graph-lane-label', 'aria-hidden': 'true' }); laneLabel.textContent = lane.title;
    const foreign = svgElement('foreignObject', { x: position.x, y: position.y, width: 270, height: 148 }); foreign.append(stepButton(item, true)); svg.append(laneLabel, foreign);
  }
  container.append(svg);
}
function renderRelations() {
  const links = relations(state.draft.protocol, state.selected);
  for (const [direction, list] of Object.entries(links)) {
    $(direction).replaceChildren();
    if (!list.length) $(direction).append(text('li', direction === 'upstream' ? 'None · root of dependency path' : 'None · terminal proposal'));
    for (const link of list) {
      const id = direction === 'upstream' ? link.from : link.to;
      const li = document.createElement('li'); li.append(actionButton(id, { type: 'select', id }), text('span', ` — ${link.label}`)); $(direction).append(li);
    }
  }
  $('dependencies').replaceChildren(...state.draft.protocol.edges.map(link => {
    const li = document.createElement('li'); li.append(text('b', `${link.from} → ${link.to}`), text('span', ` · ${link.label}`)); return li;
  }));
}
function renderEditor() {
  const item = state.draft.protocol.nodes.find(item => item.id === state.selected), buffer = bufferOf(state), spec = specs[item.kind];
  $('selected-id').textContent = item.id; $('selected-title').textContent = item.name || '(blank name)'; $('step-context').textContent = `${item.kind} / lane ${item.lane} / synthetic proposal · not admitted`;
  $('step-name').value = buffer.name; $('value').value = buffer.value; $('value').disabled = !spec; $('unit').disabled = !spec;
  const units = [...new Set([spec?.unit ?? 'Not numeric', '°C', 'K', '°F', 'mL/min', 'mL', 'min'])];
  $('unit').replaceChildren(...units.map(unit => new Option(unit, unit))); $('unit').value = spec ? buffer.unit : 'Not numeric';
  $('parameter-help').textContent = spec ? `Illustrative ${spec.min}–${spec.max} ${spec.unit}. No implicit unit conversion. Invalid proposals remain visible and block UI review.` : 'Flow-control nodes expose their condition/fork/join semantics; no fabricated numeric value. Rename is draft-only.';
  $('buffer-status').textContent = hasUnsaved(state) ? '◷ Unsaved editor buffer(s). They survive selection/view changes; save before UI-only review.' : '◇ Editor matches saved draft. This is not execution authorization.';
  const index = state.draft.protocol.nodes.findIndex(item => item.id === state.selected), linear = state.fixture === 'linear', invalid = !validate(state.draft.protocol).valid;
  $('earlier').disabled = !linear || index === 0 || invalid; $('later').disabled = !linear || index === state.draft.protocol.nodes.length-1 || invalid;
  $('reorder-help').textContent = linear ? 'Moves the selected step in the draft chain and regenerates explicit dependencies. Scientific ordering is not validated.' : 'Disabled for branch/parallel fixtures in BOTH views: a single total order would hide their semantics. Add on a labeled path instead.';
  const outgoing = relations(state.draft.protocol, state.selected).downstream;
  $('add-path').replaceChildren(...(outgoing.length ? outgoing.map(link => new Option(`${link.to} · ${link.label}`, link.to)) : [new Option('Append after terminal selected step', '')]));
}
function renderStatus() {
  const check = validate(state.draft.protocol), unsaved = hasUnsaved(state), reviewed = state.draft.reviewedRevision === state.draft.revision && !unsaved;
  $('review-status').textContent = reviewed ? '◇ UI-only review recorded. Warnings remain; no scientific approval.' : !check.valid ? '⚠ Invalid proposal. Review blocked; frozen run unchanged.' : unsaved ? '◷ Unsaved fields. UI review requires saved proposals.' : '◷ Proposed draft · UI review required.';
  $('policy-status').textContent = reviewed ? '◇ UI-reviewed shape only; NOT authorized' : '◷ Unreviewed proposal; NOT authorized';
  $('draft-reference').textContent = `${state.fixture.toUpperCase()} / proposal r${state.draft.revision}`;
  $('counts').textContent = `${state.draft.protocol.nodes.length} stable step identities · ${state.draft.protocol.edges.length} labeled dependencies`;
  $('message').textContent = state.message; $('uncertainty').value = state.uncertainty;
  $('safety').classList.toggle('uncertain', state.uncertainty !== 'Simulation snapshot only');
  $('errors').replaceChildren(...check.errors.map(error => text('li', '⚠ ' + error)));
  if (!check.errors.length) $('errors').append(text('li', '◇ Saved proposal has valid UI structure/parameters; this does NOT validate usability, safety or scientific execution.'));
  if (state.probe) {
    $('errors').append(text('li', `DIAGNOSTIC PROBE (${state.probe.kind}) · rejected copy only:`));
    state.probe.check.errors.forEach(error => $('errors').append(text('li', '⚠ ' + error)));
  }
  $('warnings').replaceChildren(...check.warnings.map(warning => text('li', '⚠ ' + warning)));
  $('fixture').value = state.fixture;
  $('fixture-dialog-detail').textContent = `Replace ${state.fixture} with ${state.pendingFixture ?? 'another fixture'}?`;
  if (state.pendingFixture && !$('fixture-dialog').open) $('fixture-dialog').showModal();
  if (!state.pendingFixture && $('fixture-dialog').open) $('fixture-dialog').close();
}
function renderObserver() {
  $('task-start').disabled = !!state.task; $('task-complete').disabled = !state.task; $('task-blocked').disabled = !state.task;
  $('task-status').textContent = state.task ? `◷ Task ${state.task.index+1} since ${state.task.startedAt} / ${state.task.variant} / ${state.task.fixture}. Self-report the outcome.` : `${state.completedTasks.length} attempts recorded; no winner selected.`;
  $('notes').replaceChildren(...state.notes.map(note => text('li', `${note.variant} / ${note.fixture}: ${note.text}`)));
  $('log-summary').textContent = `${state.events.length} events · ${state.completedTasks.length} tasks`;
  $('events').replaceChildren(...state.events.slice(-20).reverse().map(event => {
    const li = document.createElement('li'); li.append(text('time', event.time), text('strong', `${event.category.toUpperCase()} / ${event.variant} / ${event.fixture}`), text('div', event.detail)); return li;
  }));
}
function render() {
  const active = document.activeElement, stepFocus = active?.dataset.step, cursor = ['step-name','value'].includes(active?.id) ? [active.id, active.selectionStart, active.selectionEnd] : null;
  document.querySelectorAll('[data-variant]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.variant === state.variant)));
  $('cards').hidden = state.variant !== 'cards'; $('graph').hidden = state.variant !== 'graph';
  $('representation-label').textContent = state.variant === 'cards' ? '002a / SEQUENCE CARDS WITH EXPLICIT LANES' : '002b / SVG DEPENDENCY GRAPH';
  $('protocol-title').textContent = state.draft.protocol.title;
  $('flow-note').textContent = state.fixture === 'linear' ? 'One explicit dependency chain. Card order and graph arrows express the same draft order; neither reports observed execution.' : state.fixture === 'branching' ? 'EXCLUSIVE BRANCHES: YES and NO are alternatives, not successive tasks. ANY-path merge records only the selected path. The result condition is hypothetical; quarantined evidence selects neither path.' : 'PARALLEL LANES: lane A and lane B have no implied total order. ALL-dependency join requires both. Real Agent resource/lease validation is outside this prototype.';
  const projection = project(state.draft.protocol); renderCards(projection); renderGraph(projection); renderRelations(); renderEditor(); renderStatus(); renderObserver();
  if (stepFocus && !state.pendingFixture) {
    const container = state.variant === 'cards' ? $('cards') : $('graph');
    container.querySelector(`[data-step="${stepFocus}"]`)?.focus({ preventScroll: true });
  }
  if (cursor && !state.pendingFixture) { const input = $(cursor[0]); input.focus({ preventScroll: true }); input.setSelectionRange(cursor[1], cursor[2]); }
}
document.querySelectorAll('[data-variant]').forEach(button => button.addEventListener('click', () => dispatch({ type: 'variant', value: button.dataset.variant })));
$('fixture').addEventListener('change', () => dispatch({ type: 'fixture', value: $('fixture').value }));
$('fixture-cancel').addEventListener('click', event => { event.preventDefault(); dispatch({ type: 'cancelFixture' }); $('fixture').focus(); });
$('fixture-confirm').addEventListener('click', event => { event.preventDefault(); dispatch({ type: 'confirmFixture' }); $('fixture').focus(); });
$('fixture-dialog').addEventListener('cancel', event => { event.preventDefault(); dispatch({ type: 'cancelFixture' }); $('fixture').focus(); });
for (const [id, field] of [['step-name','name'],['value','value'],['unit','unit']]) $(id).addEventListener(id === 'unit' ? 'change' : 'input', () => dispatch({ type: 'buffer', fields: { [field]: $(id).value } }));
$('edit-form').addEventListener('submit', event => { event.preventDefault(); dispatch({ type: 'apply' }); });
$('restore-buffer').addEventListener('click', () => dispatch({ type: 'restoreBuffer' }));
$('review').addEventListener('click', () => dispatch({ type: 'review' }));
$('earlier').addEventListener('click', () => dispatch({ type: 'reorder', direction: -1 }));
$('later').addEventListener('click', () => dispatch({ type: 'reorder', direction: 1 }));
$('add-form').addEventListener('submit', event => { event.preventDefault(); dispatch({ type: 'add', kind: $('add-kind').value, to: $('add-path').value }); });
$('fit').addEventListener('click', () => { fitted = !fitted; $('graph').classList.toggle('fit', fitted); $('fit').setAttribute('aria-pressed', String(fitted)); });
$('uncertainty').addEventListener('change', () => dispatch({ type: 'uncertainty', value: $('uncertainty').value }));
for (const [id, kind] of [['probe-cycle','cycle'],['probe-dangling','dangling'],['probe-clear','clear']]) $(id).addEventListener('click', () => dispatch({ type: 'probe', kind }));
tasks.forEach((task,index) => $('task-choice').append(new Option(`${index+1}. ${task}`, String(index))));
$('task-start').addEventListener('click', () => dispatch({ type: 'taskStart', index: Number($('task-choice').value) }));
$('task-complete').addEventListener('click', () => dispatch({ type: 'taskFinish' }));
$('task-blocked').addEventListener('click', () => dispatch({ type: 'taskFinish', blocked: true }));
$('note-save').addEventListener('click', () => { dispatch({ type: 'note', text: $('note').value }); $('note').value = ''; });
$('export').addEventListener('click', () => {
  const report = exportSession(state), url = URL.createObjectURL(new Blob([JSON.stringify(report,null,2)], { type: 'application/json' }));
  const link = document.createElement('a'); link.href = url; link.download = `helix-spike-002-${state.session.id}.json`; document.body.append(link); link.click(); link.remove(); URL.revokeObjectURL(url);
});
new ResizeObserver(() => document.documentElement.style.setProperty('--safe-height', `${$('safety').getBoundingClientRect().height}px`)).observe($('safety'));
render();
