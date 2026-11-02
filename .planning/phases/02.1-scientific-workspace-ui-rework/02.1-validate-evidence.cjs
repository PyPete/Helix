const fs = require('node:fs');
const path = require('node:path');

const files = {
  discovery: '02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md',
  contract: '02.1-WORKSPACE-BOARD-CONTRACT.md',
  uat: '02.1-PROMOTION-UAT.md',
  harness: '02.1-PROTOTYPE-EVIDENCE-HARNESS.md',
};
const expectedViews = ['setup', 'experiment', 'plan', 'run', 'results', 'samples-analysis'];
const errors = [];

function readArtifact(name) {
  const filePath = path.join(__dirname, files[name]);
  if (!fs.existsSync(filePath)) {
    errors.push(`Missing ${name} artifact: ${files[name]}`);
    return '';
  }
  return fs.readFileSync(filePath, 'utf8');
}

function cells(line) {
  return line.trim().replace(/^\||\|$/g, '').split('|').map(value => value.trim());
}

function tables(markdown) {
  const lines = markdown.split(/\r?\n/);
  const found = [];
  for (let index = 0; index < lines.length - 1; index += 1) {
    if (!lines[index].includes('|') || !/^\s*\|?\s*:?-{3,}/.test(lines[index + 1])) continue;
    const headers = cells(lines[index]);
    const rows = [];
    for (index += 2; index < lines.length && lines[index].includes('|'); index += 1) {
      const values = cells(lines[index]);
      if (values.length === headers.length) rows.push(Object.fromEntries(headers.map((header, position) => [header, values[position]])));
    }
    found.push({ headers, rows });
  }
  return found;
}

function findTable(markdown, headers, name) {
  const table = tables(markdown).find(candidate => headers.every(header => candidate.headers.includes(header)));
  if (!table) errors.push(`${name} table is missing required columns: ${headers.join(', ')}`);
  return table;
}

function completeViews(table, name) {
  if (!table) return;
  const ids = table.rows.map(row => row.view_id);
  const missing = expectedViews.filter(view => !ids.includes(view));
  const duplicate = ids.filter((view, index) => ids.indexOf(view) !== index);
  const unexpected = ids.filter(view => !expectedViews.includes(view));
  if (missing.length) errors.push(`${name} is missing view IDs: ${missing.join(', ')}`);
  if (duplicate.length) errors.push(`${name} has duplicate view IDs: ${[...new Set(duplicate)].join(', ')}`);
  if (unexpected.length) errors.push(`${name} has unexpected view IDs: ${unexpected.join(', ')}`);
}

function incomplete(value) {
  return !value || /\bTBD\b|^\[.*\]$/i.test(value);
}

const discovery = readArtifact('discovery');
const contract = readArtifact('contract');
const uat = readArtifact('uat');
const harness = readArtifact('harness');

if (!/\b(read-only|fail-closed|healthy)\b/i.test(discovery)) errors.push('Discovery lacks reconciliation/comparator state evidence.');

const contractTable = findTable(contract, ['view_id', 'immutable_identity_field', 'revision_or_frozen_reference_field', 'provenance_or_status_field', 'source_location'], 'Contract');
completeViews(contractTable, 'Contract');

const uatTable = findTable(uat, ['view_id', 'representative_record_identity', 'revision_or_frozen_reference', 'provenance_or_status', 'evidence_location'], 'UAT');
completeViews(uatTable, 'UAT');
if (uatTable) {
  for (const row of uatTable.rows) {
    for (const header of uatTable.headers) if (incomplete(row[header])) errors.push(`UAT ${row.view_id} has blank or TBD ${header}.`);
  }
}

const harnessTable = findTable(harness, ['view_id', 'authoritative_state_row_reference', 'exact_immutable_identity', 'revision_or_frozen_reference', 'provenance_or_status', 'selected_disposition', 'uat_evidence_reference', 'no_authority_assertion'], 'Harness');
completeViews(harnessTable, 'Harness');
if (harnessTable) {
  for (const row of harnessTable.rows) {
    for (const header of harnessTable.headers.slice(1)) if (incomplete(row[header])) errors.push(`Harness ${row.view_id} has blank or TBD ${header}.`);
    if (!/RECONCILIATION-COMPARATOR-DISCOVERY\.md/i.test(row.authoritative_state_row_reference)) errors.push(`Harness ${row.view_id} lacks cited state evidence.`);
    if (!/read-only|editable/i.test(row.selected_disposition)) errors.push(`Harness ${row.view_id} lacks a selected disposition.`);
  }
}

const dispositionTable = findTable(harness, ['evidence_state', 'authoritative_state_row_reference', 'selected_disposition', 'affected_surfaces'], 'Disposition');
if (dispositionTable) {
  const expectedStates = ['healthy', 'held', 'reconciling', 'faulted', 'uncertain', 'missing', 'contradictory', 'unknown future state'];
  const states = dispositionTable.rows.map(row => row.evidence_state.toLowerCase());
  const missing = expectedStates.filter(state => !states.includes(state));
  if (missing.length) errors.push(`Disposition table is missing states: ${missing.join(', ')}`);
  for (const row of dispositionTable.rows) {
    if (row.evidence_state.toLowerCase() !== 'healthy' && !/^read-only\b/i.test(row.selected_disposition)) errors.push(`Unsafe ${row.evidence_state} disposition.`);
    if (row.evidence_state.toLowerCase() === 'healthy' && row.selected_disposition !== 'editable only when the exact supported Agent evidence is present') errors.push('Healthy state is not exact-evidence gated.');
  }
}

if (!/NON_PRIMARY_PROTOTYPE/.test(harness)) errors.push('Harness must declare NON_PRIMARY_PROTOTYPE.');
if (/\b(PROMOTED|PROMOTION_APPROVED|FINAL_PROMOTION_APPROVED)\b/i.test(harness) || /\bfinal\s+promotion\s+(?:is|was)\s+(?:approved|complete|accepted)/i.test(harness)) errors.push('Harness makes a final-promotion claim.');
if (/\b(primary|default)\s+(desktop\s+)?(route|mount|workspace)\s*(?:is|:)?\s*(enabled|registered|active|implemented)/i.test(harness)) errors.push('Harness makes an unsafe primary/default mount claim.');
if (!/only authority for drivers and physical execution/i.test(harness) || !/cannot dispatch a command, grant approval, confirm execution/i.test(harness)) errors.push('Harness lacks the Agent-only authority fence.');

if (errors.length) {
  console.error('Evidence validation failed:');
  for (const error of errors) console.error(`- ${error}`);
  process.exitCode = 1;
} else {
  console.log('Evidence validation passed: bounded six-view non-primary Phase 7 recommendation harness is complete.');
}
