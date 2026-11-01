const graph = document.querySelector('#graph');
const cards = document.querySelector('#cards');
const graphView = document.querySelector('#graphView');
const cardView = document.querySelector('#cardView');
const entityName = document.querySelector('#entityName');
const currentEntity = document.querySelector('#currentEntity');
const draftValue = document.querySelector('#draftValue');
const draftState = document.querySelector('#draftState');
const resolveDialog = document.querySelector('#resolveDialog');
const safetyDialog = document.querySelector('#safetyDialog');
const studyFields = document.querySelectorAll('#taskOutcome, #confusionMoment, #representationPreference, #recordStudy');
let selected = 'Reactor R-12';
let pendingSelection = null;
let dirty = false;

function setView(kind) { const isGraph = kind === 'graph'; graph.classList.toggle('hidden', !isGraph); cards.classList.toggle('hidden', isGraph); graphView.classList.toggle('selected', isGraph); cardView.classList.toggle('selected', !isGraph); graphView.setAttribute('aria-pressed', isGraph); cardView.setAttribute('aria-pressed', !isGraph); graphView.textContent = isGraph ? 'Graph open: relationships & parallelism' : 'Inspect relationships & parallelism'; cardView.textContent = isGraph ? 'Return to sequence cards' : 'Sequence cards'; }
function selectEntity(name) { selected = name; entityName.textContent = name; currentEntity.textContent = name; draftValue.value = name === 'Reactor R-12' ? '24.0' : ''; dirty = false; draftState.textContent = 'No unsaved changes'; document.querySelectorAll('[data-entity]').forEach(node => node.classList.toggle('selected-node', node.dataset.entity === name)); }
function requestSelection(name) { if (name === selected) return; if (!dirty) return selectEntity(name); pendingSelection = name; currentEntity.textContent = selected; resolveDialog.showModal(); }
function setTheme(choice) { document.documentElement.dataset.theme = choice; localStorage.setItem('helix-spike-theme', choice); document.querySelectorAll('[data-theme-choice]').forEach(button => button.setAttribute('aria-pressed', button.dataset.themeChoice === choice)); }

setTheme(localStorage.getItem('helix-spike-theme') || 'system');
const expectedGraphEntities = ['Reactor R-12', 'Prepare sample S-142', 'Irradiate', 'Analyze aliquot', 'Reconcile observed state'];
const expectedGraphEdges = ['edge-ready-prepare', 'edge-prepare-irradiate', 'edge-irradiate-analyze', 'edge-irradiate-reconcile', 'edge-analyze-join', 'edge-reconcile-join'];
const graphStage = document.querySelector('.graph-stage');
const graphIsComplete = expectedGraphEntities.every(entity => graphStage.querySelector(`[data-entity="${entity}"]`)) && expectedGraphEdges.every(id => document.getElementById(id));
graphStage.dataset.rendererCheck = graphIsComplete ? 'pass' : 'fail';
console.assert(graphIsComplete, 'Spike 004 graph renderer is missing an expected node or edge.');
graphView.addEventListener('click', () => setView('graph')); cardView.addEventListener('click', () => setView('cards'));
document.querySelectorAll('[data-theme-choice]').forEach(button => button.addEventListener('click', () => setTheme(button.dataset.themeChoice)));
document.querySelectorAll('[data-entity]').forEach(button => button.addEventListener('click', () => requestSelection(button.dataset.entity)));
draftValue.addEventListener('input', () => { dirty = true; draftState.textContent = 'Unsaved simulated draft - resolve before changing selection'; });
document.querySelector('#saveDraft').addEventListener('click', () => { dirty = false; draftState.textContent = 'Simulated draft saved; still a proposal, not admitted'; });
document.querySelector('#saveThenSwitch').addEventListener('click', () => { dirty = false; resolveDialog.close(); selectEntity(pendingSelection); });
document.querySelector('#discardThenSwitch').addEventListener('click', () => { dirty = false; resolveDialog.close(); selectEntity(pendingSelection); });
document.querySelector('#cancelSwitch').addEventListener('click', () => { pendingSelection = null; resolveDialog.close(); });
document.querySelector('#safetyRoute').addEventListener('click', () => safetyDialog.showModal()); document.querySelector('#closeSafety').addEventListener('click', () => safetyDialog.close());
document.querySelector('#reviewProposal').addEventListener('click', event => { event.currentTarget.textContent = 'Proposal queued for scientist review'; event.currentTarget.disabled = true; });
document.querySelector('#startStudy').addEventListener('click', () => { setView('cards'); studyFields.forEach(field => { field.disabled = false; }); document.querySelector('#studyPrompt').textContent = 'Use cards to locate the held reconciliation branch. Open the graph to explain its parallel relationship to Analyze aliquot, then return to cards and confirm the selected context remains intact.'; });
document.querySelector('#recordStudy').addEventListener('click', () => { document.querySelector('#studyStatus').textContent = 'Simulated observation recorded locally for this review; it does not select a winner.'; });
