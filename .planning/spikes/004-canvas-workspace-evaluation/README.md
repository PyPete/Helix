---
spike: "004"
idea: ivoryos-scientific-workspace
name: canvas-workspace-evaluation
type: interactive-design-evaluation
validates: "Given the built 001–003 simulated workspace comparisons, when a scientist performs the shared tasks and reviews safety states, then Helix can capture comparative evidence without treating preference as execution authorization or a production decision."
verdict: WRAPPED
related:
  - ../001-canvas-workspace-comparison/README.md
  - ../002-protocol-representation-comparison/README.md
  - ../003-inspector-layout-comparison/README.md
tags: [ui, evaluation, simulation, safety, human-review]
---

# Spike 004: Canvas Workspace Evaluation

## Scope

This is the human-review gate for the existing disposable 001–003 comparisons. It creates no product code and does not declare a winner. All fixtures remain simulated; UI interactions, drafts, and AI suggestions are proposals only. The local Helix Agent remains the sole authority for physical execution and admission.

### Current variant: Helix-safe Designer interaction

This disposable revision tests an IvoryOS-inspired typed Designer interaction without adopting its execution model. The palette contains only simulated **Agent-declared logical capabilities** and typed control-flow nodes; it deliberately exposes no raw driver calls, transports, or direct device commands. Accessible palette buttons provide the add-to-canvas equivalent. The typed inspector makes units, bindings, defaults, provenance, and required resources inspectable as local proposal data.

The graph makes control flow explicit: an **If** node has labeled **True** and **False** outputs with an evaluated-evidence placeholder, while explicit **Fork** and **Join** nodes model parallel work and display pending Agent resource/lease diagnostic placeholders. The lifecycle remains visible as Draft ? client diagnostics ? Agent preflight ? approval required ? execution observed. Submission is proposal-only: it records a simulated request for Agent preflight and never executes, dispatches, leases, approves, or contacts an Agent/API/device.

Cards-first navigation, the on-demand relationship graph, resolve-first draft guard, status-and-route reconciliation strip, themes, and observer/review panel are intentionally retained.

### Prior variant: cards-first flow building

The disposable page now tests whether cards can be the scientist's starting point for **authoring** a proposed flow, with an interactive graph workspace available for relationship editing. Scientists can add typed mock steps, select a source and Shift-click a target, create or remove mock edges, mark a branch, mark parallel work, and receive immediate invalid-edge feedback. The inspector edits only the local fixture and retains the resolve-first unsaved-draft guard. “Submit proposal” records a mock review event only: it does not call an API, Agent, driver, or device and cannot execute anything.

## Status

**WRAPPED — findings synthesized; production work remains approval-gated.** The canonical TODO is converted to follow-up production planning: [canvas-first scientific workspace](../../todos/spike-canvas-first-scientific-workspace.md). This artifact reconciles the already-built comparisons with the outstanding evidence rather than rebuilding or redesigning them.

## Evaluation sequence

1. Launch the disposable comparison host: `node .planning/spikes/server.mjs`.
2. Complete the same apparatus-orientation and linear-protocol task in ribbon/dock and canvas variants; record discoverability, completion, errors, and explanation accuracy.
3. Start in cards for the same five stable protocol identities: ready check, prepare sample, irradiate, analyze aliquot, and reconcile observed state. Locate the held reconciliation branch, open the graph on demand to explain its parallel relationship to Analyze aliquot, then return to cards and record whether selected context is retained.
4. Change device, step, sample, and result selection while a draft is pending; record whether the contextual inspector preserves draft, provenance, units, quality, and safety-review context. Repeat in Light, Dark, and System theme settings, including keyboard focus visibility.
5. Review blocked, active, paused, disconnected, AI-proposal, unreviewed-result, and reconciliation scenarios. Confirm the persistent execution/safety strip remains truthful and that no gesture appears to dispatch a physical command.
6. Export the fixture event log and attach task observations, screenshots, timings, and the state matrix here before updating any verdict.

## Decision Log

### 2026-11-01 - Reconciliation strip: status + route

The persistent execution/safety strip shows the truthful reconciliation/held status and routes the scientist to the detailed safety/evidence view. It exposes no direct `Pause`, `Hold`, or `Abort` controls while reconciliation is pending. Consequential controls remain beside their admission evidence, require their existing confirmations/acknowledgements, and never imply that the UI can authorize execution.

### 2026-11-01 - Inspector drafts: resolve before selection change

