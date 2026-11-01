const cards = document.querySelector('#cards');
const graph = document.querySelector('#graph');
const cardList = document.querySelector('#cardList');
const canvas = document.querySelector('#graphCanvas');
const feedback = document.querySelector('#connectionFeedback');
const currentEntity = document.querySelector('#currentEntity');
const stepName = document.querySelector('#stepName');
const draftValue = document.querySelector('#draftValue');
const binding = document.querySelector('#binding');
const defaultValue = document.querySelector('#defaultValue');
const provenance = document.querySelector('#provenance');
const resources = document.querySelector('#resources');
const draftState = document.querySelector('#draftState');
const resolveDialog = document.querySelector('#resolveDialog');

const templates = {
  'Prepare sample': { type: 'Agent capability · sample.prepare', temperature: '24.0', binding: 'sample: S-142', defaultValue: '24.0 °C', resources: 'balance, sample S-142' },
  Irradiate: { type: 'Agent capability · reactor.irradiate', temperature: '24.0', binding: 'recipe: irradiation-24c', defaultValue: '10 min', resources: 'Reactor R-12 lease' },
  'Analyze aliquot': { type: 'Agent capability · analysis.capture', temperature: '', binding: 'sample: aliquot-1', defaultValue: 'CSV result required', resources: 'analyzer lease' },
  'If evidence condition': { type: 'Control flow · If', temperature: '', binding: 'evidence: analysis.quality', defaultValue: 'quality = acceptable', resources: 'evidence record' },
  'Fork parallel work': { type: 'Control flow · Fork', temperature: '', binding: 'branches: 2', defaultValue: 'all branches required', resources: 'Agent lease plan pending' },
  'Join parallel work': { type: 'Control flow · Join', temperature: '', binding: 'fork: parallel-work', defaultValue: 'wait for both', resources: 'Agent lease reconciliation pending' },
};
const steps = [
  { id: 'ready', name: 'Ready check', type: 'Agent capability · safety.readyCheck', temperature: '', binding: 'apparatus: R-12', defaultValue: 'safe state required', resources: 'Reactor R-12 lease' },
  { id: 'prepare', name: 'Prepare sample', ...templates['Prepare sample'] },
  { id: 'irradiate', name: 'Irradiate', ...templates.Irradiate },
  { id: 'if-quality', name: 'If evidence condition', ...templates['If evidence condition'], kind: 'if' },
  { id: 'fork', name: 'Fork parallel work', ...templates['Fork parallel work'], kind: 'fork' },
  { id: 'analyze', name: 'Analyze aliquot', ...templates['Analyze aliquot'] },
  { id: 'reconcile', name: 'Reconcile observed state', type: 'Control flow · reconciliation', temperature: '', binding: 'observation: reactor-state', defaultValue: 'scientist review', resources: 'Agent reconciliation lease' },
  { id: 'join', name: 'Join parallel work', ...templates['Join parallel work'], kind: 'join' },
];
let edges = [['ready', 'prepare', 'next'], ['prepare', 'irradiate', 'next'], ['irradiate', 'if-quality', 'next'], ['if-quality', 'fork', 'True'], ['if-quality', 'reconcile', 'False'], ['fork', 'analyze', 'parallel'], ['fork', 'reconcile', 'parallel'], ['analyze', 'join', 'join'], ['reconcile', 'join', 'join']];
let selectedId = 'irradiate';
let targetId = null;
let selectedEdge = null;
let dirty = false;
let pendingSelection = null;
let sequence = 0;

