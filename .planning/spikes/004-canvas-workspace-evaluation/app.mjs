const cards = document.querySelector('#cards');
const graph = document.querySelector('#graph');
const cardList = document.querySelector('#cardList');
const canvas = document.querySelector('#graphCanvas');
const feedback = document.querySelector('#connectionFeedback');
const currentEntity = document.querySelector('#currentEntity');
const stepName = document.querySelector('#stepName');
const draftValue = document.querySelector('#draftValue');
const draftState = document.querySelector('#draftState');
const resolveDialog = document.querySelector('#resolveDialog');
const steps = [
  { id: 'ready', name: 'Ready check', type: 'safety', temperature: '', branch: false },
  { id: 'prepare', name: 'Prepare sample', type: 'sample', temperature: '24.0', branch: false },
  { id: 'irradiate', name: 'Irradiate', type: 'reaction', temperature: '24.0', branch: false },
  { id: 'analyze', name: 'Analyze aliquot', type: 'analysis', temperature: '', branch: false },
  { id: 'reconcile', name: 'Reconcile observed state', type: 'reconciliation', temperature: '', branch: false },
];
let edges = [['ready', 'prepare'], ['prepare', 'irradiate'], ['irradiate', 'analyze'], ['irradiate', 'reconcile']];
let selectedId = 'prepare';
let targetId = null;
let selectedEdge = null;
let dirty = false;
let pendingSelection = null;
let sequence = 0;

function selected() { return steps.find(step => step.id === selectedId); }
function label(id) { return steps.find(step => step.id === id)?.name ?? id; }
function setView(kind) { const isGraph = kind === 'graph'; graph.classList.toggle('hidden', !isGraph); cards.classList.toggle('hidden', isGraph); document.querySelector('#graphView').setAttribute('aria-pressed', isGraph); document.querySelector('#cardView').setAttribute('aria-pressed', !isGraph); }
function render() {
  cardList.innerHTML = steps.map(step => `<button class="step-card ${step.id === selectedId ? 'selected' : ''}" data-step="${step.id}"><strong>${step.name}</strong><span>${step.type}${step.branch ? ' · branch' : ''}</span></button>`).join('');
  canvas.innerHTML = `<div class="edge-list"><h3>Connections</h3>${edges.map(([from, to]) => `<button class="edge ${selectedEdge?.[0] === from && selectedEdge?.[1] === to ? 'selected' : ''}" data-edge="${from}|${to}">${label(from)} → ${label(to)}</button>`).join('') || '<p>No connections yet.</p>'}</div><div class="node-list">${steps.map(step => `<button class="node ${step.id === selectedId ? 'selected' : ''} ${step.id === targetId ? 'target' : ''}" data-step="${step.id}"><strong>${step.name}</strong><span>${step.type}${step.branch ? ' · branch point' : ''}</span></button>`).join('')}</div>`;
  const step = selected();
  currentEntity.textContent = step ? `${step.name} (${step.type})` : 'No step selected';
  stepName.value = step?.name ?? '';
  draftValue.value = step?.temperature ?? '';
  [stepName, draftValue, document.querySelector('#saveDraft'), document.querySelector('#removeStep')].forEach(control => control.disabled = !step);
}
function choose(id) { selectedId = id; targetId = null; selectedEdge = null; dirty = false; draftState.textContent = 'No unsaved simulated changes.'; render(); }
function requestSelection(id) { if (id === selectedId) return; if (!dirty) { choose(id); return; } pendingSelection = id; document.querySelector('#resolveCopy').textContent = `Your local simulated draft for ${label(selectedId)} must be resolved before selecting ${label(id)}.`; resolveDialog.showModal(); }
function invalid(reason) { feedback.textContent = `Invalid connection: ${reason} Repair it before submitting a proposal.`; feedback.className = 'invalid'; }
function connect() { if (!selectedId || !targetId) return invalid('select a source and a target'); if (selectedId === targetId) return invalid('a step cannot connect to itself'); if (edges.some(([from, to]) => from === selectedId && to === targetId)) return invalid('that edge already exists'); if (targetId === 'ready') return invalid('the Ready check cannot have incoming edges'); edges.push([selectedId, targetId]); feedback.textContent = `Connection created: ${label(selectedId)} → ${label(targetId)}.`; feedback.className = 'valid'; targetId = null; render(); }
function addStep(name) { sequence += 1; const id = `${name.toLowerCase().replaceAll(' ', '-')}-${sequence}`; steps.push({ id, name, type: name === 'Irradiate' ? 'reaction' : 'scientific step', temperature: '', branch: false }); choose(id); feedback.textContent = `${name} added as simulated mock data.`; }
function markParallel() { if (!selectedId || !targetId) return invalid('select two steps to mark as parallel'); feedback.textContent = `${label(selectedId)} and ${label(targetId)} are marked as parallel work in this simulated proposal.`; feedback.className = 'valid'; }
function theme(choice) { document.documentElement.dataset.theme = choice; localStorage.setItem('helix-spike-theme', choice); }