Changing contextual-inspector selection while the current entity has an unsaved draft requires explicit resolution: save, discard, or cancel the selection change. The draft is never silently retained, reassigned, autosaved, retargeted, approved, dispatched, or applied to a reviewed fixture/run. The resolution surface must retain the current entity's stable identity, provenance, units, quality flags, and review state.

### 2026-11-01 - Primary work surface: canvas graph + cards toggle

The study randomizes its starting representation rather than treating graph-first as evidence. The graph has directional, labeled connectors and a compact legend; cards present the same five nodes as a main path plus an explicitly grouped parallel branch. Switching views never resolves a draft, changes semantics, or grants execution authority.

### 2026-11-01 - Navigation hypothesis revised after held-reconciliation observation

The observer found cards clearer during the randomized held-reconciliation task because navigation was clearer. Graph-first is therefore no longer the working preference. The next disposable variant should evaluate cards as the navigational default and retain the graph on demand for relationship and parallel-inspection tasks. This remains a tentative hypothesis from one observation; it does not settle the representation choice, approve production UI, or replace the existing side-by-side hybrid without comparative evidence.

### 2026-11-01 - Cards-first/on-demand-graph variant selected

The next disposable variant uses sequence cards as the primary navigator. Its graph opens only on demand for relationship and parallelism inspection, and returning to cards must retain the selected context. Theme controls, the truthful safety status-and-route strip, resolve-first draft handling, visible uncertainty/reconciliation, the observer panel, and the Agent-only authority boundary remain unchanged. This is a spike implementation decision, not a production UI decision.

### 2026-11-01 - Theme and representation parity

Theme is local to this disposable page: Light, Dark, and System choices persist in browser storage and use local `data-theme` CSS variables. Connector, status, warning, and focus colors retain distinct, high-contrast roles in each mode. The observer panel records task outcome, confusion moment, and preference without asserting a winner.

## How to Run and Review

1. Run `node .planning/spikes/server.mjs` from the repository root.
2. Open `http://127.0.0.1:4317/004-canvas-workspace-evaluation/`.
3. Choose each Theme setting and check readable text, distinct status/connector colors, and visible keyboard focus.
4. Start the review task. Using cards, locate the held reconciliation branch; open the graph only to identify its parallel relationship to Analyze aliquot; return to cards and confirm selected context remains intact. Record outcome, confusion moment, and preference (including no preference).
5. Verify the same five named nodes and statuses, selection, inspector, and draft-resolution dialog are preserved. In cards, verify branches are grouped as parallel rather than numbered fake-sequential steps.
6. Edit the inspector temperature, then select another graph node/card. Verify the resolve-first dialog requires Save simulated draft, Discard draft, or Keep editing; no draft is silently reassigned.
7. Select Open safety evidence in the persistent strip. Verify held/reconciliation status routes to evidence without direct operational controls.
8. Review the AI proposal. It must remain a proposal requiring scientist review and Agent admission, never an execution control.

### Experiential authoring task (required)

1. Start **Experiential review task**. From cards, open the relationship workspace and use the Agent-declared palette to add or inspect typed steps; confirm no raw driver calls appear.
2. Build or inspect an **If evidence condition** with clearly labeled **True** and **False** outputs. Inspect its evaluated-evidence placeholder and typed inspector fields (unit, binding, default, provenance, resources).
3. Build or inspect the explicit **Fork parallel work** and **Join parallel work** path. Inspect the visible pending Agent resource/lease diagnostic placeholders.
4. Edit a typed inspector field, attempt to change selection, and confirm Save, Discard, or Keep editing is required.
5. Submit only the proposal for Agent preflight. Confirm the lifecycle does not advance past the simulated proposal boundary and that no Agent/API/device call or execution occurred.

**Falsifiable safety/UX criterion:** this variant is supported only if reviewers can complete all five stages without assistance, correctly explain the If true/false and Fork/Join semantics, retain context after resolving a draft, and state that submit requests Agent preflight rather than execution. It is falsified if a reviewer finds a raw driver/direct-authority action, cannot locate the visible resource/lease or evidence placeholders, observes silent draft loss, or believes submission can execute physical work.

## Observation / Verification Criteria

