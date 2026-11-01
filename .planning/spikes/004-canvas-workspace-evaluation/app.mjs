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
let selected = 'Reactor R-12';
let pendingSelection = null;
let dirty = false;

function setView(kind) {
  const isGraph = kind === 'graph';
  graph.classList.toggle('hidden', !isGraph);
  cards.classList.toggle('hidden', isGraph);
  graphView.classList.toggle('selected', isGraph);
  cardView.classList.toggle('selected', !isGraph);
}
function selectEntity(name) {
  selected = name;
  entityName.textContent = name;
  currentEntity.textContent = name;
  draftValue.value = name === 'Reactor R-12' ? '24.0' : '';
  dirty = false;
  draftState.textContent = 'No unsaved changes';
}
function requestSelection(name) {
  if (name === selected) return;
  if (!dirty) return selectEntity(name);
  pendingSelection = name;
  currentEntity.textContent = selected;
  resolveDialog.showModal();
}
graphView.addEventListener('click', () => setView('graph'));
cardView.addEventListener('click', () => setView('cards'));
document.querySelectorAll('[data-entity]').forEach(button => button.addEventListener('click', () => requestSelection(button.dataset.entity)));
draftValue.addEventListener('input', () => { dirty = true; draftState.textContent = 'Unsaved simulated draft - resolve before changing selection'; });
document.querySelector('#saveDraft').addEventListener('click', () => { dirty = false; draftState.textContent = 'Simulated draft saved; still a proposal, not admitted'; });
document.querySelector('#saveThenSwitch').addEventListener('click', () => { dirty = false; resolveDialog.close(); selectEntity(pendingSelection); });
document.querySelector('#discardThenSwitch').addEventListener('click', () => { dirty = false; resolveDialog.close(); selectEntity(pendingSelection); });
document.querySelector('#cancelSwitch').addEventListener('click', () => { pendingSelection = null; resolveDialog.close(); });
document.querySelector('#safetyRoute').addEventListener('click', () => safetyDialog.showModal());
document.querySelector('#closeSafety').addEventListener('click', () => safetyDialog.close());
document.querySelector('#reviewProposal').addEventListener('click', event => { event.currentTarget.textContent = 'Proposal queued for scientist review'; event.currentTarget.disabled = true; });