document.querySelector('#cardView').addEventListener('click', () => setView('cards'));
document.querySelector('#graphView').addEventListener('click', () => setView('graph'));
document.querySelector('#openBuilder').addEventListener('click', () => setView('graph'));
document.querySelectorAll('[data-theme-choice]').forEach(button => button.addEventListener('click', () => theme(button.dataset.themeChoice)));
document.querySelectorAll('[data-add]').forEach(button => button.addEventListener('click', () => addStep(button.dataset.add)));
document.addEventListener('click', event => { const step = event.target.closest('[data-step]'); if (step) { if (event.shiftKey && step.dataset.step !== selectedId) { targetId = step.dataset.step; feedback.textContent = `Target selected: ${label(targetId)}. Choose Create connection.`; render(); } else requestSelection(step.dataset.step); } const edge = event.target.closest('[data-edge]'); if (edge) { selectedEdge = edge.dataset.edge.split('|'); render(); } });
[stepName, draftValue].forEach(input => input.addEventListener('input', () => { dirty = true; draftState.textContent = 'Unsaved simulated draft — resolve before changing selection.'; }));
document.querySelector('#saveDraft').addEventListener('click', () => { const step = selected(); step.name = stepName.value.trim() || step.name; step.temperature = draftValue.value; dirty = false; draftState.textContent = 'Simulated draft saved locally; it is not admitted or executed.'; render(); });
document.querySelector('#removeStep').addEventListener('click', () => { if (!selectedId) return; edges = edges.filter(([from, to]) => from !== selectedId && to !== selectedId); const index = steps.findIndex(step => step.id === selectedId); steps.splice(index, 1); choose(steps[0]?.id); feedback.textContent = 'Step removed from the simulated draft.'; });
document.querySelector('#connectSteps').addEventListener('click', connect);
document.querySelector('#removeConnection').addEventListener('click', () => { if (!selectedEdge) return invalid('select a connection to remove'); edges = edges.filter(([from, to]) => from !== selectedEdge[0] || to !== selectedEdge[1]); selectedEdge = null; feedback.textContent = 'Selected simulated connection removed.'; render(); });
document.querySelector('#makeBranch').addEventListener('click', () => { const step = selected(); if (!step) return invalid('select a source step'); step.branch = true; feedback.textContent = `${step.name} is now a simulated branch point.`; feedback.className = 'valid'; render(); });
document.querySelector('#makeParallel').addEventListener('click', markParallel);
document.querySelector('#saveThenSwitch').addEventListener('click', () => { document.querySelector('#saveDraft').click(); resolveDialog.close(); choose(pendingSelection); });
document.querySelector('#discardThenSwitch').addEventListener('click', () => { dirty = false; resolveDialog.close(); choose(pendingSelection); });
document.querySelector('#cancelSwitch').addEventListener('click', () => { pendingSelection = null; resolveDialog.close(); });
document.querySelector('#safetyRoute').addEventListener('click', () => document.querySelector('#safetyDialog').showModal());
document.querySelector('#closeSafety').addEventListener('click', () => document.querySelector('#safetyDialog').close());
document.querySelector('#submitProposal').addEventListener('click', () => { document.querySelector('#proposalStatus').textContent = 'Simulated proposal recorded for review. No Agent validation, API call, command, or execution occurred.'; });
document.querySelector('#startStudy').addEventListener('click', () => { setView('cards'); document.querySelector('#studyPrompt').textContent = 'Task started: make a branch at Irradiate, select two downstream steps with Shift-click and mark them parallel, attempt an Irradiate → Ready check edge, remove/repair it, edit a step, then submit the proposal.'; });
theme(localStorage.getItem('helix-spike-theme') || 'system'); render();