- [ ] Light, Dark, and System themes preserve contrast, focus indication, connector meaning, and status meaning.
- [ ] Graph and cards preserve the same five protocol identities, statuses, selection, inspector, and draft dialog.
- [ ] Graph connectors are directional and labeled; cards express branches as parallel, not fake sequential steps.
- [ ] The cards-first review task tests both navigation and on-demand parallel-relationship inspection without losing selected context.
- [ ] Inspector selection is blocked until an unsaved draft is explicitly resolved.
- [ ] Held/reconciliation state stays visible across views and routes to evidence with no strip controls.
- [ ] AI proposal wording never claims approval, dispatch, or device authority.
- [ ] All displayed readings and actions are simulated fixtures; no Agent, API, or device call occurs.
- [ ] Reviewer can author or inspect typed If/else and Fork/Join relationships, see evidence/resource/lease placeholders, and submit only a proposal for Agent preflight.

## Evidence checklist

- [ ] Annotated screenshots for baseline and canvas tasks
- [ ] Task timings, completion notes, errors, and clarity observations
- [ ] Graph/card control-flow comparison using identical stable step IDs
- [ ] Inspector selection/draft-preservation observations
- [ ] Execution/safety state matrix including uncertainty and reconciliation
- [ ] AI proposal, safety-review, blocked, and unreviewed/quarantined evidence walkthrough
- [ ] Exported fixture event log and observer notes
- [ ] Reviewed comparison summary, limitations, and separately approval-gated next slice

## Investigation Trail

- 2026-11-01: Reconciled the canonical pending TODO with built spikes 001–003. No production source was changed and no human comparative evidence has been recorded in this artifact.
- 2026-11-01: Revised the disposable fixture for accessible local theme choice and structurally equivalent graph/cards study. The hypothesis remains falsifiable: if randomized participants complete the shared task faster, more accurately, and with less reported confusion in cards (or show no meaningful difference), graph-first preference is not supported.
- 2026-11-01: One held-reconciliation observation indicates cards were clearer because navigation was clearer. The revised, still-falsifiable hypothesis is that cards should be the navigation default while graph remains on demand for relationship and parallel inspection; compare that variant with the existing side-by-side hybrid before drawing a conclusion.
- 2026-11-01: The cards-first/on-demand-graph disposable variant was selected for review. It is supported only if reviewers can complete the combined navigation-and-parallelism task without help and retain selected context after returning to cards; otherwise the cards-first hypothesis fails for this task.

## Results

### 2026-11-01 - Randomized held-reconciliation task

In the randomized held-reconciliation task, the observer reported that **cards were clearer because navigation was clearer**. This is a single comparative observation and evidence against graph-first for this task. It revises the working hypothesis toward cards-first navigation with an on-demand graph for relationship and parallel inspection, while retaining the side-by-side hybrid as the comparison condition. It is not a final representation decision, a general usability verdict, an execution authorization, or approval for production redesign. Follow-up evidence should test the proposed cards-first/on-demand-graph variant against the hybrid and identify whether navigation, information hierarchy, parallelism representation, or visual density drove the difference.

No comparative verdict yet. Existing positive feedback is preserved as preference only; it is not usability validation, an execution authorization, or approval for production redesign.

### 2026-11-01 - Graph renderer defect invalidated prior graph evidence

The prior graph review is **invalid**: its SVG connectors and absolutely positioned cards used incompatible layout coordinates, leaving floating lines and only the Reconcile card reliably visible. It cannot be used as usability evidence. The repaired disposable renderer puts all five cards and six named directional edges in one 900px coordinate stage; its horizontal overflow prevents clipping at a normal desktop viewport. At load, the in-artifact sanity check sets `data-renderer-check="pass"` only if all five stable node identities and all six expected edge elements exist. Re-run the visual review before recording any graph finding.

### Graph visual re-review (required)

At `http://127.0.0.1:4317/004-canvas-workspace-evaluation/`, open **Inspect relationships & parallelism** at a normal desktop viewport. Confirm Reactor R-12 ? Prepare S-142 ? Irradiate runs left-to-right; both labeled parallel paths terminate at Analyze aliquot and Reconcile state, then visibly join; no line floats without an endpoint; and returning to cards preserves the selected inspector context. In DevTools, `.graph-stage` must report `data-renderer-check="pass"`.

## Wrap-up

**2026-11-01 — WRAPPED.** Findings, evidence limits, clean-room boundary, and the dedicated production-phase recommendation are recorded in [WRAP-UP.md](./WRAP-UP.md). The renderer defect invalidates prior graph evidence; the single held-reconciliation observation that cards navigation was clearer is retained as bounded comparative evidence. No production UI or Phase 2 implementation changed.