function selected() { return steps.find((step) => step.id === selectedId); }
function label(id) { return steps.find((step) => step.id === id)?.name ?? id; }
function setView(kind) { const isGraph = kind === 'graph'; graph.classList.toggle('hidden', !isGraph); cards.classList.toggle('hidden', isGraph); document.querySelector('#graphView').setAttribute('aria-pressed', isGraph); document.querySelector('#cardView').setAttribute('aria-pressed', !isGraph); }
function nodeDetails(step) { if (step.kind === 'if') return '<span class="outputs">True → / False →</span><small>Evaluated evidence: awaiting Agent preflight</small>'; if (step.kind === 'fork') return '<span class="outputs">Parallel branches →</span><small>Resource/lease diagnostic: pending from Agent</small>'; if (step.kind === 'join') return '<span class="outputs">Wait for all branches</span><small>Resource/lease diagnostic: pending from Agent</small>'; return ''; }
function render() {
  cardList.innerHTML = steps.map((step) => `<button class="step-card ${step.id === selectedId ? 'selected' : ''}" data-step="${step.id}"><strong>${step.name}</strong><span>${step.type}</span>${nodeDetails(step)}</button>`).join('');
  canvas.innerHTML = `<div class="edge-list"><h3>Typed connections</h3>${edges.map(([from, to, output]) => `<button class="edge ${selectedEdge?.[0] === from && selectedEdge?.[1] === to ? 'selected' : ''}" data-edge="${from}|${to}">${label(from)} <strong>[${output}]</strong> → ${label(to)}</button>`).join('')}</div><div class="node-list">${steps.map((step) => `<button class="node ${step.id === selectedId ? 'selected' : ''} ${step.id === targetId ? 'target' : ''}" data-step="${step.id}"><strong>${step.name}</strong><span>${step.type}</span>${nodeDetails(step)}</button>`).join('')}</div>`;
  const step = selected();
  currentEntity.textContent = step ? `${step.name} (${step.type})` : 'No step selected';
  stepName.value = step?.name ?? ''; draftValue.value = step?.temperature ?? ''; binding.value = step?.binding ?? ''; defaultValue.value = step?.defaultValue ?? '';
  provenance.textContent = step ? `Provenance: Agent-declared catalog fixture · stable id ${step.id}` : '';
  resources.textContent = step ? `Required resources: ${step.resources}` : '';
  [stepName, draftValue, binding, defaultValue, document.querySelector('#saveDraft'), document.querySelector('#removeStep')].forEach((control) => { control.disabled = !step; });
}
function choose(id) { selectedId = id; targetId = null; selectedEdge = null; dirty = false; draftState.textContent = 'No unsaved simulated changes.'; render(); }
function requestSelection(id) { if (id === selectedId) return; if (!dirty) { choose(id); return; } pendingSelection = id; document.querySelector('#resolveCopy').textContent = `Resolve the local draft for ${label(selectedId)} before selecting ${label(id)}. Its units, bindings, defaults, provenance, and resources remain attached to the current step.`; resolveDialog.showModal(); }
function invalid(reason) { feedback.textContent = `Client diagnostic: invalid connection — ${reason}. Repair it before proposal preflight.`; feedback.className = 'invalid'; }
function connect() { if (!selectedId || !targetId) return invalid('select a source and target'); if (selectedId === targetId) return invalid('a step cannot connect to itself'); if (edges.some(([from, to]) => from === selectedId && to === targetId)) return invalid('that connection already exists'); if (targetId === 'ready') return invalid('Ready check cannot have incoming flow'); const source = selected(); const output = source?.kind === 'if' ? 'True (label before review)' : source?.kind === 'fork' ? 'parallel' : 'next'; edges.push([selectedId, targetId, output]); targetId = null; feedback.textContent = 'Client diagnostic: typed connection added to the local proposal.'; feedback.className = 'valid'; render(); }
function addStep(name) { sequence += 1; const template = templates[name]; const id = `${name.toLowerCase().replaceAll(' ', '-')}-${sequence}`; const kind = name.startsWith('If') ? 'if' : name.startsWith('Fork') ? 'fork' : name.startsWith('Join') ? 'join' : undefined; steps.push({ id, name, ...template, kind }); choose(id); feedback.textContent = `${name} added as a local typed proposal step.`; }
function setKind(kind) { const step = selected(); if (!step) return invalid('select a step first'); step.kind = kind; step.type = `Control flow · ${kind === 'if' ? 'If' : kind === 'fork' ? 'Fork' : 'Join'}`; feedback.textContent = `${step.name} is now a typed ${step.type} step.`; feedback.className = 'valid'; render(); }
function theme(choice) { document.documentElement.dataset.theme = choice; localStorage.setItem('helix-spike-theme', choice); }

