---
spike: "003"
idea: ivoryos-scientific-workspace
name: inspector-layout-comparison
type: comparison
variants: [003a-contextual-inspector, 003b-permanent-panels]
validates: "Given linked apparatus, protocol, sample and result records, when inspecting and switching context with saved/unsaved fields, then compare provenance clarity, discoverability and retained draft/review/uncertainty context without preselecting a layout winner."
verdict: PENDING
status: built-awaiting-human-review
related: ["001", "002"]
tags: [ui, inspector, provenance, simulation, safety, comparison]
created: 2026-10-30
---

# Spike 003: Context changes. Your intent stays.

## What This Validates

**Given** the same linked device, step, sample and result identities, **when** the scientist traces provenance, edits draft fields and changes selection/layout, **then** collect evidence about discoverability, continuity, evidence quality comprehension and truthful draft/run separation. Unit checks cannot decide layout usability.

003a allocates **one contextual side inspector**, following the selected entity. 003b allocates **three permanent panels below the work area**: device, protocol properties, and sample/evidence. Each category retains its last inspected entity while global selection changes. Same fields, provenance links, fixtures and actions are available in both. On narrow screens panels stack but remain distinct allocated sections. No color-only substitute for a real layout difference.

## Research

### Prior context and scope

Re-read [manifest](../MANIFEST.md), [conventions](../CONVENTIONS.md), pending [TODO](../../todos/spike-canvas-first-scientific-workspace.md), and project AGENTS; checked for nested planning AGENTS (none found). Continues the existing spike workflow, user-selected vanilla/Node stack and restrained 001 visual language. No product changes, dependencies, child agents, original TODO completion or findings wrap-up.

001's **"this looks really good"** remains positive visual feedback/PENDING, not a validated workspace winner. 002's exact comment, dated 2026-10-30, is recorded in its [README](../002-protocol-representation-comparison/README.md): **"I like the workflow graph a lot but too be honest, being able to alternative between both would be brilliant"**. Immediately added that preference under the same idea in MANIFEST: graph preferred, cards first-class and interchangeable, preserving stable IDs, selection and saved/unsaved edits without mutating semantics. Neither inspector layout is preselected.

### Source ledger

Inspected **2026-10-30** through GitHub commit/tree metadata and pinned raw HTTPS. Exact revision verified: **`d451defe20452ca5c11c49f38bc6ad291f4d52de`**, commit date **2026-10-23T22:24:04Z**. Source inspection, not a live upstream usability trial.

| Reference | Direct inspected source | Observation / applicability |
|---|---|---|
| Progressive step edit | [StepEditDialog.tsx](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/frontend/src/components/StepEditDialog.tsx) | Modal wraps StepEditor without retry for a future run step; displays run/step context. This is not a universal apparatus/sample inspector. Borrow contextual identity/disclosure, not live run editing. |
| Shared argument editor | [StepEditor.tsx](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/frontend/src/components/StepEditor.tsx) | Per-argument text, declared units, source-column indications, save/problem state, scope choices and optional retry. Helix spike saves only draft proposals; no API, retry, backend guard or future-run mutation copied. |
| Readable result plus raw record | [ResultView.tsx](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/packages/shared-ui/src/ResultView.tsx) | Field/value leaf table with raw JSON toggle; deliberately does not guess units from suffixes. Borrow explicit units and source visibility; use always-discoverable native details rather than hover-only affordance. |
| Linked workflow disclosure | [WorkflowPeek.tsx](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/packages/shared-ui/src/WorkflowPeek.tsx) | Read-only right drawer keeps canvas visible, shows pinned/latest version and warns when newer; optional detach/edit actions concern linked workflows. Borrow version/read-only context, not detach/update behavior or a claim that upstream proves either layout. |

