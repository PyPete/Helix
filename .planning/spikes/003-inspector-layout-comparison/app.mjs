import { entities, links, tasks, createState, reduce, relations, trace, bufferOf, editableFields, unsavedIds, validate, exportSession } from './model.mjs';
const $ = id => document.getElementById(id);
let state = createState(new Date().toISOString(),crypto.randomUUID());
const el = (tag,content='',className='') => { const node=document.createElement(tag);node.textContent=content;if(className)node.className=className;return node; };
const svgEl = (tag,attrs={}) => { const node=document.createElementNS('http://www.w3.org/2000/svg',tag);Object.entries(attrs).forEach(([key,value])=>node.setAttribute(key,String(value)));return node; };
function dispatch(action) { state=reduce(state,action);render(); }
function button(label,action) { const node=el('button',label);node.type='button';node.dataset.action=JSON.stringify(action);node.addEventListener('click',()=>dispatch(action));return node; }
function nameOf(id) { return state.draft.labels[id] ?? entities[id].name; }
function entityButton(id) {
  const entity=entities[id],node=button('',{type:'select',id});node.className='entity-button';node.dataset.entity=id;node.setAttribute('aria-pressed',String(state.selected===id));node.classList.toggle('related',trace(state.selected).includes(id));
  node.append(el('small',`${entity.kind.toUpperCase()} / ${id}`),el('strong',nameOf(id)));
  node.append(el('small',id==='R-201'||id==='heat'?`Draft ${state.draft.heat.value} ${state.draft.heat.unit} · run 50 °C`:entity.kind==='result'?state.evidence.quality:entity.kind==='sample'?'Synthetic 1 mL · correlation S-014':'Synthetic proposal · 1 mL'));
  if(unsavedIds(state).includes(id))node.append(el('span','◷ Pending fields retained','pending-tag'));
  node.setAttribute('aria-label',`${entity.kind} ${id}: ${nameOf(id)}. ${relations(id).upstream.map(link=>'From '+link.from).join(', ')} ${relations(id).downstream.map(link=>'To '+link.to).join(', ')}. Select to inspect.`);
  return node;
}
function renderWork() {
  const graph=$('entity-graph');graph.replaceChildren();graph.hidden=state.representation!=='graph';$('entity-cards').hidden=state.representation!=='cards';
  const positions={'R-201':[20,25],'S-401':[20,250],heat:[320,25],collect:[320,250],'S-014':[620,250],'RESULT-014':[620,25]};
  const svg=svgEl('svg',{viewBox:'0 0 900 415',role:'group','aria-label':'Linked apparatus, linear protocol and provenance records; not observed execution'});
  const defs=svgEl('defs'),marker=svgEl('marker',{id:'arrow-003',viewBox:'0 0 10 10',refX:9,refY:5,markerWidth:6,markerHeight:6,orient:'auto'});marker.append(svgEl('path',{d:'M0 0 L10 5 L0 10z',fill:'#698f79'}));defs.append(marker);svg.append(defs);
  for(const link of links){const [x1,y1]=positions[link.from],[x2,y2]=positions[link.to];const vertical=x1===x2;
    const sx=vertical?x1+130:x1+260,sy=vertical?(y2>y1?y1+120:y1):y1+60,tx=vertical?x2+130:x2,ty=vertical?(y2>y1?y2:y2+120):y2+60;
    svg.append(svgEl('path',{d:`M${sx} ${sy} L${tx} ${ty}`,class:'graph-path','marker-end':'url(#arrow-003)','aria-hidden':'true'}));
    const label=svgEl('text',{x:vertical?sx+8:(sx+tx)/2,y:vertical?(sy+ty)/2:(sy+ty)/2-10,class:'graph-text','aria-hidden':'true'});
    label.textContent=link.from==='heat'?'after heat':link.from==='S-014'?'S-014 correlation':link.from==='collect'?'sample source':'target';svg.append(label);
  }
  Object.entries(positions).forEach(([id,[x,y]])=>{const foreign=svgEl('foreignObject',{x,y,width:260,height:124});foreign.append(entityButton(id));svg.append(foreign);});graph.append(svg);
  const cards=$('entity-cards');cards.replaceChildren();
  const devices=el('section');devices.append(el('h3','Apparatus · linked targets'),entityButton('R-201'),entityButton('S-401'));
  const evidence=el('section');evidence.append(el('h3','Sample / evidence · provenance'),entityButton('S-014'),el('small','S-014 → RESULT-014 · correlation, not acceptance'),entityButton('RESULT-014'));
  const protocol=el('section','','protocol-group');protocol.append(el('h3','Linear protocol · explicit draft dependency'));const chain=el('div','','protocol-chain');chain.append(entityButton('heat'),el('span','after heat →'),entityButton('collect'));protocol.append(chain);cards.append(devices,evidence,protocol);
  $('selection-summary').textContent=`${entities[state.selected].kind.toUpperCase()} / ${state.selected} · linked records below. Selection never saves or reviews fields.`;
  $('trace-links').replaceChildren(...trace(state.selected).map(id=>button(`${id} · ${entities[id].kind}`,{type:'select',id})));
  $('link-ledger').replaceChildren(...links.map(link=>el('li',`${link.from} → ${link.to} · ${link.label}`)));
}
function addRelations(panel,id) {
  const section=el('section','','entity-links');section.append(el('p','TRACE PROVENANCE / TARGETS','eyebrow'));const list=el('ul');
  for(const [direction,items] of Object.entries(relations(id)))for(const link of items){const target=direction==='upstream'?link.from:link.to;const item=el('li');item.append(button(`${direction==='upstream'?'←':'→'} ${target}`,{type:'select',id:target}),el('span',` · ${link.label}`));list.append(item);}
  section.append(list);panel.append(section);
}
function editor(panel,id) {
  const fields=bufferOf(state,id);if(!fields)return;
  const form=el('form');form.noValidate=true;form.append(el('p','PROPOSED FIELDS / EXPLICIT SAVE ONLY','eyebrow'));
  const makeField=(key,label,select=false)=>{const wrapper=el('label',label);let input;
    if(select){input=el('select');['°C','K','°F'].forEach(unit=>input.append(new Option(unit,unit)));input.value=fields[key];}
    else{input=el('input');input.type='text';input.value=fields[key];input.maxLength=100;if(key==='value')input.inputMode='decimal';}
    input.dataset.edit=`${id}:${key}`;input.setAttribute('aria-label',`${id} ${label}`);
    input.addEventListener(select?'change':'input',()=>dispatch({type:'buffer',id,fields:{[key]:input.value}}));wrapper.append(input);return wrapper;};
  if(id==='R-201'){const row=el('div','','field-row');row.append(makeField('value','Heat proposal'),makeField('unit','Unit',true));form.append(row,el('p','Illustrative 20–80 °C. Blank/nonfinite, out-of-range and wrong-unit proposals block review. No implicit conversion.','small muted'));}
  else form.append(makeField('name','Step label'));
  const pending=unsavedIds(state).includes(id);form.append(el('p',pending?'◷ UNSAVED for '+id+' · selecting elsewhere never saves/discards.':'◇ Fields match saved draft.','pending'));
  if(state.buffers[id]&&state.buffers[id].baseRevision!==state.draft.revision)form.append(el('p',`Draft advanced since this buffer began (r${state.buffers[id].baseRevision} → r${state.draft.revision}). This entity edits separate fields; saving merges only these fields, never another entity's input.`,'rebase'));
  const actions=el('div','','button-row'),save=el('button','Save draft');save.type='submit';save.dataset.action=JSON.stringify({type:'save',id});save.disabled=!pending;const discard=button('Discard unsaved',{type:'discard',id});discard.disabled=!pending;actions.append(save,discard);form.append(actions);
  form.addEventListener('submit',event=>{event.preventDefault();dispatch({type:'save',id});});panel.append(form);
}
function evidencePreview(panel) {
  const evidence=state.evidence;panel.append(el('p',`${evidence.quality.toUpperCase()} · ${evidence.reason}`,`quality${evidence.reviewed?' reviewed':''}`));
  panel.append(el('p',`Source ${evidence.artifact} / immutable ${evidence.sourceRevision}\nRecord ${evidence.record}\nCorrelation ${evidence.sample} → ${evidence.run} / ${evidence.protocol}\nUnit ${evidence.unit} explicitly declared; no actual CSV import.`,'source'));
  const table=el('table');table.append(el('caption',`Synthetic CSV preview · ${evidence.unit} · ${evidence.quality}`));const header=el('tr');['Source time','Concentration (mg/L)','Quality'].forEach(value=>header.append(el('th',value)));const head=el('thead');head.append(header);table.append(head);const body=el('tbody');evidence.rows.forEach(row=>{const tr=el('tr');[row.time,String(row.value),row.quality].forEach(value=>tr.append(el('td',value)));body.append(tr);});table.append(body);panel.append(table);
  const raw=el('details');raw.append(el('summary','Inspect immutable synthetic source record'),el('pre',JSON.stringify(evidence,null,2)));panel.append(raw);
  panel.append(el('p','No accept/promote action. Pre-reviewed data is a separate fixture; CSV-Q-r1 remains quarantined. Inspection/UI draft review never authorizes a branch or campaign.','small muted'));
}
function detailPanel(id,heading) {
  const entity=entities[id],panel=el('section','','panel detail');panel.classList.toggle('current',state.selected===id);panel.setAttribute('aria-label',`${heading}: ${id}`);
  const top=el('div','','panel-heading');top.append(el('span',heading,'eyebrow'),el('span',id,'chip'));panel.append(top,el('h2',nameOf(id)),el('p',`${entity.kind.toUpperCase()} / stable ID ${id} / ${entity.unit}`,'small muted'));
  if(['sample','result'].includes(entity.kind)){
    if(entity.kind==='sample')panel.append(el('p',`Sample source ${entity.source} · synthetic collected volume 1 mL. Linked result belongs to S-014, not a guessed sample.`,'source'));
    evidencePreview(panel);
  }else{
    panel.append(el('p',`Source ${entity.source} · immutable SIM-RUN-014 / approved-fixture-r7. ${entity.quality}.`,'source'));
    panel.append(el('p',id==='R-201'||id==='heat'?`Saved draft heat ${state.draft.heat.value} ${state.draft.heat.unit}; approved run stays 50 °C. Saved step label: ${state.draft.labels.heat}.`:'Collection proposal 1 mL; synthetic approved run unchanged.','saved'));
    panel.append(el('p',state.draft.reviewedRevision===state.draft.revision&&!unsavedIds(state).length?'◇ UI-only reviewed draft, NOT scientifically approved or admitted.':'◷ Draft/pending intent not reviewed or authorized.','quality'));
    if(state.uncertainty!=='Simulation snapshot only')panel.append(el('p','⚠ Snapshot stale. No present physical reading is known. Reconciliation required.','quality'));
    editor(panel,id);
  }
  addRelations(panel,id);return panel;
}
function renderPanels() {
  const container=$('detail-panels');container.replaceChildren();
  if(state.layout==='contextual')container.append(detailPanel(state.selected,'Single contextual inspector'));
  else{const selections=state.panelSelections;container.append(detailPanel(selections.device,'Permanent / device detail'),detailPanel(selections.step,'Permanent / protocol properties'),detailPanel(selections.evidence,'Permanent / sample & evidence'));}
}
function render() {
  const active=document.activeElement,editKey=active?.dataset.edit,entityFocus=active?.dataset.entity,actionKey=active?.dataset.action,cursor=editKey&&active.tagName==='INPUT'?[active.selectionStart,active.selectionEnd]:null;
  $('inspection-layout').className=`inspection-layout ${state.layout}`;
  document.querySelectorAll('[data-layout]').forEach(node=>node.setAttribute('aria-pressed',String(node.dataset.layout===state.layout)));
  document.querySelectorAll('[data-representation]').forEach(node=>node.setAttribute('aria-pressed',String(node.dataset.representation===state.representation)));
  renderWork();renderPanels();const pending=unsavedIds(state),check=validate(state);
  $('draft-status').textContent=`Proposal r${state.draft.revision} / ${state.draft.reviewedRevision===state.draft.revision&&!pending.length?'UI-only reviewed':'NOT authorized'}`;
  $('pending-summary').textContent=pending.length?`◷ Pending fields: ${pending.join(', ')}. Retained independently; review blocked until saved/discarded.`:'No unsaved fields. Saved draft is separate from the admitted run.';
  $('diagnostics').replaceChildren(...check.errors.map(error=>el('li','⚠ '+error,'error')),...check.warnings.map(warning=>el('li',warning)));
  $('scenario').value=state.scenario;$('evidence-fixture').value=state.evidenceFixture;
  $('run-status').textContent=`${state.scenario} · simulated snapshot, not a command`;$('uncertainty').textContent=state.uncertainty;$('evidence-status').textContent=`Evidence: ${state.evidence.quality}`;$('message').textContent=state.message;
  $('safety').classList.toggle('uncertain',state.uncertainty!=='Simulation snapshot only');
  $('begin').disabled=!!state.task;$('complete').disabled=!state.task;$('blocked').disabled=!state.task;
  $('task-status').textContent=state.task?`Task ${state.task.index+1} since ${state.task.startedAt} / ${state.task.layout}`:`${state.completedTasks.length} attempts recorded; human verdict pending.`;
  $('notes').replaceChildren(...state.notes.map(note=>el('li',`${note.layout} / ${note.entity}: ${note.text}`)));
  $('log-summary').textContent=`${state.events.length} events · ${state.notes.length} notes · ${state.completedTasks.length} tasks`;
  $('events').replaceChildren(...state.events.slice(-20).reverse().map(event=>{const li=el('li');li.append(el('time',event.time),el('strong',`${event.category} / ${event.layout} / ${event.entity} / ${event.representation} / ${event.scenario}`),el('div',event.detail));return li;}));
  if(state.pendingReset&&!$('reset-dialog').open)$('reset-dialog').showModal();if(!state.pendingReset&&$('reset-dialog').open)$('reset-dialog').close();
  if(!state.pendingReset){if(editKey){const input=[...document.querySelectorAll('[data-edit]')].find(node=>node.dataset.edit===editKey);input?.focus({preventScroll:true});if(cursor&&input)input.setSelectionRange(...cursor);}else if(entityFocus){const container=state.representation==='graph'?$('entity-graph'):$('entity-cards');[...container.querySelectorAll('[data-entity]')].find(node=>node.dataset.entity===entityFocus)?.focus({preventScroll:true});}else if(actionKey){const target=[...document.querySelectorAll('[data-action]')].find(node=>node.dataset.action===actionKey&&!node.disabled);if(target)target.focus({preventScroll:true});else{const action=JSON.parse(actionKey);[...document.querySelectorAll('[data-edit]')].find(node=>node.dataset.edit.startsWith(action.id+':'))?.focus({preventScroll:true});}}}
}
document.querySelectorAll('[data-layout]').forEach(node=>node.addEventListener('click',()=>dispatch({type:'layout',value:node.dataset.layout})));
document.querySelectorAll('[data-representation]').forEach(node=>node.addEventListener('click',()=>dispatch({type:'representation',value:node.dataset.representation})));
$('scenario').addEventListener('change',()=>dispatch({type:'scenario',value:$('scenario').value}));$('evidence-fixture').addEventListener('change',()=>dispatch({type:'evidence',value:$('evidence-fixture').value}));
$('review').addEventListener('click',()=>dispatch({type:'review'}));$('reset').addEventListener('click',()=>dispatch({type:'reset'}));
$('cancel-reset').addEventListener('click',()=>{dispatch({type:'cancelReset'});$('reset').focus();});$('confirm-reset').addEventListener('click',()=>{dispatch({type:'confirmReset'});$('reset').focus();});$('reset-dialog').addEventListener('cancel',event=>{event.preventDefault();dispatch({type:'cancelReset'});$('reset').focus();});
tasks.forEach((task,index)=>$('task').append(new Option(`${index+1}. ${task}`,String(index))));
$('begin').addEventListener('click',()=>dispatch({type:'taskStart',index:Number($('task').value)}));$('complete').addEventListener('click',()=>dispatch({type:'taskFinish'}));$('blocked').addEventListener('click',()=>dispatch({type:'taskFinish',blocked:true}));
$('save-note').addEventListener('click',()=>{dispatch({type:'note',text:$('note').value});$('note').value='';});
$('export').addEventListener('click',()=>{const url=URL.createObjectURL(new Blob([JSON.stringify(exportSession(state),null,2)],{type:'application/json'}));const link=el('a');link.href=url;link.download=`helix-spike-003-${state.session.id}.json`;document.body.append(link);link.click();link.remove();URL.revokeObjectURL(url);});
new ResizeObserver(()=>document.documentElement.style.setProperty('--safe-height',`${$('safety').getBoundingClientRect().height}px`)).observe($('safety'));
render();
