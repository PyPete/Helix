#!/usr/bin/env node

const { readFileSync } = require("node:fs");
const { resolve } = require("node:path");

const phaseDir = __dirname;
const uatPath = resolve(phaseDir, "03-INSTALLED-TAURI-UAT.md");
const decisionPath = resolve(phaseDir, "03-PROMOTION-DECISION.md");
const uat = readFileSync(uatPath, "utf8");
const decision = readFileSync(decisionPath, "utf8");
const expectedViews = ["setup", "experiment", "plan", "run", "results", "samples-analysis"];
const evidenceColumns = [
  "representative record identity",
  "revision or frozen reference",
  "provenance or status",
  "original board observed outcome",
  "legacy comparator observed outcome",
  "keyboard-only evidence",
  "screen-reader evidence",
  "evidence location",
];
const errors = [];

function parseTable(markdown, heading) {
  const lines = markdown.split(/\r?\n/);
  const start = lines.findIndex((line) => line.trim() === heading);
  if (start < 0) return { headers: [], rows: [] };
  let headerIndex = start + 1;
  while (headerIndex < lines.length && !lines[headerIndex].trim().startsWith("|")) headerIndex += 1;
  if (headerIndex >= lines.length) return { headers: [], rows: [] };
  const cells = (line) => line.trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map((cell) => cell.trim());
  const headers = cells(lines[headerIndex]);
  const rows = [];
  for (let index = headerIndex + 1; index < lines.length && lines[index].trim().startsWith("|"); index += 1) {
    if (/^\|\s*:?-{3,}/.test(lines[index].trim())) continue;
    const values = cells(lines[index]);
    if (values.length === headers.length) rows.push(Object.fromEntries(headers.map((key, column) => [key, values[column]])));
  }
  return { headers, rows };
}

const { headers, rows } = parseTable(uat, "## Required six-view task evidence");
if (!uat.includes("`PENDING_INSTALLED_TAURI_UAT`")) errors.push("UAT gate must retain PENDING_INSTALLED_TAURI_UAT.");
for (const header of ["view", ...evidenceColumns, "row state"]) {
  if (!headers.includes(header)) errors.push(`UAT table is missing required column: ${header}.`);
}
if (rows.length !== expectedViews.length) errors.push(`Expected exactly six UAT rows; found ${rows.length}.`);

const placeholder = /\b(?:pending(?:_uat)?|tbd|todo|n\/a|not run|not executed|placeholder|fill in|unknown)\b|\bPENDING_[A-Z0-9_]+\b|\[[^\]]+\]/i;
for (let index = 0; index < expectedViews.length; index += 1) {
  const expected = expectedViews[index];
  const row = rows[index];
  if (!row) continue;
  if (row.view !== expected) errors.push(`Row ${index + 1} must be ${expected}; found ${row.view || "blank"}.`);
  const state = row["row state"];
  if (state !== "NOT_RUN" && state !== "COMPLETE") errors.push(`${expected}: row state must be NOT_RUN or COMPLETE.`);
  for (const column of evidenceColumns) {
    const value = row[column] ?? "";
    if (!value.trim()) errors.push(`${expected}: ${column} must not be blank.`);
    if (state === "COMPLETE" && placeholder.test(value)) errors.push(`${expected}: completed row has placeholder evidence in ${column}.`);
    if (state === "NOT_RUN" && !placeholder.test(value)) errors.push(`${expected}: NOT_RUN row must explicitly mark ${column} as pending.`);
  }
}

if (!decision.includes("`PENDING_FUTURE_SCIENTIST_DECISION`")) errors.push("Initial scientist disposition must remain pending.");
if (!/legacy comparator available/i.test(decision)) errors.push("Decision record must retain legacy comparator availability.");
if (!/makes no primary workspace selection or promotion-code change/i.test(decision)) errors.push("Decision record must state that this plan does not change primary selection.");
if (/\b(?:APPROVED|REJECTED)\b/.test(decision.replace(/Replace the pending values[\s\S]*/, ""))) errors.push("Decision record contains an actual approval/rejection before the disposition section.");
if (/automation\s+(?:alone|itself|suite)?\s*(?:completes?|approves?|promotes?)\s+(?:the\s+)?(?:uat|promotion|board)/i.test(`${uat}\n${decision}`)) {
  errors.push("Records must not claim automation completes UAT, approval, or promotion.");
}
if (!/Automated tests, a build, a prototype, preference, or incomplete evidence cannot complete UAT/i.test(uat)) {
  errors.push("UAT record must explicitly prohibit automated/prototype evidence from completing the gate.");
}

if (errors.length) {
  console.error("Evidence validation FAILED:");
  for (const error of errors) console.error(`- ${error}`);
  process.exitCode = 1;
} else {
  const complete = rows.filter((row) => row["row state"] === "COMPLETE").length;
  console.log(`Evidence structure valid: ${rows.length} rows present; ${complete}/6 actual-evidence rows complete; disposition pending.`);
  if (complete !== rows.length || decision.includes("`PENDING_FUTURE_SCIENTIST_DECISION`")) {
    console.log("Gate remains PENDING — this validator does not conduct human UAT or authorize promotion.");
  }
}