document.querySelector('#cardView').addEventListener('click', () => setView('cards'));
document.querySelector('#graphView').addEventListener('click', () => setView('graph'));
document.querySelector('#openBuilder').addEventListener('click', () => setView('graph'));
document.querySelectorAll('[data-theme-choice]').forEach((button) => button.addEventListener('click', () => theme(button.dataset.themeChoice)));
document.querySelectorAll('[data-add]').forEach((button) => button.addEventListener('click', () => addStep(button.dataset.add)));
document.addEventListener('click', (event) => { const step = event.target.closest('[data-step]'); if (step) { if (event.shiftKey && step.dataset.step !== selectedId) { targetId = step.dataset.step; feedback.textContent = `Target selected: ${label(targetId)}. Choose Create typed connection.`; render(); } else requestSelection(step.dataset.step); } const edge = event.target.closest('[data-edge]'); if (edge) { selectedEdge = edge.dataset.edge.split('|'); render(); } });
[stepName, draftValue, binding, defaultValue].forEach((input) => input.addEventListener('input', () => { dirty = true; draftState.textContent = 'Unsaved simulated draft — resolve before changing selection.'; }));
document.querySelector('#saveDraft').addEventListener('click', () => { const step = selected(); step.name = stepName.value.trim() || step.name; step.temperature = draftValue.value; step.binding = binding.value; step.defaultValue = defaultValue.value; dirty = false; draftState.textContent = 'Simulated draft saved locally; not Agent-preflighted or executed.'; render(); });
document.querySelector('#removeStep').addEventListener('click', () => { if (!selectedId) return; edges = edges.filter(([from, to]) => from !== selectedId && to !== selectedId); steps.splice(steps.findIndex((step) => step.id === selectedId), 1); choose(steps[0]?.id); feedback.textContent = 'Step removed from the local proposal.'; });
document.querySelector('#connectSteps').addEventListener('click', connect);
document.querySelector('#removeConnection').addEventListener('click', () => { if (!selectedEdge) return invalid('select a connection to remove'); edges = edges.filter(([from, to]) => from !== selectedEdge[0] || to !== selectedEdge[1]); selectedEdge = null; feedback.textContent = 'Selected local connection removed.'; render(); });
document.querySelector('#makeIf').addEventListener('click', () => setKind('if'));
document.querySelector('#makeFork').addEventListener('click', () => setKind('fork'));
document.querySelector('#makeJoin').addEventListener('click', () => setKind('join'));
document.querySelector('#saveThenSwitch').addEventListener('click', () => { document.querySelector('#saveDraft').click(); resolveDialog.close(); choose(pendingSelection); });
document.querySelector('#discardThenSwitch').addEventListener('click', () => { dirty = false; resolveDialog.close(); choose(pendingSelection); });
document.querySelector('#cancelSwitch').addEventListener('click', () => { pendingSelection = null; resolveDialog.close(); });
document.querySelector('#safetyRoute').addEventListener('click', () => document.querySelector('#safetyDialog').showModal());
document.querySelector('#closeSafety').addEventListener('click', () => document.querySelector('#safetyDialog').close());
document.querySelector('#submitProposal').addEventListener('click', () => { document.querySelector('#proposalStatus').textContent = 'Local proposal queued for simulated Agent preflight review. No API call, command, lease, admission, approval, dispatch, or execution occurred.'; });
document.querySelector('#startStudy').addEventListener('click', () => { setView('cards'); document.querySelector('#studyPrompt').textContent = 'Task started: make or inspect the If node’s True/False outputs and evidence placeholder, inspect Fork/Join diagnostics, resolve a draft on selection change, then submit only for Agent preflight.'; });
theme(localStorage.getItem('helix-spike-theme') || 'system');
render();