Repository: [ivoryos-next](https://github.com/ivoryos-ai/ivoryos-next). These inspected frontend/shared sources sit under the [root Apache-2.0 license](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/LICENSE). Do not extend that assumption to cloud: its [FSL-1.1-ALv2 license](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/cloud_frontend/LICENSE.md) has competing-use restrictions and a future Apache grant after the second anniversary, as recorded in 002. No upstream code/artwork copied.

| Approach | Potential benefit | Risk | Status |
|---|---|---|---|
| Single contextual inspector | Focused detail, more work-area width | Lost context/discovery cost on selection changes | 003a built, unvalidated |
| Permanent category panels | Simultaneous device/protocol/evidence context | Density, competing fields and longer scrolling | 003b built, unvalidated |

**Decision:** build both over an independent pure model. Graph/cards preference is held constant across layouts. Helix-specific proposals are per-entity pending buffers, explicit saves/discards, revision-bound UI-only review, frozen admitted run, quarantine and reconciliation latch. None are validated production patterns or device authority.

## How to Run

Repository root, Node 20+, no installation:

```powershell
node .planning/spikes/server.mjs
```

- Comparison: <http://127.0.0.1:4317/003-inspector-layout-comparison/>
- Launcher: <http://127.0.0.1:4317/>
- Alternate port: `node .planning/spikes/server.mjs 4318`; Ctrl+C stops it.

Unchanged localhost static server serves only spikes with host/method/traversal/CSP restrictions. No Agent, API, AI, device, telemetry or external asset requests. No storage or actual CSV import. Reload starts a new session; export before closing/replacing drafts. Styles import only 001's visual language; 003 owns its entities/state/tests, with no cross-spike mutation or product imports.

```powershell
node --test .planning/spikes/003-inspector-layout-comparison/model.test.mjs
node --test .planning/spikes/001-canvas-workspace-comparison/model.test.mjs .planning/spikes/002-protocol-representation-comparison/model.test.mjs .planning/spikes/server.test.mjs
node --check .planning/spikes/003-inspector-layout-comparison/app.mjs
node --check .planning/spikes/003-inspector-layout-comparison/model.mjs
```

## What to Expect

- Compact graph-centered work area; graph/card toggle retains canonical relationships and selection. Six linked entities: R-201, S-401, heat, collect, S-014, RESULT-014. Exact relationship ledger distinguishes capability targets, linear heat → collect dependency and synthetic sample/result provenance. Forward/reverse link buttons are available inside both layouts.
- Single inspector follows any selected entity; permanent panels retain last entity per category. Linked work-area records highlight context; no inspection is scientific acceptance.
- R-201 proposes heat value/unit; heat/collect propose step labels. Buffers are independent per entity. Selection/layout/representation changes never save, discard or review. Return to unfinished 55 °C input after inspecting a result; it remains pending while saved draft/run stay 50 °C.
- Explicit Save draft or Discard unsaved acts only on that entity. Unrelated draft revisions are visibly indicated and merged only into distinct canonical fields, not over another entity's input. Invalid saved 95 °C, blank/nonfinite, K/°F or blank name remains visible and blocks review. UI-only review requires all pending fields saved/discarded and valid saved values; later saves invalidate it. No implicit unit conversion or scientific order validation.
- Draft replacement asks before losing saved/unsaved edits. Cancel retains them; confirmation resets only draft/buffers. Run, evidence, uncertainty and observations stay unchanged. Evidence fixture loads never discard draft fields, so require no data-loss confirmation.
- Immutable SIM-RUN-014 / approved-fixture-r7 stays 50 °C. Running/Paused/Held/Disconnected/Reconciliation controls load simulated snapshots, not run commands. Stale/uncertain status persists after reconnect and selection/layout changes; no retry/replay or execution controls.
- Evidence tables show explicit mg/L, quality and source revision plus inspectable raw record. Quarantined CSV-Q-r1 / RESULT-014/quarantine-r1 cannot be accepted. Explicit pre-reviewed CSV-VALID-r2 loads a **separate synthetic source record**, never promotes CSV-Q-r1. Both correlate to S-014; no actual evidence processing, branch/campaign decision or approval action exists.
- Persistent simulation/policy/uncertainty strip, native controls/focus, responsive stacked panels, text plus semantic colors and reduced-motion support. Full keyboard/a11y/readability verification remains a human task.

## Observability

Begin/Complete/Blocked timing, observer notes and local JSON export. Completion is observer-marked, not proven task success. Each event records ISO time, category, comparison layout, entity, graph/card representation, scenario, uncertainty, fixture and draft revision. Last 20 events display; full log exports.

JSON includes identity/schema/simulation/PENDING verdict, total and split-layout durations, event/task/note/blocked counts, distinct entity/scenario counts, active/completed attempts with start/finish contexts, and detached semantic snapshot of entities/links, draft/run, buffers/pending IDs, panel selections, validation, evidence source and uncertainty. Notes render with textContent; no HTML injection or telemetry. Mid-task switching is evidence, not a clean single-layout timing comparison. Export before reload; actual browser download completion is not claimed.

## Human Review Protocol

Counterbalance first layout and run identical tasks in each. Record navigation/searches, errors, confidence and explanation accuracy alongside durations. Test uninterrupted single-layout tasks separately from preservation toggles. Export observations; no winner threshold is invented.

| Given | When | Then / evidence |
|---|---|---|
| Same canonical records | Trace device → step → sample → result, then reverse | Explain targets versus dependencies, stable S-014 identity, source revision, units and quarantine; record navigation errors |
| Unsaved 55 °C | Select result, return, switch layout/cards | Pending value survives; no silent save/review; frozen run stays 50 °C |
| Pending heat and label | Save one, discard the other | Verify per-entity action scope and review invalidation; explain any advanced-revision notice |
| Invalid heat/unit/name | Save and request review | Invalid draft visible, review blocked, no run mutation |
| Edited draft | Request reset and cancel/confirm | Verify intentional data loss and independent run/evidence/log retention |
| Quarantine/reviewed fixtures | Inspect source/table and switch fixtures | Distinguish separate source revisions from promotion; no branch/campaign authority |
| Disconnected snapshot | Inspect elsewhere then reconnect | Find stale/reconciliation warning; no safe replay inference |
| Either layout | Keyboard, small viewport, graph scrolling | Assess focus/readability, permanent-panel allocation and safety visibility |

## Investigation Trail

1. Recorded 002 preference immediately under the same idea and its README with exact quote/date; kept PENDING and 001 feedback. Added parent-provided 002 DOM smoke with attribution/limitations.
2. Verified pinned revision/date and inspected actual dialog/editor/result/drawer source. None establishes a universal inspector winner. Borrowed disclosure/version/unit concepts only.
3. Built isolated entities/links/evidence fixtures, independent run/draft, per-entity buffers and explicit review/reset rules. Distinct editable fields avoid cross-entity overwrites; evidence source fixtures remain frozen and detached.
4. Built genuinely different one-side versus three-permanent-panel layouts over the same work area/actions. Added trace links, raw/table provenance, pending/global status, responsive panels and observer export.
5. Initial 003 checks passed 12/12 model tests; app/model syntax checks passed. Covers identities/reverse links, independent buffers, layout/context preservation, save/discard/review, invalid values, source/run isolation, cancellation, fixture separation, uncertainty and detached export.
6. Implementation-session DOM smoke at 1000×700 showed one contextual versus three permanent panels, six graph buttons and no document horizontal overflow. DOM-triggered unsaved 55 °C survived evidence selection/return; pending heat/label survived permanent/cards toggles; save preserved the other buffer; pending/95 °C review blocked; reset cancellation kept 95 °C; separate reviewed fixture displayed CSV-VALID-r2; reload of Running after Disconnected retained stale reconciliation. Frozen strip remained 50 °C. Used DOM `.click()`/events, not native pointer/a11y/full visual verification.

## Results

Final targeted rerun: **39/39 passed** (12 for 003, 10 for 001, 16 for 002, 1 shared server boundary); 003 app/model syntax and scoped whitespace checks passed. Console warning/error check after loading the updated app returned no messages, no truncation or dropped entries. A programmatic-focus DOM probe retained input focus through rerender and returned focus to the entity field after explicit save; this is not a native keyboard or full accessibility trial.

**PENDING — built, awaiting human comparative review.** Tests establish narrow simulation semantics and DOM smoke establishes limited wiring, not a layout winner, scientist task dataset, full accessibility/visual verification or production feasibility.

Limitations: six synthetic entities, one linear protocol, fixed graph positions, no dragging/reorder here (002 owns the authoring comparison), no actual CSV/chart measurement, durable execution, device/backend/API/AI or Agent admission. Evidence preview is a table/raw record, not an analytical calculation. No real reconciliation or campaign policy engine. Large datasets and native navigation remain unproven.

**Next:** human review of 003 and retained 001/002 comparison questions. Original TODO remains pending; no production blueprint, validated findings skill or premature wrap-up. Graph/card interchange is explicit user preference, not exclusion of cards or proof of superiority.
